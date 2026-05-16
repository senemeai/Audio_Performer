using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ScoreItemUI : MonoBehaviour
{
    public TextMeshProUGUI textName;
    public TextMeshProUGUI textPath;
    public TextMeshProUGUI textDate;
    public TextMeshProUGUI textStyle;

    private ScoreIndexEntry entry;
    private Action<ScoreItemUI> onClick;
    private Action<ScoreItemUI> onDoubleClick;
    private float lastClickTime;

    public ScoreIndexEntry Entry => entry;

    public void Setup(ScoreIndexEntry e, Action<ScoreItemUI> click, Action<ScoreItemUI> doubleClick)
    {
        entry = e;
        onClick = click;
        onDoubleClick = doubleClick;
        Refresh();

        var btn = GetComponent < Button > ();
        if (btn != null) btn.onClick.AddListener(OnClick);
    }

    void Refresh()
    {
        textName.text = entry.displayName;
        textPath.text = System.IO.Path.GetDirectoryName(entry.filePath);
        textDate.text = DateTimeOffset.FromUnixTimeSeconds(entry.saveTime).ToString("yyyy-MM-dd HH:mm");
        textStyle.text = entry.style;
    }

    void OnClick()
    {
        float now = Time.time;
        if (now - lastClickTime < 0.3f)
            onDoubleClick?.Invoke(this);
        else
            onClick?.Invoke(this);
        lastClickTime = now;
    }
}