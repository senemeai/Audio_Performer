using UnityEngine;
using System.Collections.Generic;
using System.IO;
using MeltySynth;
using System.Text;  // 如果已有就不加
public class SF2AudioManager : MonoBehaviour
{
    public static SF2AudioManager Instance;

    [Header("SF2 文件路径 (放在 StreamingAssets)")]
    public string sf2FileName = "GeneralUser-GS.sf2";

    private Synthesizer synthesizer;
    private AudioSource audioSource;
    private float[] left;
    private float[] right;
    private bool isRecording = false;
    private List<float> recordingBuffer = new List<float>();
    private int recordingSampleRate = 44100;

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
        recordingSampleRate = sampleRate;
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

        // 录音：缓存最终混音输出
        if (isRecording)
        {
            lock (recordingBuffer)
            {
                for (int i = 0; i < data.Length; i++)
                    recordingBuffer.Add(data[i]);
            }
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
    public void StartRecording()
    {
        if (isRecording) return;
        lock (recordingBuffer) { recordingBuffer.Clear(); }
        isRecording = true;
        Debug.Log("[SF2AudioManager] 开始录音");
    }

    public void StopRecording(string filePath)
    {
        if (!isRecording) return;
        isRecording = false;

        float[] samplesToWrite;
        lock (recordingBuffer)
        {
            samplesToWrite = recordingBuffer.ToArray();
            recordingBuffer.Clear();
        }

        if (samplesToWrite.Length == 0)
        {
            Debug.LogWarning("[SF2AudioManager] 录音为空");
            return;
        }

        WriteWavFile(filePath, samplesToWrite, recordingSampleRate);
        Debug.Log($"[SF2AudioManager] 录音已保存: {filePath}");
    }

    public void DiscardRecording()
    {
        isRecording = false;
        lock (recordingBuffer) { recordingBuffer.Clear(); }
        Debug.Log("[SF2AudioManager] 录音已丢弃");
    }

    private void WriteWavFile(string path, float[] data, int sampleRate)
    {
        // data 是交错立体声 float，转为 16bit PCM
        byte[] pcmData = new byte[data.Length * 2];
        for (int i = 0; i < data.Length; i++)
        {
            short sample = (short)Mathf.Clamp(data[i] * 32767f, -32768f, 32767f);
            pcmData[i * 2] = (byte)(sample & 0xFF);
            pcmData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            int channels = 2;
            int bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            int totalDataLen = pcmData.Length;
            int fileSize = 36 + totalDataLen;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(totalDataLen);
            writer.Write(pcmData);
        }
    }
    void OnDestroy()
    {
        if (audioSource != null && audioSource.clip != null)
            Destroy(audioSource.clip);
    }
}