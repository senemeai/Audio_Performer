using System;

[Serializable]
public class NoteEvent
{
    public int key_number;      // 1-88
    public int velocity;        // 1-127
    public int start_tick;      // 起始 tick
    public int duration_tick;   // 持续 tick

    [NonSerialized] public float arpeggioDelay;      // 琶音延迟（秒）
    [NonSerialized] public float visualTriggerTime;  // 视觉触发绝对时间（秒）
    [NonSerialized] public float audioTriggerTime;   // 音频触发绝对时间（秒）
    [NonSerialized] public float visualDuration;     // 视觉持续时长（秒），按所在片段 BPM 计算
}