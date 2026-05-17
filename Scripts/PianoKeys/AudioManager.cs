using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频路径前缀")]
    public string baseResourcePath = "Audio/";
    private string currentInstrumentPrefix = "piano";
    private Dictionary<int, AudioClip> clips = new Dictionary<int, AudioClip>();
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent < AudioSource > ();
        if (audioSource == null) audioSource = gameObject.AddComponent < AudioSource > ();

        // 读取上次保存的乐器偏好
        string saved = PlayerPrefs.GetString("MusicPlayer_LastInstrument", "piano");
        SwitchInstrument(saved, false);
    }

    /// <summary>
    /// 切换乐器音色
    /// </summary>
    public void SwitchInstrument(string instrumentPrefix, bool savePreference = true)
    {
        currentInstrumentPrefix = instrumentPrefix;
        LoadClips();

        if (savePreference)
        {
            PlayerPrefs.SetString("MusicPlayer_LastInstrument", instrumentPrefix);
            PlayerPrefs.Save();
        }

        Debug.Log($"[乐器切换] 当前音色: {instrumentPrefix}，加载 {clips.Count}/88");
    }

    public string GetCurrentInstrumentPrefix()
    {
        return currentInstrumentPrefix;
    }

    void LoadClips()
    {
        clips.Clear();
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int fallbackCount = 0;

        for (int i = 0; i < 88; i++)
        {
            int midi = i + 21;
            int n = midi % 12;
            int octave = (midi / 12) - 1;
            string keyName = noteNames[n] + octave;

            // 先尝试加载目标乐器
            string targetPath = baseResourcePath + currentInstrumentPrefix + "_" + keyName;
            AudioClip c = Resources.Load < AudioClip > (targetPath);

            // 如果缺失，fallback 到 piano
            if (c == null && currentInstrumentPrefix != "piano")
            {
                string fallbackPath = baseResourcePath + "piano_" + keyName;
                c = Resources.Load < AudioClip > (fallbackPath);
                if (c != null) fallbackCount++;
            }

            if (c != null)
                clips[midi] = c;
        }

        if (fallbackCount > 0 && currentInstrumentPrefix != "piano")
        {
            Debug.LogWarning($"[乐器切换] {currentInstrumentPrefix} 缺失 {fallbackCount} 个音频，已自动 fallback 到 piano。" +
                $"\n如需完整体验，请将 {currentInstrumentPrefix}_A0.mp3 ~ {currentInstrumentPrefix}_C8.mp3 放入 Resources/Audio/");
        }
    }

    public void PlayNote(int midi, float volume = 1f)
    {
        if (clips.ContainsKey(midi))
            audioSource.PlayOneShot(clips[midi], Mathf.Clamp01(volume));
        else
            Debug.LogWarning($"未找到 Midi {midi} 的音频（当前乐器: {currentInstrumentPrefix}）");
    }
}