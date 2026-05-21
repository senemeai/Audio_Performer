using System.Collections.Generic;
using UnityEngine;

namespace PianoComposition
{
    public static class TextureLibrary
    {
        public static readonly Dictionary<TextureType, TextureConfig> Textures = new Dictionary<TextureType, TextureConfig>
        {
            [TextureType.Homophonic] = new TextureConfig(
                TextureType.Homophonic, "柱式和弦", 3, 5, 0.0f,
                new int[] { 0 }
            ),

            [TextureType.Broken] = new TextureConfig(
                TextureType.Broken, "分解和弦", 2, 4, 0.02f,
                new int[] { 0, 4, 8, 12 }
            ),

            [TextureType.Alberti] = new TextureConfig(
                TextureType.Alberti, "阿尔贝蒂低音", 3, 3, 0.015f,
                new int[] { 0, 8, 4, 12 }
            ),

            [TextureType.Arpeggiated] = new TextureConfig(
                TextureType.Arpeggiated, "琶音", 3, 6, 0.008f,
                new int[] { 0, 2, 4, 6, 8, 10 }
            ),

            [TextureType.Walking] = new TextureConfig(
                TextureType.Walking, "行走低音", 1, 2, 0.1f,
                new int[] { 0, 4, 8, 12 }
            ),

            [TextureType.Alternating] = new TextureConfig(
                TextureType.Alternating, "交替织体", 2, 4, 0.0f,
                new int[] { 0, 8 }
            )
        };

        public static readonly Dictionary<BrokenPatternType, BrokenPattern> BrokenPatterns = new Dictionary<BrokenPatternType, BrokenPattern>
        {
            [BrokenPatternType.Classic] = new BrokenPattern(
                "古典分解",
                BrokenPatternType.Classic,
                new int[] { 0, 2, 0, 1 },
                new int[] { 0, 4, 8, 12 },
                "根→五→高根→高三 | 经典好听模式"
            ),

            [BrokenPatternType.RootFifth] = new BrokenPattern(
                "根五交替",
                BrokenPatternType.RootFifth,
                new int[] { 0, 2, 0, 2 },
                new int[] { 0, 4, 8, 12 },
                "根音和五音交替，稳定空旷"
            ),

            [BrokenPatternType.Standard] = new BrokenPattern(
                "标准上行",
                BrokenPatternType.Standard,
                new int[] { 0, 1, 2, 0 },
                new int[] { 0, 4, 8, 12 },
                "逐级上行，明亮自然"
            ),

            [BrokenPatternType.Waltz] = new BrokenPattern(
                "华尔兹",
                BrokenPatternType.Waltz,
                new int[] { 0, 2, 0 },
                new int[] { 0, 4, 8 },
                "三拍圆舞曲，优雅旋转"
            ),

            [BrokenPatternType.Arpeggio] = new BrokenPattern(
                "快速琶音",
                BrokenPatternType.Arpeggio,
                new int[] { 0, 1, 2, 0, 2, 1 },
                new int[] { 0, 2, 4, 6, 8, 10 },
                "快速流动，华丽装饰"
            ),

            [BrokenPatternType.Boogie] = new BrokenPattern(
                "布吉伍吉",
                BrokenPatternType.Boogie,
                new int[] { 0, 2, 1, 2, 0, 2, 1, 2 },
                new int[] { 0, 2, 3, 5, 8, 10, 11, 13 },
                "摇摆节奏，蓝调风味"
            )
        };

        public static List<TextureType> GetTextureSequence(string style, int sectionIndex = 0)
        {
            var sequence = new List<TextureType>();

            switch (style)
            {
                case "Pop":
                    sequence.AddRange(new[] {
                        TextureType.Broken, TextureType.Homophonic,
                        TextureType.Alberti, TextureType.Homophonic
                    });
                    break;

                case "Classical":
                    sequence.AddRange(new[] {
                        TextureType.Alberti, TextureType.Alberti,
                        TextureType.Arpeggiated, TextureType.Homophonic
                    });
                    break;

                case "Jazz":
                    sequence.AddRange(new[] {
                        TextureType.Walking, TextureType.Alternating,
                        TextureType.Alternating, TextureType.Homophonic
                    });
                    break;

                default:
                    sequence.AddRange(new[] {
                        TextureType.Broken, TextureType.Homophonic,
                        TextureType.Broken, TextureType.Homophonic
                    });
                    break;
            }

            return sequence;
        }

        public static BrokenPattern GetRecommendedBrokenPattern(string style, int measureIndex)
        {
            switch (style)
            {
                case "Pop":
                    return measureIndex % 2 == 0 ?
                        BrokenPatterns[BrokenPatternType.Standard] :
                        BrokenPatterns[BrokenPatternType.Classic];

                case "Classical":
                    return measureIndex < 2 ?
                        BrokenPatterns[BrokenPatternType.Classic] :
                        BrokenPatterns[BrokenPatternType.Arpeggio];

                case "Jazz":
                    return measureIndex % 2 == 0 ?
                        BrokenPatterns[BrokenPatternType.RootFifth] :
                        BrokenPatterns[BrokenPatternType.Boogie];

                default:
                    return BrokenPatterns[BrokenPatternType.Classic];
            }
        }

        public static List<int> GetTriggerTicks(TextureType type, int chordNotesCount)
        {
            var config = Textures[type];
            var ticks = new List<int>();

            switch (type)
            {
                case TextureType.Homophonic:
                    for (int i = 0; i < chordNotesCount; i++)
                        ticks.Add(0);
                    break;

                case TextureType.Broken:
                    for (int i = 0; i < chordNotesCount && i < config.rhythmPattern.Length; i++)
                        ticks.Add(config.rhythmPattern[i % config.rhythmPattern.Length]);
                    break;

                case TextureType.Alberti:
                    for (int i = 0; i < chordNotesCount && i < config.rhythmPattern.Length; i++)
                        ticks.Add(config.rhythmPattern[i]);
                    break;

                case TextureType.Walking:
                    for (int i = 0; i < chordNotesCount; i++)
                        ticks.Add(i * 4);
                    break;

                case TextureType.Alternating:
                    ticks.Add(0);
                    for (int i = 1; i < chordNotesCount; i++)
                        ticks.Add(8);
                    break;

                default:
                    for (int i = 0; i < chordNotesCount; i++)
                        ticks.Add(0);
                    break;
            }

            return ticks;
        }

        public static float GetDelayBetweenNotes(TextureType type)
        {
            return Textures[type].defaultDelay;
        }

        public static List<NoteEvent> GenerateBrokenChord(
            int rootKeyNumber,
            int thirdKeyNumber,
            int fifthKeyNumber,
            BrokenPattern pattern,
            int measureStartTick,
            int baseVelocity,
            int octaveUp = 12)
        {
            var notes = new List<NoteEvent>();

            int[] pitches = new int[] { rootKeyNumber, thirdKeyNumber, fifthKeyNumber };
            int[] pitchesHigh = new int[] {
                rootKeyNumber + octaveUp,
                thirdKeyNumber + octaveUp,
                fifthKeyNumber + octaveUp
            };

            for (int i = 0; i < pattern.noteOrder.Length && i < pattern.tickPattern.Length; i++)
            {
                int noteIndex = pattern.noteOrder[i];
                bool useHigh = (i >= 2 && pattern.type == BrokenPatternType.Classic && noteIndex == 0);

                int keyNumber;
                if (useHigh)
                    keyNumber = pitchesHigh[noteIndex];
                else if (noteIndex == 0 && pattern.type == BrokenPatternType.Classic && i == 2)
                    keyNumber = pitchesHigh[0];
                else
                    keyNumber = pitches[noteIndex];

                int velocity = noteIndex == 0 ?
                    Mathf.Clamp(baseVelocity + 5, 50, 85) :
                    Mathf.Clamp(baseVelocity - 5, 40, 75);

                int duration = (i == pattern.tickPattern.Length - 1) ? 6 : 2;

                notes.Add(new NoteEvent
                {
                    key_number = keyNumber,
                    velocity = velocity,
                    start_tick = measureStartTick + pattern.tickPattern[i],
                    duration_tick = duration
                });
            }

            return notes;
        }
    }
}