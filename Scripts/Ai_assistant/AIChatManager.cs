using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public Button btnSaveScore;
    public Button btnImportScore;

    [Header("API 配置")]
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey = "sk-your-key-here";
    public string modelName = "deepseek-chat";

    private string currentStyle = "Pop"; // 内部英文Key
    private string currentKeySignature = "C";
    private StringBuilder chatHistory = new StringBuilder();

    private string pendingJson = "";
    private bool isNewRequest = true;
    private List<NoteEvent> lastSegmentNotes = new List<NoteEvent>();
    private int lastBpm = 80;

    private List<NoteEvent> sessionExportNotes = new List<NoteEvent>();
    private int sessionTotalBars = 0;
    private int sessionBpm = 80;

    private string userStartNote = "";
    private string parsedKeySignature = "";

    void Start()
    {
        chatPanel.SetActive(false);
        SetupStyleDropdown();
        SetupButtons();

        // 关键修改：导入按钮始终显示，其他乐谱控制按钮默认隐藏
        btnImportScore.gameObject.SetActive(true);
        SetMusicControlButtonsActive(false);
        btnGenerate.gameObject.SetActive(false);

        if (ScorePlayer.Instance != null)
            ScorePlayer.Instance.OnPlaybackFinished += OnPlaybackFinished;
    }

    void SetupStyleDropdown()
    {
        styleDropdown.ClearOptions();
        styleDropdown.AddOptions(new System.Collections.Generic.List<string>(StylePresets.DisplayNames));
        styleDropdown.onValueChanged.AddListener(OnStyleChanged);
        OnStyleChanged(0);
    }

    void OnStyleChanged(int index)
    {
        currentStyle = StylePresets.Names[index];
    }

    // 获取当前风格的中文显示名（用于保存到索引库）
    string GetCurrentStyleDisplayName()
    {
        int idx = System.Array.IndexOf(StylePresets.Names, currentStyle);
        return idx >= 0 ? StylePresets.DisplayNames[idx] : currentStyle;
    }

    void SetupButtons()
    {
        btnSend.onClick.AddListener(OnSend);
        btnClose.onClick.AddListener(() => chatPanel.SetActive(false));
        btnGenerate.onClick.AddListener(OnGenerateScore);
        btnPlay.onClick.AddListener(OnPlay);
        btnPause.onClick.AddListener(OnPause);
        btnContinueGen.onClick.AddListener(OnContinueGenerate);
        btnSaveScore.onClick.AddListener(OnSaveScore);
        btnImportScore.onClick.AddListener(OnImportScore);
    }

    public void OpenChat()
    {
        chatPanel.SetActive(true);
        inputField.text = "";
        if (string.IsNullOrEmpty(aiText.text))
            aiText.text = "AI：请选择风格并描述你想要的音乐（如：来首悲伤的流行曲）。\n\n";
    }

    void OnSend()
    {
        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        ParseUserRequirements(msg);
        AppendChat("你", msg, "#00FF00");
        inputField.text = "";

        isNewRequest = true;
        pendingJson = "";
        sessionExportNotes.Clear();
        sessionTotalBars = 0;

        AppendChat("AI", " 正在为您编曲，请稍候...", "#FFCC66");
        StartCoroutine(RequestAI(msg));
    }

    void OnContinueGenerate()
    {
        isNewRequest = false;
        int bars = ScorePlayer.Instance.TotalBarsAccumulated;
        string lastContext = ScorePlayer.Instance.GetLastNotesContext(4);

        string prompt = $"【续写任务】\n" +
            $"上一段已生成 {bars} 小节。\n" +
            $"上一段结尾音符：{lastContext}\n" +
            $"必须保持同样速度 BPM={lastBpm}，同样调式 {currentKeySignature} 大调，风格 {GetCurrentStyleDisplayName()}。\n" +
            $"严格要求：\n" +
            $"1. 旋律必须紧接上一段结尾自然延续\n" +
            $"2. 第一个音符的 start_tick 必须是 0\n" +
            $"3. 直接输出 JSON，不要任何文字说明";

        AppendChat("系统", " 正在续写下一段乐谱...", "#66FFFF");
        StartCoroutine(RequestAI(prompt));
    }

    void OnGenerateScore()
    {
        if (!string.IsNullOrEmpty(pendingJson))
        {
            ProcessScoreJson(pendingJson, true);
            pendingJson = "";
        }
        else
        {
            AppendChat("系统", "暂无待加载的乐谱，请先在输入框描述您想要的音乐。", "#FF6666");
        }
    }

    void ParseUserRequirements(string msg)
    {
        userStartNote = "";
        parsedKeySignature = "";

        Match keyMatch = Regex.Match(msg, @"\b([A-G][#b]?)(大调|小调)\b");
        if (keyMatch.Success)
        {
            parsedKeySignature = keyMatch.Groups[1].Value;
            currentKeySignature = keyMatch.Groups[2].Value == "小调" ? parsedKeySignature + "m" : parsedKeySignature;
        }

        Match startMatch = Regex.Match(msg, @"(?:以|从|用)\s*([A-G][#b]?)(\d)\s*(?:调|音|键)?(?:为)?(?:开始|起始|开头)");
        if (startMatch.Success)
            userStartNote = startMatch.Groups[1].Value + startMatch.Groups[2].Value;
    }

    int GetKeyNumberFromName(string noteName)
    {
        string namePart = Regex.Match(noteName, @"[A-G][#b]?").Value;
        string numPart = Regex.Match(noteName, @"\d+").Value;
        if (!int.TryParse(numPart, out int octave)) return 40;

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
        byte[] raw = Encoding.UTF8.GetBytes(body);
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
                    pendingJson = json;
                    AppendChat("系统", "乐谱已生成完毕！请点击【生成此乐谱】按钮加载到播放器，然后点击【演奏】试听。", "#66FFFF");
                    btnGenerate.gameObject.SetActive(true);
                    SetMusicControlButtonsActive(false);
                }
                else
                {
                    ProcessScoreJson(json, false);
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

        if (clearFirst)
        {
            sessionExportNotes.Clear();
            sessionTotalBars = 0;
            sessionBpm = data.bpm;
        }
        int tickOffset = sessionTotalBars * 16;
        foreach (var note in data.notes)
        {
            sessionExportNotes.Add(new NoteEvent
            {
                key_number = note.key_number,
                velocity = note.velocity,
                start_tick = note.start_tick + tickOffset,
                duration_tick = note.duration_tick
            });
        }
        sessionTotalBars += data.total_bars;

        lastSegmentNotes = new List< NoteEvent > (data.notes);
        lastBpm = data.bpm;

        ScorePlayer.Instance.AppendScore(data, clearFirst);

        string actionText = clearFirst ? "已加载" : "已续接";
        AppendChat("系统", $"乐谱{actionText}！共{data.notes.Length}个音符，{data.total_bars}小节。点击演奏试听。", "#66FFFF");

        btnGenerate.gameObject.SetActive(false);
        SetMusicControlButtonsActive(true);
    }

    void OnSaveScore()
    {
        if (sessionExportNotes.Count == 0)
        {
            AppendChat("系统", "当前没有可保存的乐谱，请先生成。", "#FF6666");
            return;
        }

        string path = FileDialogHelper.SaveFile("保存乐谱", "JSON文件|*.json", "乐谱.json");
        if (string.IsNullOrEmpty(path)) return;

        var exportData = new ScoreData
        {
            bpm = sessionBpm,
            key_signature = currentKeySignature,
            style = currentStyle,
            total_bars = sessionTotalBars,
            notes = sessionExportNotes.ToArray()
        };
        string json = JsonUtility.ToJson(exportData);
        File.WriteAllText(path, json);

        var entry = new ScoreIndexEntry
        {
            displayName = Path.GetFileNameWithoutExtension(path),
            filePath = path,
            style = GetCurrentStyleDisplayName(), // 存中文，方便筛选
            totalBars = sessionTotalBars,
            saveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            remark = ""
        };
        AddScoreIndex(entry);

        AppendChat("系统", $"乐谱已保存到：{path}", "#66FFFF");
    }

    void OnImportScore()
    {
        string path = FileDialogHelper.OpenFile("导入乐谱", "JSON文件|*.json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        var raw = JsonUtility.FromJson < ScoreDataRaw > (json);
        if (raw == null || raw.notes == null || raw.notes.Length == 0)
        {
            AppendChat("系统", "导入文件格式无效或为空", "#FF6666");
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
        data = ScoreValidator.Validate(data, raw.key_signature ?? currentKeySignature);

        ScorePlayer.Instance.Clear();
        ScorePlayer.Instance.AppendScore(data, true);

        sessionExportNotes = new List< NoteEvent > (data.notes);
        sessionTotalBars = data.total_bars;
        sessionBpm = data.bpm;

        AppendChat("系统", $"已导入并加载：{Path.GetFileName(path)}，共{data.notes.Length}个音符。点击演奏试听。", "#66FFFF");
        btnGenerate.gameObject.SetActive(false);
        SetMusicControlButtonsActive(true);

        var entry = new ScoreIndexEntry
        {
            displayName = Path.GetFileNameWithoutExtension(path),
            filePath = path,
            style = raw.style ?? GetCurrentStyleDisplayName(),
            totalBars = data.total_bars,
            saveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            remark = "外部导入"
        };
        AddScoreIndex(entry);
    }

    // 关键修复：相同路径去重，有则更新，无则新增
    void AddScoreIndex(ScoreIndexEntry entry)
    {
        if (LoginRegisterManager.CurrentUser == null) return;
        if (LoginRegisterManager.CurrentUser.scoreIndex == null)
            LoginRegisterManager.CurrentUser.scoreIndex = new List< ScoreIndexEntry > ();

        var existing = LoginRegisterManager.CurrentUser.scoreIndex.Find(x => x.filePath == entry.filePath);
        if (existing != null)
        {
            existing.displayName = entry.displayName;
            existing.style = entry.style;
            existing.totalBars = entry.totalBars;
            existing.saveTime = entry.saveTime;
            existing.remark = entry.remark;
        }
        else
        {
            LoginRegisterManager.CurrentUser.scoreIndex.Add(entry);
        }

        LoginRegisterManager.UpdateCurrentUser();
    }

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
        if (txt != null) txt.text = playing ? "停止" : "演奏";
    }

    void UpdatePauseText(bool paused)
    {
        var txt = btnPause.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = paused ? "继续" : "暂停";
    }

    // 关键修改：只控制与"当前乐谱"相关的按钮，不包含 ImportScore
    void SetMusicControlButtonsActive(bool active)
    {
        btnPlay.gameObject.SetActive(active);
        btnPause.gameObject.SetActive(active);
        btnContinueGen.gameObject.SetActive(active);
        btnSaveScore.gameObject.SetActive(active);
        // btnImportScore 不在这里控制，始终独立显示
    }

    string BuildSystemPrompt()
    {
        var preset = StylePresets.GetPreset(currentStyle);
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
               "1. 动机发展：先设计一个2~4个音的短动机，然后在后续小节对其进行变化\n" +
               "   - 模进：动机整体移高或移低若干音程\n" +
               "   - 节奏变化：把长音拆成短音，或把短音合并成长音\n" +
               "   - 倒影：把动机音高上下翻转\n" +
               "2. 弱起与留白：旋律可以从弱拍进入（start_tick=2/4/6），不要每小节第1 tick都必须有音\n" +
               "   - 允许整小节只有左手伴奏，右手旋律休止\n" +
               "3. 旋律线不要太平：允许跳进（如从C4跳到G4），不要总是一个音阶一个音阶爬\n\n" +

               "【声部分配】\n" +
               "1. 右手旋律（key_number ≥ 44）：velocity 70-110\n" +
               "   - 大部分时间同一tick只有1个音，但允许偶尔的双音\n" +
               "   - 旋律音的 duration_tick 可以变化：1（短促）、2（八分）、4（四分）、6（附点）\n" +
               "2. 左手伴奏（key_number ≤ 43）：velocity 40-70\n" +
               "   - 柱式和弦：多个音写在同一 start_tick\n" +
               "   - 分解和弦/琶音：同一和弦内 start_tick 依次递增 0,1,2,3\n" +
               "   - 低音长音可以跨越多个小节（duration_tick=16或更长）\n" +
               "3. 双手配合：\n" +
               "   - 强拍（tick=0）可以只有左手根音，右手旋律延后2~4 tick进入\n" +
               "   - 同一tick最多5个音\n\n" +

               "【节奏与休止】\n" +
               "- 不要填满所有tick，适当留白\n" +
               "- 允许附点节奏（duration_tick=3, 6, 12）\n" +
               "- 允许切分：强拍休止，弱拍进音\n\n" +

               $"【风格参数】{GetCurrentStyleDisplayName()}，BPM {preset.bpmMin}-{preset.bpmMax}\n" +
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