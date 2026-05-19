using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordingUIController : MonoBehaviour
{
    [Header("UI 引用")]
    public Button btnRecordToggle;
    public Image imgRecordDot;
    public TextMeshProUGUI textRecordLabel;
    public TextMeshProUGUI textRecordStatus;
    public TextMeshProUGUI textRecordTimer;

    private bool isRecording = false;
    private float recordStartTime;
    private Coroutine blinkCoroutine;
    private Coroutine timerCoroutine;

    void Start()
    {
        btnRecordToggle.onClick.AddListener(OnRecordToggle);
        UpdateUIIdle();
    }

    void OnRecordToggle()
    {
        if (!isRecording)
        {
            SF2AudioManager.Instance.StartRecording();
            isRecording = true;
            recordStartTime = Time.time;

            textRecordLabel.text = "停止";
            textRecordStatus.text = "录音中...";
            blinkCoroutine = StartCoroutine(BlinkDot());
            timerCoroutine = StartCoroutine(UpdateTimer());
        }
        else
        {
            StopCoroutine(blinkCoroutine);
            StopCoroutine(timerCoroutine);

            textRecordLabel.text = "录音";
            textRecordStatus.text = "保存中...";
            imgRecordDot.color = Color.gray;

            string path = FileDialogHelper.SaveFile("保存录音", "WAV文件|*.wav", "录音.wav");
            if (!string.IsNullOrEmpty(path))
            {
                SF2AudioManager.Instance.StopRecording(path);
                textRecordStatus.text = "保存成功";
            }
            else
            {
                SF2AudioManager.Instance.DiscardRecording();
                textRecordStatus.text = "已取消";
            }

            isRecording = false;
            textRecordTimer.text = "--:--";
        }
    }

    IEnumerator BlinkDot()
    {
        while (true)
        {
            imgRecordDot.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            imgRecordDot.color = new Color(1f, 0f, 0f, 0.3f);
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator UpdateTimer()
    {
        while (true)
        {
            float elapsed = Time.time - recordStartTime;
            int minutes = (int)(elapsed / 60);
            int seconds = (int)(elapsed % 60);
            textRecordTimer.text = $"{minutes:D2}:{seconds:D2}";
            yield return new WaitForSeconds(0.1f);
        }
    }

    void UpdateUIIdle()
    {
        textRecordLabel.text = "录音";
        textRecordStatus.text = "就绪";
        textRecordTimer.text = "--:--";
        imgRecordDot.color = Color.gray;
    }
}