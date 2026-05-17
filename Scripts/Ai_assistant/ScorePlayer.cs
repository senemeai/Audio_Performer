using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ScorePlayer : MonoBehaviour
{
    public static ScorePlayer Instance;

    [Header("引用")]
    public PianoKeyboard pianoKeyboard;

    private List<NoteEvent> mergedNotes = new List<NoteEvent>();
    private bool isPlaying = false;
    private bool isPaused = false;
    private float playStartTime;
    private float pauseOffset;
    private int nextNoteIndex;
    private int totalBarsAccumulated = 0;

    public System.Action OnPlaybackFinished;

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public int TotalBarsAccumulated => totalBarsAccumulated;

    void Awake()
    {
        Instance = this;
    }

    public void Clear()
    {
        Stop();
        mergedNotes.Clear();
        totalBarsAccumulated = 0;
    }

    public void AppendScore(ScoreData segment, bool clearFirst = false)
    {
        if (segment?.notes == null || segment.notes.Length == 0) return;
        if (clearFirst) Clear();

        float tickSec = 15f / Mathf.Max(1, segment.bpm);

        // 1. 计算 offset：现有音符的实际最大结束时间
        float offset = 0f;
        if (mergedNotes.Count > 0)
        {
            foreach (var n in mergedNotes)
            {
                float end = n.visualTriggerTime + n.visualDuration;
                if (end > offset) offset = end;
            }
        }

        // 2. 【关键修复】拼接模式：平移新片段，使第一个音紧贴 offset
        int minTick = 0;
        if (!clearFirst && segment.notes.Length > 0)
        {
            minTick = int.MaxValue;
            foreach (var n in segment.notes)
                if (n.start_tick < minTick) minTick = n.start_tick;
        }

        foreach (var note in segment.notes)
        {
            var copy = new NoteEvent
            {
                key_number = note.key_number,
                velocity = note.velocity,
                start_tick = note.start_tick,
                duration_tick = note.duration_tick,
                arpeggioDelay = note.arpeggioDelay
            };

            // 平移：减去 minTick，让第一个音从 0 开始算，紧贴上一段结尾
            copy.visualTriggerTime = offset + ((note.start_tick - minTick) * tickSec);
            copy.audioTriggerTime = copy.visualTriggerTime + copy.arpeggioDelay;
            copy.visualDuration = note.duration_tick * tickSec;

            mergedNotes.Add(copy);
        }

        totalBarsAccumulated += Mathf.Max(1, segment.total_bars);
        mergedNotes.Sort((a, b) => a.visualTriggerTime.CompareTo(b.visualTriggerTime));
    }

    // 供 AI 续写时参考：返回最后几个音符的上下文
    public string GetLastNotesContext(int count = 4)
    {
        if (mergedNotes == null || mergedNotes.Count == 0) return "";
        var sb = new StringBuilder();
        int start = Mathf.Max(0, mergedNotes.Count - count);
        for (int i = start; i < mergedNotes.Count; i++)
        {
            var n = mergedNotes[i];
            sb.Append($"key_number:{n.key_number}(tick:{n.start_tick}), ");
        }
        return sb.ToString().TrimEnd(' ', ',');
    }

    public void Play()
    {
        if (mergedNotes.Count == 0) return;
        Stop();
        isPlaying = true;
        isPaused = false;
        playStartTime = Time.time;
        pauseOffset = 0;
        nextNoteIndex = 0;
    }

    public void Pause()
    {
        if (!isPlaying || isPaused) return;
        isPaused = true;
        pauseOffset = Time.time - playStartTime;
        pianoKeyboard.ResetAllKeys();
    }

    public void Resume()
    {
        if (!isPlaying || !isPaused) return;
        isPaused = false;
        playStartTime = Time.time - pauseOffset;
    }

    public void Stop()
    {
        isPlaying = false;
        isPaused = false;
        StopAllCoroutines();
        pianoKeyboard.ResetAllKeys();
        nextNoteIndex = 0;
        pauseOffset = 0;
    }

    void Update()
    {
        if (!isPlaying || isPaused) return;

        float currentTime = Time.time - playStartTime;

        while (nextNoteIndex < mergedNotes.Count)
        {
            if (mergedNotes[nextNoteIndex].audioTriggerTime <= currentTime)
            {
                TriggerNote(mergedNotes[nextNoteIndex]);
                nextNoteIndex++;
            }
            else break;
        }

        // 结束检测：基于实际最后一个音的结束时间
        if (nextNoteIndex >= mergedNotes.Count && mergedNotes.Count > 0)
        {
            var last = mergedNotes[mergedNotes.Count - 1];
            float actualEnd = last.visualTriggerTime + last.visualDuration;
            if (currentTime > actualEnd + 1.0f)
            {
                Stop();
                OnPlaybackFinished?.Invoke();
            }
        }
    }

    void TriggerNote(NoteEvent note)
    {
        float volume = Mathf.Clamp(note.velocity / 127f, 0.15f, 1f);
        int midi = note.key_number + 20;

        PianoKey key = pianoKeyboard.GetKeyByMidi(midi);
        if (key == null) return;

        key.PressVisual(true);
        SF2AudioManager.Instance.PlayNote(midi, volume);
        StartCoroutine(ReleaseVisual(key, note.visualDuration));
    }

    IEnumerator ReleaseVisual(PianoKey key, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (key != null)
        {
            key.PressVisual(false);
            SF2AudioManager.Instance.StopNote(key.midiNote);
        }
    }
}