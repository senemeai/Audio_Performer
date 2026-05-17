using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentManager : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI textInstrumentName;
    public Button btnPrev;
    public Button btnNext;

    [Header("乐器配置")]
    public List<InstrumentInfo> instruments = new List<InstrumentInfo>
    {
        new InstrumentInfo("treble_upright", "高音立式钢琴"),
        new InstrumentInfo("piano", "原声钢琴"),
        new InstrumentInfo("grand_piano", "大钢琴"),
        new InstrumentInfo("bright_piano", "亮音大钢琴"),
        new InstrumentInfo("electric_grand", "电钢琴"),
        new InstrumentInfo("honky_tonk", "酒吧钢琴"),
        new InstrumentInfo("electric", "电钢琴1"),
        new InstrumentInfo("electric2", "电钢琴2"),
        new InstrumentInfo("celesta", "钢片琴"),
        new InstrumentInfo("glockenspiel", "钟琴"),
        new InstrumentInfo("music_box", "八音盒"),
        new InstrumentInfo("vibraphone", "电颤琴"),
        new InstrumentInfo("xylophone", "木琴"),
        new InstrumentInfo("dulcimer", "扬琴"),
        new InstrumentInfo("harmonica", "口琴"),
        new InstrumentInfo("guitar_nylon", "尼龙弦吉他"),
        new InstrumentInfo("guitar_steel", "钢弦吉他"),
        new InstrumentInfo("guitar_jazz", "爵士乐电吉他"),
        new InstrumentInfo("guitar_clean", "清音电吉他"),
        new InstrumentInfo("acoustic_bass", "原声贝斯"),
        new InstrumentInfo("violin", "小提琴"),
        new InstrumentInfo("harp", "竖琴"),
        new InstrumentInfo("soprano_sax", "高音萨克斯"),
        new InstrumentInfo("piccolo", "短笛"),
        new InstrumentInfo("koto", "筝"),
        new InstrumentInfo("shanai", "唢呐"),
        new InstrumentInfo("tinkle_bell", "铃铛"),
        new InstrumentInfo("synth_drum", "合成鼓"),
        new InstrumentInfo("bird", "鸟鸣声")
    };

private int currentIndex = 0;

[System.Serializable]
public class InstrumentInfo
{
    public string prefix;
    public string displayName;

    public InstrumentInfo(string p, string name)
    {
        prefix = p;
        displayName = name;
    }
}

void Start()
{
    string savedPrefix = PlayerPrefs.GetString("MusicPlayer_LastInstrument", "piano");
    currentIndex = instruments.FindIndex(x => x.prefix == savedPrefix);
    if (currentIndex < 0) currentIndex = 0;

    btnPrev.onClick.AddListener(OnPrev);
    btnNext.onClick.AddListener(OnNext);

    ApplySwitch(false); // 启动时应用，但不重复保存
}

void OnPrev()
{
    currentIndex = (currentIndex - 1 + instruments.Count) % instruments.Count;
    ApplySwitch();
}

void OnNext()
{
    currentIndex = (currentIndex + 1) % instruments.Count;
    ApplySwitch();
}

    void ApplySwitch(bool save = true)
    {
        var info = instruments[currentIndex];
        SF2AudioManager.Instance.SwitchInstrument(info.prefix);

        if (textInstrumentName != null)
            textInstrumentName.text = info.displayName;
    }
}