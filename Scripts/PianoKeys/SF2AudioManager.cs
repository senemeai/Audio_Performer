using UnityEngine;
using System.Collections.Generic;
using System.IO;
using MeltySynth;

public class SF2AudioManager : MonoBehaviour
{
    public static SF2AudioManager Instance;

    [Header("SF2 文件路径 (放在 StreamingAssets)")]
    public string sf2FileName = "GeneralUser-GS.sf2";

    private Synthesizer synthesizer;
    private AudioSource audioSource;
    private float[] left;
    private float[] right;

    // GM 音色号映射表（修正版，补全缺失 key）
    public static readonly Dictionary<string, int> InstrumentMap = new Dictionary<string, int>
    {
        {"treble_upright", 1},   // 高音立式钢琴
        {"piano", 0},            // 原声钢琴
        {"grand_piano", 0},      // 大钢琴
        {"bright_piano", 1},     // 亮音大钢琴
        {"electric_grand", 2},   // 电钢琴
        {"honky_tonk", 3},       // 酒吧钢琴
        {"electric", 4},         // 电钢琴1
        {"electric2", 5},        // 电钢琴2
        {"celesta", 8},          // 钢片琴
        {"glockenspiel", 9},     // 钟琴
        {"music_box", 10},       // 八音盒
        {"vibraphone", 11},      // 电颤琴
        {"xylophone", 13},       // 木琴
        {"dulcimer", 15},        // 扬琴
        {"harmonica", 22},       // 口琴
        {"guitar_nylon", 24},    // 尼龙弦吉他
        {"guitar_steel", 25},    // 钢弦吉他
        {"guitar_jazz", 26},     // 爵士乐电吉他
        {"guitar_clean", 27},    // 清音电吉他
        {"acoustic_bass", 32},   // 原声贝斯
        {"violin", 40},          // 小提琴
        {"harp", 46},            // 竖琴
        {"soprano_sax", 64},     // 高音萨克斯
        {"piccolo", 72},         // 短笛
        {"koto", 107},           // 筝
        {"shanai", 111},         // 唢呐
        {"tinkle_bell", 112},    // 铃铛
        {"synth_drum", 118},     // 合成鼓
        {"bird", 123}            // 鸟鸣声
    };

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        string path = Path.Combine(Application.dataPath, "Resources", "StreamingAssets", sf2FileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"[SF2AudioManager] 找不到 SF2 文件: {path}\n请把 {sf2FileName} 放进 Assets/Resources/StreamingAssets/");
            return;
        }

        int sampleRate = AudioSettings.outputSampleRate;
        synthesizer = new Synthesizer(path, sampleRate);

        // 默认钢琴
        synthesizer.ProcessMidiMessage(0, 0xC0, 0, 0);

        // 改用 OnAudioFilterRead 降低延迟
        audioSource.clip = AudioClip.Create("SF2Stream", 1024, 2, sampleRate, false);
        audioSource.loop = true;
        audioSource.Play();

        left = new float[1024];
        right = new float[1024];

        Debug.Log($"[SF2AudioManager] 加载成功，采样率: {sampleRate}");
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (synthesizer == null)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        int samples = data.Length / channels;
        if (left.Length < samples)
        {
            left = new float[samples];
            right = new float[samples];
        }

        synthesizer.Render(
            new System.Span<float>(left, 0, samples),
            new System.Span<float>(right, 0, samples)
        );

        for (int i = 0; i < samples; i++)
        {
            data[i * channels] = left[i];
            if (channels > 1)
                data[i * channels + 1] = right[i];
        }
    }

    public void PlayNote(int midi, float volume = 1f)
    {
        if (synthesizer == null) return;
        int velocity = Mathf.Clamp((int)(volume * 127f), 1, 127);
        synthesizer.NoteOn(0, midi, velocity);
    }

    public void StopNote(int midi)
    {
        if (synthesizer == null) return;
        synthesizer.NoteOff(0, midi);
    }
    public void StopNoteImmediate(int midi)
    {
        if (synthesizer == null) return;
        synthesizer.NoteOffImmediate(0, midi);
    }
    public void StopAllNotes()
    {
        if (synthesizer == null) return;
        synthesizer.NoteOffAll(false); // false = 自然衰减，不立即Kill
    }
    public void StopAllNotesImmediate()
    {
        if (synthesizer == null) return;
        synthesizer.NoteOffAll(0, true);
    }
    public void SwitchInstrument(string prefix)
    {
        if (synthesizer == null) return;
        if (InstrumentMap.TryGetValue(prefix, out int gm))
        {
            synthesizer.ProcessMidiMessage(0, 0xC0, gm, 0);
            Debug.Log($"[SF2] 切换乐器成功: {prefix} → GM{gm}");
        }
        else
        {
            synthesizer.ProcessMidiMessage(0, 0xC0, 0, 0);
            Debug.LogWarning($"[SF2] 未知乐器 '{prefix}'，fallback 到钢琴 (GM0)。请在 InstrumentMap 里添加映射！");
        }
    }

    public string GetCurrentInstrumentPrefix()
    {
        return "piano";
    }

    void OnDestroy()
    {
        if (audioSource != null && audioSource.clip != null)
            Destroy(audioSource.clip);
    }
}