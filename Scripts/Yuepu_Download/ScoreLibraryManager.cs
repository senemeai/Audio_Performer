using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreLibraryManager : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelLibrary;
    public Transform contentParent;
    public GameObject prefabScoreItem;

    [Header("详情面板")]
    public GameObject panelDetail;
    public TMP_InputField inputDetailName;
    public TextMeshProUGUI textDetailPath;
    public TextMeshProUGUI textDetailMeta;
    public TMP_InputField inputRemark;
    public Button btnSaveRemark;

    [Header("操作按钮")]
    public Button btnClose;
    public Button btnImportExternal;
    public Button btnPlayThis;
    public Button btnOpenFolder;
    public Button btnRelocate;
    public Button btnRename;
    public Button btnDelete;

    [Header("筛选排序")]
    public TMP_Dropdown dropdownFilterStyle;
    public TMP_Dropdown dropdownSort;

    private List<ScoreItemUI> itemUIs = new List<ScoreItemUI>();
    private ScoreIndexEntry selectedEntry;

    void Start()
    {
        panelLibrary.SetActive(false);
        panelDetail.SetActive(false);

        btnClose.onClick.AddListener(() => panelLibrary.SetActive(false));
        btnImportExternal.onClick.AddListener(OnImportExternal);
        btnPlayThis.onClick.AddListener(OnPlaySelected);
        btnOpenFolder.onClick.AddListener(OnOpenFolder);
        btnRelocate.onClick.AddListener(OnRelocate);
        btnRename.onClick.AddListener(OnRename);
        btnDelete.onClick.AddListener(OnDelete);
        btnSaveRemark.onClick.AddListener(OnSaveRemark);

        // 防御性绑定：防止未拖入导致 Start 崩溃
        if (dropdownFilterStyle != null)
        {
            dropdownFilterStyle.onValueChanged.AddListener(OnFilterChanged);
            dropdownFilterStyle.ClearOptions();
            dropdownFilterStyle.AddOptions(new List<string> {
                "全部风格", "流行抒情", "轻快儿歌", "古典练习曲", "爵士即兴", "电影史诗", "极简氛围"
            });
        }
        else
        {
            Debug.LogWarning("[ScoreLibraryManager] Dropdown_FilterStyle 未绑定！筛选功能不可用。");
        }

        if (dropdownSort != null)
        {
            dropdownSort.onValueChanged.AddListener(OnSortChanged);
            dropdownSort.ClearOptions();
            dropdownSort.AddOptions(new List<string> { "时间倒序", "时间正序", "名称A-Z" });
        }
        else
        {
            Debug.LogWarning("[ScoreLibraryManager] Dropdown_Sort 未绑定！排序功能不可用。");
        }
    }

    public void OpenLibrary()
    {
        panelLibrary.SetActive(true);
        RefreshList();
    }

    void RefreshList()
    {
        foreach (var item in itemUIs)
            if (item != null) Destroy(item.gameObject);
        itemUIs.Clear();

        var entries = GetFilteredSortedEntries();
        foreach (var e in entries)
        {
            var go = Instantiate(prefabScoreItem, contentParent);
            var ui = go.GetComponent < ScoreItemUI > ();
            ui.Setup(e, OnItemClick, OnItemDoubleClick);
            itemUIs.Add(ui);
        }

        panelDetail.SetActive(false);
        selectedEntry = null;
    }

    List<ScoreIndexEntry> GetFilteredSortedEntries()
    {
        if (LoginRegisterManager.CurrentUser?.scoreIndex == null)
            return new List< ScoreIndexEntry > ();

        var list = new List< ScoreIndexEntry > (LoginRegisterManager.CurrentUser.scoreIndex);

        string filter = dropdownFilterStyle.options[dropdownFilterStyle.value].text;
        if (filter != "全部风格")
            list = list.Where(x => x.style == filter).ToList();

        int sort = dropdownSort.value;
        if (sort == 0) list = list.OrderByDescending(x => x.saveTime).ToList();
        else if (sort == 1) list = list.OrderBy(x => x.saveTime).ToList();
        else if (sort == 2) list = list.OrderBy(x => x.displayName).ToList();

        return list;
    }

    void OnItemClick(ScoreItemUI item)
    {
        selectedEntry = item.Entry;
        ShowDetail(item.Entry);
    }

    void OnItemDoubleClick(ScoreItemUI item)
    {
        selectedEntry = item.Entry;
        LoadAndPlay(item.Entry);
    }

    void ShowDetail(ScoreIndexEntry e)
    {
        panelDetail.SetActive(true);
        inputDetailName.text = e.displayName;
        textDetailPath.text = e.filePath;
        textDetailMeta.text = $"风格：{e.style} | 小节数：{e.totalBars} | 保存于：{System.DateTimeOffset.FromUnixTimeSeconds(e.saveTime):yyyy-MM-dd HH:mm}";
        inputRemark.text = e.remark ?? "";
        textDetailPath.color = Color.white;
    }

    void OnPlaySelected()
    {
        if (selectedEntry != null) LoadAndPlay(selectedEntry);
    }

    void LoadAndPlay(ScoreIndexEntry e)
    {
        if (!File.Exists(e.filePath))
        {
            textDetailPath.color = Color.red;
            return;
        }
        textDetailPath.color = Color.white;

        string json = File.ReadAllText(e.filePath);
        var raw = JsonUtility.FromJson < ScoreDataRaw > (json);
        if (raw == null || raw.notes == null || raw.notes.Length == 0)
        {
            textDetailPath.color = Color.red;
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
        data = ScoreValidator.Validate(data, raw.key_signature ?? "C");

        ScorePlayer.Instance.Clear();
        ScorePlayer.Instance.AppendScore(data, true);
        ScorePlayer.Instance.Play();

        panelLibrary.SetActive(false);
    }

    void OnOpenFolder()
    {
        if (selectedEntry == null) return;
        string path = selectedEntry.filePath;
        if (!File.Exists(path)) return;

        string dir = Path.GetDirectoryName(path);
#if UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
#endif
    }

    void OnRelocate()
    {
        if (selectedEntry == null) return;
        string newPath = FileDialogHelper.OpenFile("重新定位乐谱文件", "JSON文件|*.json");
        if (string.IsNullOrEmpty(newPath)) return;

        selectedEntry.filePath = newPath;
        LoginRegisterManager.UpdateCurrentUser();
        RefreshList();
    }

    void OnRename()
    {
        if (selectedEntry == null) return;
        string newName = inputDetailName.text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        selectedEntry.displayName = newName;
        LoginRegisterManager.UpdateCurrentUser();
        RefreshList();
    }

    void OnSaveRemark()
    {
        if (selectedEntry == null) return;
        selectedEntry.remark = inputRemark.text;
        LoginRegisterManager.UpdateCurrentUser();
    }

    void OnDelete()
    {
        if (selectedEntry == null) return;

        var list = LoginRegisterManager.CurrentUser.scoreIndex;
        list.Remove(selectedEntry);
        LoginRegisterManager.UpdateCurrentUser();
        RefreshList();
    }

    void OnImportExternal()
    {
        string path = FileDialogHelper.OpenFile("导入乐谱", "JSON文件|*.json");
        if (string.IsNullOrEmpty(path)) return;

        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        var raw = JsonUtility.FromJson < ScoreDataRaw > (json);
        if (raw == null || raw.notes == null || raw.notes.Length == 0)
        {
            Debug.LogWarning("导入文件格式无效");
            return;
        }

        var entry = new ScoreIndexEntry
        {
            displayName = Path.GetFileNameWithoutExtension(path),
            filePath = path,
            style = raw.style ?? "未知",
            totalBars = raw.total_bars,
            saveTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            remark = "外部导入"
        };

        if (LoginRegisterManager.CurrentUser.scoreIndex == null)
            LoginRegisterManager.CurrentUser.scoreIndex = new List< ScoreIndexEntry > ();
        LoginRegisterManager.CurrentUser.scoreIndex.Add(entry);
        LoginRegisterManager.UpdateCurrentUser();

        RefreshList();
    }

    void OnFilterChanged(int index) => RefreshList();
    void OnSortChanged(int index) => RefreshList();
}