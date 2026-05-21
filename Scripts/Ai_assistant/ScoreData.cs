using System;
using System.Collections.Generic;

[Serializable]
public class ScoreData
{
    public int bpm;
    public string key_signature;
    public string style;
    public int total_bars;
    public NoteEvent[] notes;   // JsonUtility 支持数组解析
    public string[] chords;
    public List<NoteEvent> GetNotesAsList()
    {
        return notes != null ? new List< NoteEvent > (notes) : new List< NoteEvent > ();
    }
}

// 用于 JsonUtility 直接解析的包装（数组兼容）
[Serializable]
public class ScoreDataRaw
{
    public int bpm;
    public string key_signature;
    public string style;
    public int total_bars;
    public NoteEvent[] notes;
}