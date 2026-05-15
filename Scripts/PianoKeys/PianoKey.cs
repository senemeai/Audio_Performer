using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PianoKey : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("键位信息")]
    public int midiNote;
    public bool isBlackKey;
    public string keyName;

    [Header("UI组件 —— Text子物体")]
    public Image keyImage;
    public TextMeshProUGUI keyNameText;

    [Header("视觉反馈")]
    public Sprite normalSprite;
    public Sprite pressedSprite;

    [Header("按下位移")]
    public Vector3 normalPosition;
    public Vector3 pressedPosition;

    // 静态变量：全局鼠标按下状态 & 最后触发的键
    private static bool globalMouseDown = false;
    private static PianoKey lastPressedKey = null;

    void Reset()
    {
        keyImage = GetComponent<Image>();
        if (keyNameText == null)
            keyNameText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void SetKeyInfo(int midi, string name, bool isBlack)
    {
        midiNote = midi;
        keyName = name;
        isBlackKey = isBlack;

        if (keyNameText != null)
        {
            keyNameText.text = name;
            keyNameText.fontSize = isBlack ? 10 : 14;
            keyNameText.color = isBlack ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f);
        }
    }

    // ========== 新增：供全局调用的静态重置 ==========
    public static void ResetGlobalState()
    {
        globalMouseDown = false;
        lastPressedKey = null;
    }
    // ================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        globalMouseDown = true;
        lastPressedKey = this;
        PressVisual(true);
        AudioManager.Instance.PlayNote(midiNote);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        globalMouseDown = false;
        PressVisual(false);
        if (lastPressedKey == this) lastPressedKey = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (globalMouseDown && lastPressedKey != this)
        {
            if (lastPressedKey != null)
                lastPressedKey.PressVisual(false);

            lastPressedKey = this;
            PressVisual(true);
            AudioManager.Instance.PlayNote(midiNote);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (globalMouseDown && lastPressedKey == this)
        {
            PressVisual(false);
            lastPressedKey = null;
        }
    }

    public void PressVisual(bool pressed)
    {
        if (keyImage != null && normalSprite != null && pressedSprite != null)
            keyImage.sprite = pressed ? pressedSprite : normalSprite;
        transform.localPosition = pressed ? pressedPosition : normalPosition;
    }

    public void TriggerAutoPlay(float duration)
    {
        PressVisual(true);
        AudioManager.Instance.PlayNote(midiNote);
        Invoke(nameof(ReleaseAutoPlay), duration);
    }

    void ReleaseAutoPlay()
    {
        PressVisual(false);
    }
}