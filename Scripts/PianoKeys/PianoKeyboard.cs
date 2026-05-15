using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PianoKeyboard : MonoBehaviour
{
    [Header("预制体 —— 拖入上面做好的")]
    public GameObject whiteKeyPrefab;
    public GameObject blackKeyPrefab;

    [Header("容器")]
    public RectTransform whiteKeysContainer;
    public RectTransform blackKeysContainer;

    [Header("布局参数")]
    public float pianoWidth = 1600f;
    public float whiteKeyHeight = 220f;

    private PianoKey[] keys = new PianoKey[88];
    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    void Start()
    {
        GenerateKeys();
    }
    // ========== 新增：全局鼠标抬起监听 ==========
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ResetAllKeys();
        }
    }

    /// <summary>
    /// 强制恢复所有88个键到未按下状态
    /// </summary>
    public void ResetAllKeys()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null)
                keys[i].PressVisual(false);
        }
        PianoKey.ResetGlobalState();
    }
    // ============================================
    void GenerateKeys()
    {
        float whiteWidth = pianoWidth / 52f;
        float blackWidth = whiteWidth * 0.65f;
        float blackHeight = whiteKeyHeight * 0.65f;
        int whiteIndex = 0;

        for (int i = 0; i < 88; i++)
        {
            int midi = i + 21;
            int noteIndex = midi % 12;
            int octave = (midi / 12) - 1;
            string keyName = noteNames[noteIndex] + octave;
            bool isBlack = keyName.Contains("#");

            if (!isBlack)
            {
                GameObject go = Instantiate(whiteKeyPrefab, whiteKeysContainer);
                RectTransform rt = go.GetComponent < RectTransform > ();
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.sizeDelta = new Vector2(whiteWidth, whiteKeyHeight);
                rt.anchoredPosition = new Vector2(whiteIndex * whiteWidth, 0);

                PianoKey pk = go.GetComponent<PianoKey>();
                pk.SetKeyInfo(midi, keyName, false);
                pk.normalPosition = rt.localPosition;
                pk.pressedPosition = rt.localPosition + new Vector3(0, -5f, 0);

                keys[i] = pk;
                whiteIndex++;
            }
            else
            {
                float x = whiteIndex * whiteWidth - blackWidth * 0.5f;

                GameObject go = Instantiate(blackKeyPrefab, blackKeysContainer);
                RectTransform rt = go.GetComponent < RectTransform > ();
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.sizeDelta = new Vector2(blackWidth, blackHeight);
                rt.anchoredPosition = new Vector2(x, 0);

                PianoKey pk = go.GetComponent<PianoKey>();
                pk.SetKeyInfo(midi, keyName, true);
                pk.normalPosition = rt.localPosition;
                pk.pressedPosition = rt.localPosition + new Vector3(0, -3f, 0);

                keys[i] = pk;
            }
        }
    }

    void AddPointerEvents(GameObject target, int keyIndex)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((e) => {
            keys[keyIndex].PressVisual(true);
            AudioManager.Instance.PlayNote(keys[keyIndex].midiNote);
        });
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener((e) => {
            keys[keyIndex].PressVisual(false);
        });
        trigger.triggers.Add(up);
    }

    public PianoKey GetKeyByMidi(int midi)
    {
        int index = midi - 21;
        if (index >= 0 && index < 88) return keys[index];
        return null;
    }
}