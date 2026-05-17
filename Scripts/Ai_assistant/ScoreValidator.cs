using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ScoreValidator
{
    public static ScoreData Validate(ScoreData input, string keySignature)
    {
        if (input == null) return null;

        var output = new ScoreData
        {
            bpm = Mathf.Clamp(input.bpm, 40, 200),
            key_signature = input.key_signature,
            style = input.style,
            total_bars = Mathf.Max(1, input.total_bars),
            notes = new NoteEvent[0]
        };

        var rawNotes = input.GetNotesAsList();
        var validNotes = new List< NoteEvent > ();

        // 1. 调式校验 + 格式清理
        var allowedKeys = GetAllowedKeys(keySignature);

        foreach (var note in rawNotes)
        {
            if (note.key_number < 1 || note.key_number > 88) continue;

            // 【关键修改】左手伴奏（低音区）强制调内音，保证和弦和谐
            if (note.key_number <= 43 && !allowedKeys.Contains(note.key_number))
                note.key_number = SnapToNearest(note.key_number, allowedKeys);
            // 右手旋律：允许黑键作为经过音/色彩音，只有偏离调内音超过1个半音才修正
            else if (note.key_number >= 44 && !allowedKeys.Contains(note.key_number))
            {
                int nearest = SnapToNearest(note.key_number, allowedKeys);
                // 与最近调内音距离 > 1 个半音，说明不是合理的经过音，强制修正
                if (Mathf.Abs(note.key_number - nearest) > 2)
                    note.key_number = nearest;
                // 否则保留黑键（如 D# 作为 D→E 的经过音）
            }

            note.velocity = Mathf.Clamp(note.velocity, 1, 127);
            note.duration_tick = Mathf.Max(1, Mathf.Min(note.duration_tick, 32));

            validNotes.Add(note);
        }

        // 2. 密度过滤（同一 tick 最多 6 个音）
        validNotes = FilterDensity(validNotes, 6);

        // 3. 强制旋律单音/双音约束（防止和弦堆叠混乱）
        validNotes = EnforceMelodySingleNote(validNotes);

        // 4. 时间排序
        validNotes.Sort((a, b) => a.start_tick.CompareTo(b.start_tick));

        // 5. 琶音标记
        MarkArpeggio(validNotes);

        output.notes = validNotes.ToArray();
        return output;
    }
    static List<int> GetAllowedKeys(string keySig)
    {
        bool isMinor = keySig.EndsWith("m") || keySig.Contains("小调");
        string root = keySig.Replace("m", "").Replace("大调", "").Replace("小调", "").Trim();

        int rootIndex = NoteNameToSemitone(root);
        int[] intervals = isMinor
            ? new int[] { 0, 2, 3, 5, 7, 8, 10 }  // 自然小调：全半全全半全全
            : new int[] { 0, 2, 4, 5, 7, 9, 11 }; // 大调：全全半全全全半

        var allowed = new List<int>();
        foreach (int iv in intervals)
            allowed.Add((rootIndex + iv) % 12);

        var list = new List<int>();
        for (int i = 1; i <= 88; i++)
        {
            int midi = i + 20;
            int n = midi % 12;
            if (allowed.Contains(n))
                list.Add(i);
        }
        return list;
    }

    static int NoteNameToSemitone(string name)
    {
        switch (name)
        {
            case "C": return 0;
            case "C#": case "Db": return 1;
            case "D": return 2;
            case "D#": case "Eb": return 3;
            case "E": return 4;
            case "F": return 5;
            case "F#": case "Gb": return 6;
            case "G": return 7;
            case "G#": case "Ab": return 8;
            case "A": return 9;
            case "A#": case "Bb": return 10;
            case "B": return 11;
            default: return 0;
        }
    }

    static int SnapToNearest(int key, List<int> allowed)
    {
        int nearest = allowed[0];
        int minDist = Mathf.Abs(key - nearest);
        foreach (var k in allowed)
        {
            int d = Mathf.Abs(key - k);
            if (d < minDist) { minDist = d; nearest = k; }
        }
        return nearest;
    }

    static List<NoteEvent> FilterDensity(List<NoteEvent> notes, int max)
    {
        var groups = notes.GroupBy(n => n.start_tick);
        var result = new List<NoteEvent>();
        foreach (var g in groups)
        {
            var list = g.OrderByDescending(n => n.velocity).Take(max).ToList();
            result.AddRange(list);
        }
        return result;
    }

    /// <summary>
    /// 强制右手旋律区域（key_number >= 44）同一时刻最多 2 个音
    /// 允许双音（如三度、六度），禁止三音以上的右手和弦
    /// </summary>
    static List<NoteEvent> EnforceMelodySingleNote(List<NoteEvent> notes)
    {
        var result = new List<NoteEvent>();
        var groups = notes.GroupBy(n => n.start_tick);

        foreach (var g in groups)
        {
            var list = g.ToList();
            var rightHandNotes = list.Where(n => n.key_number >= 44).ToList();
            var leftHandNotes = list.Where(n => n.key_number <= 43).ToList();

            // 右手（旋律）同一时刻最多保留 2 个音（允许双音，禁止三音以上和弦）
            if (rightHandNotes.Count > 2)
            {
                // 保留力度最大的 2 个音
                rightHandNotes = rightHandNotes.OrderByDescending(n => n.velocity).Take(2).ToList();
            }

            result.AddRange(leftHandNotes);
            result.AddRange(rightHandNotes);
        }

        return result.OrderBy(n => n.start_tick).ToList();
    }

    /// <summary>
    /// 智能琶音标记：
    /// - 左手和弦（≥3个音）：25ms 间隔琶音
    /// - 右手双音：3ms 轻微延迟（几乎同时）
    /// - 单音：无延迟
    /// - 右手三音以上：已被 EnforceMelodySingleNote 过滤，不会出现
    /// </summary>
    static void MarkArpeggio(List<NoteEvent> notes)
    {
        // 按 start_tick 分组
        var groups = notes.GroupBy(n => n.start_tick).ToList();

        foreach (var g in groups)
        {
            var list = g.OrderBy(n => n.key_number).ToList();

            // 判断是否为左手和弦（低音区，3个音以上）
            bool isLeftHandChord = list.All(n => n.key_number <= 43) && list.Count >= 3;
            // 判断是否为右手双音
            bool isRightHandDouble = list.All(n => n.key_number >= 44) && list.Count == 2;

            if (isLeftHandChord)
            {
                // 左手和弦：25ms 间隔琶音，让和弦听起来更圆润
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.025f;
            }
            else if (isRightHandDouble)
            {
                // 右手双音：3ms 轻微延迟（模拟真实钢琴无法绝对同时的物理特性）
                // 几乎听不出，但能让声音略微自然
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.003f;
            }
            else if (list.Count > 1 && list.Any(n => n.key_number <= 43) && list.Any(n => n.key_number >= 44))
            {
                // 混合手位（左右手同时按）：低音优先触发（先左后右）
                var left = list.Where(n => n.key_number <= 43).OrderBy(n => n.key_number).ToList();
                var right = list.Where(n => n.key_number >= 44).OrderBy(n => n.key_number).ToList();
                float delay = 0f;
                foreach (var note in left)
                {
                    note.arpeggioDelay = delay;
                    delay += 0.015f;
                }
                foreach (var note in right)
                {
                    note.arpeggioDelay = delay;
                    delay += 0.008f;
                }
            }
            else if (list.Count > 1)
            {
                // 其他情况（如同区域双音）：轻微延迟
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.005f;
            }
            else
            {
                // 单音：无延迟
                list[0].arpeggioDelay = 0f;
            }
        }
    }
}