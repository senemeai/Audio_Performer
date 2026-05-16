using UnityEngine;

public static class StylePresets
{
    // 内部英文 Key（给代码用，绝不暴露乱码风险）
    public static readonly string[] Names = new string[]
    {
        "Pop", "Kids", "Classical", "Jazz", "Epic", "Minimal"
    };

    // 中文显示名（给 UI 用）
    public static readonly string[] DisplayNames = new string[]
    {
        "流行抒情", "轻快儿歌", "古典练习曲", "爵士即兴", "电影史诗", "极简氛围"
    };

    public static StyleInfo GetPreset(string name)
    {
        switch (name)
        {
            case "Pop": return new StyleInfo(75, 90, "C-Am-F-G", "起弱60→渐强90→收弱50");
            case "Kids": return new StyleInfo(110, 130, "C-F-G", "均匀明亮80-100");
            case "Classical": return new StyleInfo(80, 120, "C-F-G-C", "严格乐句起伏");
            case "Jazz": return new StyleInfo(100, 140, "F-Bb-C7", "弱拍重音对比");
            case "Epic": return new StyleInfo(60, 80, "Am-Em-F-C", "爆发式40→110→70");
            case "Minimal": return new StyleInfo(50, 70, "C-Am", "极弱绵延30-60");
            default: return new StyleInfo(80, 100, "C-Am-F-G", "均匀80");
        }
    }

    public struct StyleInfo
    {
        public int bpmMin, bpmMax;
        public string chordProgression;
        public string dynamicsProfile;

        public StyleInfo(int min, int max, string chords, string dyn)
        {
            bpmMin = min; bpmMax = max; chordProgression = chords; dynamicsProfile = dyn;
        }
    }
}