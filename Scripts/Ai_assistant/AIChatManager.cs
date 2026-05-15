using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIChatManager : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject chatPanel;
    public TextMeshProUGUI aiText;
    public TMP_InputField inputField;
    public Button btnSend;
    public Button btnClose;
    public TMP_Dropdown styleDropdown;

    [Header("控制按钮")]
    public Button btnGenerate;
    public Button btnPlay;
    public Button btnPause;
    public Button btnContinueGen;

    [Header("API 配置")]
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey = "sk-your-key-here";
    public string modelName = "deepseek-chat";

    private string currentStyle = "流行抒情";
    private string currentKeySignature = "C";
    private StringBuilder chatHistory = new StringBuilder();

    // 缓存与模式
    private string pendingJson = "";
    private bool isNewRequest = true;
    private List<NoteEvent> lastSegmentNotes = new List<NoteEvent>();
    private int lastBpm = 80;

    // 新增：解析用户输入中的音乐要求
    private string userStartNote = "";      // 用户指定的起始音，如"D1"
    private string parsedKeySignature = ""; // 用户输入中解析出的调式

    void Start()
    {
        chatPanel.SetActive(false);
        SetupStyleDropdown();
        SetupButtons();
        SetControlButtonsActive(false);
        btnGenerate.gameObject.SetActive(false);

        if (ScorePlayer.Instance != null)
            ScorePlayer.Instance.OnPlaybackFinished += OnPlaybackFinished;
    }

    void SetupStyleDropdown()
    {
        styleDropdown.ClearOptions();
        styleDropdown.AddOptions(new List<string>(StylePresets.Names));
        styleDropdown.onValueChanged.AddListener(OnStyleChanged);
        OnStyleChanged(0);
    }

    void OnStyleChanged(int index)
    {
        currentStyle = StylePresets.Names[index];
    }

    void SetupButtons()
    {
        btnSend.onClick.AddListener(OnSend);
        btnClose.onClick.AddListener(() => chatPanel.SetActive(false));
        btnGenerate.onClick.AddListener(OnGenerateScore);
        btnPlay.onClick.AddListener(OnPlay);
        btnPause.onClick.AddListener(OnPause);
        btnContinueGen.onClick.AddListener(OnContinueGenerate);
    }

    public void OpenChat()
    {
        chatPanel.SetActive(true);
        inputField.text = "";
        if (string.IsNullOrEmpty(aiText.text))
            aiText.text = "AI：请选择风格并描述你想要的音乐（如：来首悲伤的流行曲）。\n\n";
    }

    // ========== 发送新请求（覆盖旧乐谱）==========
    void OnSend()
    {
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // 解析用户输入中的调式、起始音等要求
        ParseUserRequirements(msg);

        AppendChat("你", msg, "#00FF00");
        inputField.text = "";

        isNewRequest = true;
        pendingJson = "";

        AppendChat("AI", "🎵 正在为您编曲，请稍候...", "#FFCC66");
        StartCoroutine(RequestAI(msg));
    }

    // ========== 继续生成（拼接旧乐谱）==========
    void OnContinueGenerate()
    {
        isNewRequest = false;
        int bars = ScorePlayer.Instance.TotalBarsAccumulated;

        // 获取上一段结尾上下文
        string lastContext = ScorePlayer.Instance.GetLastNotesContext(4);

        string prompt = $"【续写任务】\n" +
            $"上一段已生成 {bars} 小节。\n" +
            $"上一段结尾音符：{lastContext}\n" +
            $"必须保持同样速度 BPM={lastBpm}，同样调式 {currentKeySignature} 大调，风格 {currentStyle}。\n" +
            $"严格要求：\n" +
            $"1. 旋律必须紧接上一段结尾自然延续，像同一首曲子的无缝延续\n" +
            $"2. 第一个音符的 start_tick 必须是 0（紧接上一段末尾，不要留空白）\n" +
            $"3. 直接输出 JSON，不要任何文字说明";

        AppendChat("系统", "🎵 正在续写下一段乐谱...", "#66FFFF");
        StartCoroutine(RequestAI(prompt));
    }

    // ========== 点击"生成此乐谱"：加载缓存的 JSON ==========
    void OnGenerateScore()
    {
        if (!string.IsNullOrEmpty(pendingJson))
        {
            ProcessScoreJson(pendingJson, true); // true = 清空旧乐谱，加载新的
            pendingJson = "";
        }
        else
        {
            AppendChat("系统", "暂无待加载的乐谱，请先在输入框描述您想要的音乐。", "#FF6666");
        }
    }

    // ========== 解析用户输入中的音乐要求 ==========
    void ParseUserRequirements(string msg)
    {
        userStartNote = "";
        parsedKeySignature = "";

        // 1. 匹配调式：D大调、D小调、D#大调、Bb小调
        Match keyMatch = Regex.Match(msg, @"\b([A-G][#b]?)(大调|小调)\b");
        if (keyMatch.Success)
        {
            parsedKeySignature = keyMatch.Groups[1].Value;
            if (keyMatch.Groups[2].Value == "小调")
                currentKeySignature = parsedKeySignature + "m";
            else
                currentKeySignature = parsedKeySignature;
        }

        // 2. 匹配起始音：以D1开始、从C4起始、用F#5开头、以D1调为开始
        Match startMatch = Regex.Match(msg, @"(?:以|从|用)\s*([A-G][#b]?)(\d)\s*(?:调|音|键)?(?:为)?(?:开始|起始|开头)");
        if (startMatch.Success)
        {
            userStartNote = startMatch.Groups[1].Value + startMatch.Groups[2].Value; // 如"D1"
        }
    }

    // 将音名转换为 key_number（如 "D1" → 6，"C4" → 40，"F#5" → 74）
    int GetKeyNumberFromName(string noteName)
    {
        string namePart = Regex.Match(noteName, @"[A-G][#b]?").Value;
        string numPart = Regex.Match(noteName, @"\d+").Value;
        if (!int.TryParse(numPart, out int octave)) return 40; // 默认中央C

        string[] semis = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int semitone = -1;
        for (int i = 0; i < semis.Length; i++)
            if (semis[i] == namePart) { semitone = i; break; }

        if (semitone < 0) return 40;

        int midi = (octave + 1) * 12 + semitone;
        return Mathf.Clamp(midi - 20, 1, 88);
    }

    IEnumerator RequestAI(string userMessage)
    {
        string systemPrompt = BuildSystemPrompt();
        string body = BuildRequestBody(systemPrompt, userMessage);

        UnityWebRequest req = new UnityWebRequest(apiUrl, "POST");
        byte[] raw = System.Text.Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler.text;
            string content = ExtractContent(resp);
            string json = ExtractJson(content);

            if (!string.IsNullOrEmpty(json))
            {
                if (isNewRequest)
                {
                    // 新请求：缓存 JSON，提示用户点击"生成此乐谱"
                    pendingJson = json;
                    AppendChat("系统", "✅ 乐谱已生成完毕！请点击【生成此乐谱】按钮加载到播放器，然后点击【演奏】试听。", "#66FFFF");
                    btnGenerate.gameObject.SetActive(true);
                    SetControlButtonsActive(false);
                }
                else
                {
                    // 继续生成：直接解析并拼接
                    ProcessScoreJson(json, false); // false = 不清空，拼接到后面
                }
            }
            else
            {
                AppendChat("AI", content, "#FFCC66");
                btnGenerate.gameObject.SetActive(true);
            }
        }
        else
        {
            AppendChat("系统", "请求失败：" + req.error, "#FF6666");
        }

        ScrollToBottom();
    }

    void ProcessScoreJson(string json, bool clearFirst)
    {
        ScoreDataRaw raw = JsonUtility.FromJson < ScoreDataRaw > (json);
        if (raw == null || raw.notes == null || raw.notes.Length == 0)
        {
            AppendChat("系统", "乐谱解析失败或为空", "#FF6666");
            return;
        }

        ScoreData data = new ScoreData
        {
            bpm = raw.bpm,
            key_signature = raw.key_signature,
            style = raw.style,
            total_bars = raw.total_bars,
            notes = raw.notes
        };

        data = ScoreValidator.Validate(data, currentKeySignature);
        if (data.notes.Length == 0)
        {
            AppendChat("系统", "校验后无有效音符", "#FF6666");
            return;
        }

        // 保存原始音符和 BPM，供续写上下文用
        lastSegmentNotes = new List< NoteEvent > (data.notes);
        lastBpm = data.bpm;

        ScorePlayer.Instance.AppendScore(data, clearFirst);

        string actionText = clearFirst ? "已加载" : "已续接";
        AppendChat("系统", $"乐谱{actionText}！共{data.notes.Length}个音符，{data.total_bars}小节。点击演奏试听。", "#66FFFF");

        btnGenerate.gameObject.SetActive(false);
        SetControlButtonsActive(true);
    }

    // ========== 演奏控制 ==========
    void OnPlay()
    {
        if (ScorePlayer.Instance.IsPlaying)
        {
            ScorePlayer.Instance.Stop();
            UpdatePlayText(false);
        }
        else
        {
            ScorePlayer.Instance.Play();
            UpdatePlayText(true);
        }
        UpdatePauseText(false);
    }

    void OnPause()
    {
        if (ScorePlayer.Instance.IsPaused)
        {
            ScorePlayer.Instance.Resume();
            UpdatePauseText(false);
        }
        else
        {
            ScorePlayer.Instance.Pause();
            UpdatePauseText(true);
        }
    }

    void OnPlaybackFinished()
    {
        UpdatePlayText(false);
        UpdatePauseText(false);
    }

    void UpdatePlayText(bool playing)
    {
        var txt = btnPlay.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = playing ? "■ 停止" : "▶ 演奏";
    }

    void UpdatePauseText(bool paused)
    {
        var txt = btnPause.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = paused ? "▶ 继续" : "⏸ 暂停";
    }

    void SetControlButtonsActive(bool active)
    {
        btnPlay.gameObject.SetActive(active);
        btnPause.gameObject.SetActive(active);
        btnContinueGen.gameObject.SetActive(active);
    }

    // ========== 工具方法 ==========
    string BuildSystemPrompt()
    {
        var preset = StylePresets.GetPreset(currentStyle);

        // 根据用户输入动态插入强制要求
        string userReqSection = "";
        if (!string.IsNullOrEmpty(parsedKeySignature))
            userReqSection += $"【用户指定调式】{currentKeySignature}。必须严格使用此调式，禁止擅自改回C大调。\n";
        if (!string.IsNullOrEmpty(userStartNote))
            userReqSection += $"【用户指定起始音】第一个音符必须是{userStartNote}（key_number={GetKeyNumberFromName(userStartNote)}），禁止从其他音开始。\n";

        return "你是一位专业的88键钢琴编曲AI。\n\n" +
               "【输出格式】\n" +
               "1. 只输出```json代码块，块外不要有任何文字\n" +
               "2. 字段：key_number(1-88), velocity(1-127), start_tick(int,≥0), duration_tick(int,≥1)\n" +
               "3. 必须包含：bpm, key_signature, total_bars\n" +
               "4. 1 tick = 1个十六分音符，1小节 = 16 tick\n" +
               "5. 最多64个Note（约4小节）\n\n" +

               "【和声与调式】\n" +
               userReqSection +
               $"当前调式：{currentKeySignature}大调\n" +
               $"可选和弦进行（每段任选一套，可混搭）：{preset.chordProgression}\n" +
               "- 右手旋律优先使用和弦内音（根音、三音、五音、七音）\n" +
               "- 允许使用经过音：相邻白键之间的半音（如 D→E 之间用 D#），但必须是短音（duration_tick=1）且弱奏（velocity≤50）\n" +
               "- 经过音每小节最多出现2次，像\"滑过\"一样轻触即可\n\n" +

               "【旋律自由度】\n" +
               "1. 动机发展：先设计一个2~4个音的短动机，然后在后续小节对其进行变化，而不是每次都写全新随机音\n" +
               "   - 模进：动机整体移高或移低若干音程\n" +
               "   - 节奏变化：把长音拆成短音，或把短音合并成长音\n" +
               "   - 倒影：把动机音高上下翻转\n" +
               "2. 弱起与留白：旋律可以从弱拍进入（start_tick=2/4/6），不要每小节第1 tick都必须有音\n" +
               "   - 允许整小节只有左手伴奏，右手旋律休止\n" +
               "   - 留白让音乐有呼吸感\n" +
               "3. 旋律线不要太平：允许跳进（如从C4跳到G4），不要总是一个音阶一个音阶爬\n\n" +

               "【声部分配】\n" +
               "1. 右手旋律（key_number ≥ 44）：velocity 70-110\n" +
               "   - 大部分时间同一tick只有1个音，但允许偶尔的双音（如三度、六度）\n" +
               "   - 旋律音的 duration_tick 可以变化：1（短促）、2（八分）、4（四分）、6（附点）\n" +
               "2. 左手伴奏（key_number ≤ 43）：velocity 40-70\n" +
               "   - 柱式和弦：多个音写在同一 start_tick\n" +
               "   - 分解和弦/琶音：同一和弦内 start_tick 依次递增 0,1,2,3\n" +
               "   - 低音长音可以跨越多个小节（duration_tick=16或更长），铺底用\n" +
               "3. 双手配合：\n" +
               "   - 强拍（tick=0）可以只有左手根音，右手旋律延后2~4 tick进入\n" +
               "   - 同一tick最多5个音\n\n" +

               "【节奏与休止】\n" +
               "- 不要填满所有tick，适当留白\n" +
               "- 允许附点节奏（duration_tick=3, 6, 12）\n" +
               "- 允许切分：强拍休止，弱拍进音\n\n" +

               $"【风格参数】{currentStyle}，BPM {preset.bpmMin}-{preset.bpmMax}\n" +
               $"【力度轮廓】{preset.dynamicsProfile}\n\n" +

               "【禁止】\n" +
               "- 禁止key_number超出1-88\n" +
               "- 禁止velocity为0或>127\n" +
               "- 禁止duration_tick=0\n" +
               "- 禁止连续4小节使用完全相同的旋律，必须有变化";
    }

    string BuildRequestBody(string system, string user)
    {
        string sys = EscapeJson(system);
        string usr = EscapeJson(user);
        return $"{{\"model\":\"{modelName}\",\"temperature\":0.3,\"messages\":[{{\"role\":\"system\",\"content\":\"{sys}\"}},{{\"role\":\"user\",\"content\":\"{usr}\"}}]}}";
    }

    string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    // 安全提取 content，避免正则回溯卡死
    string ExtractContent(string raw)
    {
        int start = raw.IndexOf("\"content\":\"");
        if (start < 0) return raw;
        start += 11;

        StringBuilder sb = new StringBuilder();
        for (int i = start; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c == '\\' && i + 1 < raw.Length)
            {
                char next = raw[i + 1];
                if (next == 'n') sb.Append('\n');
                else if (next == 'r') sb.Append('\r');
                else if (next == 't') sb.Append('\t');
                else if (next == '"') sb.Append('"');
                else if (next == '\\') sb.Append('\\');
                else sb.Append(next);
                i++;
            }
            else if (c == '"' && (i + 1 >= raw.Length || raw[i + 1] == ',' || raw[i + 1] == '}'))
            {
                return sb.ToString();
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    string ExtractJson(string raw)
    {
        int s = raw.IndexOf("```json");
        if (s >= 0)
        {
            s += 7;
            int e = raw.IndexOf("```", s);
            if (e > s) return raw.Substring(s, e - s).Trim();
        }
        s = raw.IndexOf("{");
        int e2 = raw.LastIndexOf("}");
        if (s >= 0 && e2 > s) return raw.Substring(s, e2 - s + 1);
        return "";
    }

    void AppendChat(string role, string msg, string color)
    {
        chatHistory.AppendLine($"<color={color}>{role}：</color>{msg}\n");
        aiText.text = chatHistory.ToString();
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
    }
}