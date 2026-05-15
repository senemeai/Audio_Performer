using UnityEngine;

public static class StylePresets
{
    public static readonly string[] Names = new string[]
    {
        "流行抒情", "轻快儿歌", "古典练习曲", "爵士即兴", "电影史诗", "极简氛围"
    };

    public static StyleInfo GetPreset(string name)
    {
        switch (name)
        {
            case "流行抒情":
                return new StyleInfo(75, 90,
                "A: C-Am-F-G | B: Am-F-C-G | C: F-C-G-Am",
                "起弱60→渐强90→收弱50");
            case "轻快儿歌":
                return new StyleInfo(110, 130,
                "A: C-F-G | B: G-C-D7 | C: C-G-Am-F",
                "均匀明亮80-100");
            case "古典练习曲":
                return new StyleInfo(80, 120,
                "A: C-F-G-C | B: C-G-Am-F | C: Am-Dm-G-C",
                "严格乐句起伏");
            case "爵士即兴":
                return new StyleInfo(100, 140,
                "A: F-Bb-C7 | B: Gm7-C7-F | C: Dm7-G7-C",
                "弱拍重音对比");
            case "电影史诗":
                return new StyleInfo(60, 80,
                "A: Am-Em-F-C | B: Dm-Am-C-G | C: Am-F-Dm-E",
                "爆发式40→110→70");
            case "极简氛围":
                return new StyleInfo(50, 70,
                "A: C-Am | B: Am-Em | C: F-C",
                "极弱绵延30-60");
            default:
                return new StyleInfo(80, 100,
                "A: C-Am-F-G | B: Am-F-C-G | C: F-C-G-Am",
                "均匀80");
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