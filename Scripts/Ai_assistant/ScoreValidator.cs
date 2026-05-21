using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PianoComposition;

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
        var validNotes = new List<NoteEvent>();

        string effectiveKey = input.key_signature ?? keySignature ?? "C";
        var allowedKeys = GetAllowedKeys(effectiveKey);

        foreach (var note in rawNotes)
        {
            if (note.key_number < 1 || note.key_number > 88) continue;

            if (note.key_number <= 43 && !allowedKeys.Contains(note.key_number))
                note.key_number = SnapToNearest(note.key_number, allowedKeys);
            else if (note.key_number >= 44 && !allowedKeys.Contains(note.key_number))
            {
                int nearest = SnapToNearest(note.key_number, allowedKeys);
                if (Mathf.Abs(note.key_number - nearest) > 2)
                    note.key_number = nearest;
            }

            note.velocity = Mathf.Clamp(note.velocity, 1, 127);
            note.duration_tick = Mathf.Max(1, Mathf.Min(note.duration_tick, 32));

            validNotes.Add(note);
        }

        validNotes = FilterDensity(validNotes, 6);
        validNotes = EnforceMelodySingleNote(validNotes);
        validNotes.Sort((a, b) => a.start_tick.CompareTo(b.start_tick));

        // 应用织体优化
        validNotes = OptimizeLeftHandTexture(validNotes, input.style);

        // 标记琶音延迟
        MarkArpeggio(validNotes);

        output.notes = validNotes.ToArray();
        output = ApplyMusicalPolish(output);

        return output;
    }

    static List<int> GetAllowedKeys(string keySig)
    {
        bool isMinor = keySig.EndsWith("m") || keySig.Contains("小调");
        string root = keySig.Replace("m", "").Replace("大调", "").Replace("小调", "").Trim();

        int rootIndex = NoteNameToSemitone(root);
        int[] intervals = isMinor
            ? new int[] { 0, 2, 3, 5, 7, 8, 10 }
            : new int[] { 0, 2, 4, 5, 7, 9, 11 };

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

    static List<NoteEvent> EnforceMelodySingleNote(List<NoteEvent> notes)
    {
        var result = new List<NoteEvent>();
        var groups = notes.GroupBy(n => n.start_tick);

        foreach (var g in groups)
        {
            var list = g.ToList();
            var rightHandNotes = list.Where(n => n.key_number >= 44).ToList();
            var leftHandNotes = list.Where(n => n.key_number <= 43).ToList();

            if (rightHandNotes.Count > 2)
            {
                rightHandNotes = rightHandNotes.OrderByDescending(n => n.velocity).Take(2).ToList();
            }

            result.AddRange(leftHandNotes);
            result.AddRange(rightHandNotes);
        }

        return result.OrderBy(n => n.start_tick).ToList();
    }

    // 新增：优化左手织体
    static List<NoteEvent> OptimizeLeftHandTexture(List<NoteEvent> notes, string style)
    {
        var result = new List<NoteEvent>();
        var notesByMeasure = notes.GroupBy(n => n.start_tick / 16).ToList();

        var textureSequence = TextureLibrary.GetTextureSequence(style);

        for (int m = 0; m < notesByMeasure.Count && m < textureSequence.Count; m++)
        {
            var measure = notesByMeasure[m].ToList();
            var leftHand = measure.Where(n => n.key_number <= 43).ToList();
            var rightHand = measure.Where(n => n.key_number >= 44).ToList();

            // 右手旋律直接添加
            result.AddRange(rightHand);

            // 左手应用织体优化
            if (leftHand.Count >= 3)
            {
                var optimizedLeft = ApplyTextureToChord(leftHand, textureSequence[m], style, m);
                result.AddRange(optimizedLeft);
            }
            else if (leftHand.Count == 2)
            {
                // 双音：简单处理
                foreach (var note in leftHand)
                {
                    note.arpeggioDelay = 0.01f;
                    result.Add(note);
                }
            }
            else if (leftHand.Count == 1)
            {
                result.AddRange(leftHand);
            }
        }

        return result.OrderBy(n => n.start_tick).ToList();
    }

    static List<NoteEvent> ApplyTextureToChord(List<NoteEvent> chordNotes, TextureType texture, string style, int measureIndex)
    {
        var result = new List<NoteEvent>();
        if (chordNotes.Count == 0) return result;

        chordNotes = chordNotes.OrderBy(n => n.key_number).ToList();

        // 尝试应用经典分解模式
        if (texture == TextureType.Broken || texture == TextureType.Alberti)
        {
            var brokenResult = ApplyClassicBrokenPattern(chordNotes, measureIndex * 16, style, measureIndex);
            if (brokenResult != null && brokenResult.Count > 0)
                return brokenResult;
        }

        // 回退到普通织体处理
        var triggerTicks = TextureLibrary.GetTriggerTicks(texture, chordNotes.Count);
        var interNoteDelay = TextureLibrary.GetDelayBetweenNotes(texture);

        for (int i = 0; i < chordNotes.Count; i++)
        {
            var original = chordNotes[i];
            var newNote = new NoteEvent
            {
                key_number = original.key_number,
                velocity = AdjustVelocityForTexture(original.velocity, texture, i, chordNotes.Count),
                start_tick = original.start_tick,
                duration_tick = original.duration_tick,
                arpeggioDelay = (i < triggerTicks.Count ? triggerTicks[i] * 0.01f : 0) + (i * interNoteDelay)
            };
            result.Add(newNote);
        }

        return result;
    }

    static List<NoteEvent> ApplyClassicBrokenPattern(List<NoteEvent> chordNotes, int measureStartTick, string style, int measureIndex)
    {
        if (chordNotes.Count < 3) return null;

        chordNotes = chordNotes.OrderBy(n => n.key_number).ToList();

        int rootKey = chordNotes[0].key_number;
        int thirdKey = chordNotes[1].key_number;
        int fifthKey = chordNotes[2].key_number;

        int interval1 = thirdKey - rootKey;
        int interval2 = fifthKey - thirdKey;

        if ((interval1 != 3 && interval1 != 4) || (interval2 != 3 && interval2 != 4))
        {
            return null;
        }

        var pattern = TextureLibrary.GetRecommendedBrokenPattern(style, measureIndex);
        int avgVelocity = (int)chordNotes.Average(n => n.velocity);

        return TextureLibrary.GenerateBrokenChord(
            rootKey, thirdKey, fifthKey, pattern,
            measureStartTick, avgVelocity, 12
        );
    }

    static int AdjustVelocityForTexture(int originalVel, TextureType texture, int noteIndex, int totalNotes)
    {
        switch (texture)
        {
            case TextureType.Homophonic:
                return Mathf.Clamp(originalVel, 50, 80);
            case TextureType.Broken:
                if (noteIndex == 0) return Mathf.Clamp(originalVel + 5, 55, 85);
                return Mathf.Clamp(originalVel - 5, 45, 70);
            case TextureType.Alberti:
                if (noteIndex == 0) return Mathf.Clamp(originalVel + 10, 60, 90);
                return Mathf.Clamp(originalVel - 10, 40, 65);
            case TextureType.Walking:
                return Mathf.Clamp(originalVel + 8, 55, 85);
            default:
                return originalVel;
        }
    }

    static void MarkArpeggio(List<NoteEvent> notes)
    {
        var groups = notes.GroupBy(n => n.start_tick).ToList();

        foreach (var g in groups)
        {
            var list = g.OrderBy(n => n.key_number).ToList();

            bool isLeftHandChord = list.All(n => n.key_number <= 43) && list.Count >= 3;

            if (isLeftHandChord && list.Count == 3)
            {
                float[] classicDelays = { 0f, 0.015f, 0.028f, 0.038f };
                for (int i = 0; i < list.Count && i < classicDelays.Length; i++)
                {
                    list[i].arpeggioDelay = classicDelays[i];
                }
            }
            else if (isLeftHandChord && list.Count > 3)
            {
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.012f;
            }
            else if (list.Count == 2 && list.All(n => n.key_number >= 44))
            {
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.003f;
            }
            else if (list.Count > 1)
            {
                for (int i = 0; i < list.Count; i++)
                    list[i].arpeggioDelay = i * 0.005f;
            }
            else
            {
                if (list.Count > 0)
                    list[0].arpeggioDelay = 0f;
            }
        }
    }

    static ScoreData ApplyMusicalPolish(ScoreData data)
    {
        if (data?.notes == null || data.notes.Length == 0) return data;

        var notes = data.notes.ToList();
        var groupedByMeasure = notes.GroupBy(n => n.start_tick / 16).ToList();

        for (int measureIndex = 0; measureIndex < groupedByMeasure.Count && measureIndex < 4; measureIndex++)
        {
            int targetVel = measureIndex switch
            {
                0 => Random.Range(65, 75),
                1 => Random.Range(75, 85),
                2 => Random.Range(85, 100),
                3 => Random.Range(60, 80),
                _ => 75
            };

            foreach (var note in groupedByMeasure[measureIndex])
            {
                if (note.key_number >= 44)
                    note.velocity = Mathf.Clamp(targetVel + Random.Range(-5, 5), 60, 110);
                else
                    note.velocity = Mathf.Clamp(targetVel - Random.Range(15, 25), 40, 70);
            }
        }

        data.notes = notes.OrderBy(n => n.start_tick).ToArray();
        return data;
    }

    public static string GetTexturePromptText(string style)
    {
        var sequence = TextureLibrary.GetTextureSequence(style);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【左手伴奏织体安排（4小节）】");

        string[] textureNames = { "柱式和弦", "分解和弦", "阿尔贝蒂低音", "琶音", "行走低音", "交替织体" };

        for (int i = 0; i < sequence.Count && i < 4; i++)
        {
            string name = textureNames[(int)sequence[i]];
            string desc = TextureLibrary.Textures[sequence[i]].description;
            sb.AppendLine($"小节{i + 1}：{name} - {desc}");
        }

        sb.AppendLine("\n【经典分解模式 - 最重要】");
        sb.AppendLine("请使用【根→五→高根→高三】模式，例如C大三和弦：");
        sb.AppendLine("- 第1个音：C2（根音）→ tick 0");
        sb.AppendLine("- 第2个音：G2（五音）→ tick 4");
        sb.AppendLine("- 第3个音：C3（高八度根音）→ tick 8");
        sb.AppendLine("- 第4个音：E3（高八度三音）→ tick 12");
        sb.AppendLine("这种模式好听的原因：根音到五音是纯五度跳跃，音响开阔；五音到高八度根音又是五度，音区扩展；高八度根音到高八度三音是三度，温柔收尾。");
        sb.AppendLine("\n【禁止】不要使用简单的1-3-5-1或1-2-3-1均分模式，这些听感很一般。");

        return sb.ToString();
    }
}