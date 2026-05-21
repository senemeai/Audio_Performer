using System.Collections.Generic;

namespace PianoComposition
{
    public enum TextureType
    {
        Homophonic,     // 柱式织体：所有音同时发声，厚重有力
        Broken,         // 分解织体：音依次弹出，流动轻盈
        Alberti,        // 阿尔贝蒂低音：低-高-中-高的经典模式
        Arpeggiated,    // 琶音织体：快速琶音，华丽
        Walking,        // 行走低音：每拍一个低音，爵士感
        Alternating     // 交替织体：低音-和弦-低音-和弦
    }

    public enum BrokenPatternType
    {
        Standard,      // 标准：1-3-5-1 (根-三-五-根)
        RootFifth,     // 根-五模式：1-5-1-5
        Classic,       // 古典模式：1-5-1-3 (C-G-C-E)
        Arpeggio,      // 琶音：1-3-5-1(高) 上行
        Reverse,       // 反向：1(高)-5-3-1 下行
        Waltz,         // 华尔兹：1-5-1 三拍子
        Boogie         // 布吉：1-3-5-6-5-3 六连音
    }

    [System.Serializable]
    public class TextureConfig
    {
        public TextureType type;
        public string description;
        public int minVoices;
        public int maxVoices;
        public float defaultDelay;
        public int[] rhythmPattern;

        public TextureConfig(TextureType t, string desc, int minV, int maxV, float delay, int[] rhythm)
        {
            type = t;
            description = desc;
            minVoices = minV;
            maxVoices = maxV;
            defaultDelay = delay;
            rhythmPattern = rhythm;
        }
    }

    [System.Serializable]
    public class BrokenPattern
    {
        public string name;
        public BrokenPatternType type;
        public int[] noteOrder;
        public int[] tickPattern;
        public string description;

        public BrokenPattern(string n, BrokenPatternType t, int[] order, int[] ticks, string desc)
        {
            name = n;
            type = t;
            noteOrder = order;
            tickPattern = ticks;
            description = desc;
        }
    }
}