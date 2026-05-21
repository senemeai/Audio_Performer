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
using PianoComposition;

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

    private string currentStyle = "Pop";
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
            $"3. 左手伴奏使用经典分解模式（根→五→高根→高三）\n" +
            $"4. 直接输出 JSON，不要任何文字说明";

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
        ScoreDataRaw raw = JsonUtility.FromJson<ScoreDataRaw>(json);
        if (raw == null || raw.notes == null || raw.notes.Length == 0)
        {
            AppendChat("系统", "乐谱解析失败或为空", "#FF6666");
            return;
        }

        ScoreData data = new ScoreData
        {
            bpm = raw.bpm,
            key_signature = raw.key_signature,
            style = raw.style ?? currentStyle,
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

        lastSegmentNotes = new List<NoteEvent>(data.notes);
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
            style = GetCurrentStyleDisplayName(),
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
        var raw = JsonUtility.FromJson<ScoreDataRaw>(json);
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

        sessionExportNotes = new List<NoteEvent>(data.notes);
        sessionTotalBars = data.total_bars;
        sessionBpm = data.bpm;

        AppendChat("系统", $"已导入并加载：{Path.GetFileName(path)}，共{data.notes.Length}个音符。点击演奏试听。", "#66FFFF");
        btnGenerate.gameObject.SetActive(false);
        SetMusicControlButtonsActive(true);
    }

    void AddScoreIndex(ScoreIndexEntry entry)
    {
        if (LoginRegisterManager.CurrentUser == null) return;
        if (LoginRegisterManager.CurrentUser.scoreIndex == null)
            LoginRegisterManager.CurrentUser.scoreIndex = new List<ScoreIndexEntry>();

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

    void SetMusicControlButtonsActive(bool active)
    {
        btnPlay.gameObject.SetActive(active);
        btnPause.gameObject.SetActive(active);
        btnContinueGen.gameObject.SetActive(active);
        btnSaveScore.gameObject.SetActive(active);
    }

    string BuildSystemPrompt()
    {
        var preset = StylePresets.GetPreset(currentStyle);
        string texturePrompt = ScoreValidator.GetTexturePromptText(currentStyle);

        string userReqSection = "";
        if (!string.IsNullOrEmpty(parsedKeySignature))
            userReqSection += $"【用户指定调式】{currentKeySignature}。必须严格使用此调式，禁止擅自改回C大调。\n";
        if (!string.IsNullOrEmpty(userStartNote))
            userReqSection += $"【用户指定起始音】第一个音符必须是{userStartNote}（key_number={GetKeyNumberFromName(userStartNote)}），禁止从其他音开始。\n";

        return "你是一位专业的88键钢琴编曲AI，精通各种伴奏织体和经典分解模式。\n\n" +
               "【输出格式】\n" +
               "1. 只输出```json代码块，块外不要有任何文字\n" +
               "2. 字段：key_number(1-88), velocity(1-127), start_tick(int,≥0), duration_tick(int,≥1)\n" +
               "3. 必须包含：bpm, key_signature, total_bars, style\n" +
               "4. 1 tick = 1个十六分音符，1小节 = 16 tick\n" +
               "5. 建议4小节，最多64个Note\n\n" +

               texturePrompt + "\n\n" +

               userReqSection +
               $"当前调式：{currentKeySignature}\n" +
               $"可选和弦进行：{preset.chordProgression}\n\n" +

               "【右手旋律原则】\n" +
               "1. 旋律要有起伏，不要一直在一个音区\n" +
               "2. 每个乐句后要有呼吸（1-2 tick空白）\n" +
               "3. 高潮在第三小节\n" +
               "4. 旋律音 velocity 70-100\n\n" +

               $"【风格参数】{GetCurrentStyleDisplayName()}，BPM {preset.bpmMin}-{preset.bpmMax}\n" +
               $"【力度轮廓】{preset.dynamicsProfile}\n\n" +

               "【禁止】\n" +
               "- 禁止key_number超出1-88\n" +
               "- 禁止velocity为0或>127\n" +
               "- 禁止duration_tick=0\n" +
               "- 禁止连续4小节使用完全相同的旋律\n" +
               "- 禁止使用简单的1-3-5-1或1-2-3-1均分分解模式\n\n" +

               "现在请生成一段4小节、有织体变化、使用经典分解模式的钢琴曲。";
    }

    string BuildRequestBody(string system, string user)
    {
        string sys = EscapeJson(system);
        string usr = EscapeJson(user);
        return $"{{\"model\":\"{modelName}\",\"temperature\":0.2,\"messages\":[{{\"role\":\"system\",\"content\":\"{sys}\"}},{{\"role\":\"user\",\"content\":\"{usr}\"}}]}}";
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