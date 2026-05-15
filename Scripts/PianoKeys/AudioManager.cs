using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public string resourcePath = "Audio/piano_";

    private Dictionary<int, AudioClip> clips = new Dictionary<int, AudioClip>();
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent < AudioSource > ();
        if (audioSource == null) audioSource = gameObject.AddComponent < AudioSource > ();

        LoadClips();
    }

    void LoadClips()
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        for (int i = 0; i < 88; i++)
        {
            int midi = i + 21;
            int n = midi % 12;
            int octave = (midi / 12) - 1;
            string keyName = noteNames[n] + octave;
            string file = resourcePath + keyName;

            AudioClip c = Resources.Load < AudioClip > (file);
            if (c != null) clips[midi] = c;
        }

        Debug.Log($"音频加载完成：{clips.Count}/88");
    }

    public void PlayNote(int midi, float volume = 1f)
    {
        if (clips.ContainsKey(midi))
            audioSource.PlayOneShot(clips[midi], Mathf.Clamp01(volume));
        else
            Debug.LogWarning($"未找到 Midi {midi} 的音频");
    }
}