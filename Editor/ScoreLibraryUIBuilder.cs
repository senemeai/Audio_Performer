#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ScoreLibraryUIBuilder
{
    [MenuItem("GameObject/UI/生成乐谱库面板 (700x300)", false, 10)]
    static void Build()
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        Canvas canvas = Object.FindObjectOfType < Canvas > ();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent < Canvas > ();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent < CanvasScaler > ();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // ========== 主面板 700x300 ==========
        GameObject panel = CreatePanel("Panel_ScoreLibrary", canvas.transform, new Color(0.05f, 0.05f, 0.05f, 0.1f));
        RectTransform prt = panel.GetComponent < RectTransform > ();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(700, 300);

        // ========== 标题栏 高度35 ==========
        GameObject titleBar = CreatePanel("Panel_LibTitleBar", prt, new Color(0.08f, 0.08f, 0.08f, 0.1f));
        SetRect(titleBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -17.5f), new Vector2(0, 35));

        GameObject txtTitle = CreateText("Text_Title", titleBar.transform, "我的乐谱库", 22, TextAlignmentOptions.Center);
        SetRect(txtTitle, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        GameObject btnClose = CreatePixelButton("Btn_Close", titleBar.transform, "×", new Color(0.6f, 0.15f, 0.15f, 0.1f));
        SetRect(btnClose, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(30, 30));

        GameObject btnImport = CreatePixelButton("Btn_ImportExternal", titleBar.transform, "导入", new Color(0.15f, 0.35f, 0.55f, 0.1f));
        SetRect(btnImport, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(55, 0), new Vector2(60, 26));

        GameObject ddFilter = CreatePixelDropdown("Dropdown_FilterStyle", titleBar.transform, new List<string> { "全部", "流行", "儿歌", "古典", "爵士", "史诗", "极简" });
        SetRect(ddFilter, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(130, 0), new Vector2(90, 26));

        GameObject ddSort = CreatePixelDropdown("Dropdown_Sort", titleBar.transform, new List<string> { "时间倒序", "时间正序", "名称" });
        SetRect(ddSort, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(230, 0), new Vector2(80, 26));

        // ========== ScrollView 列表区 ==========
        GameObject scroll = CreatePixelScrollView("ScrollView_ScoreList", prt);
        SetRect(scroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -17.5f), new Vector2(0, -35));

        Transform content = scroll.transform.Find("Viewport/Content");
        if (content != null)
        {
            GameObject example = CreateScoreItemExample("ScoreItem_Example", content);
            SetRect(example, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(0, 40));
        }

        // ========== 详情面板 高度50 默认隐藏 ==========
        GameObject detail = CreatePanel("Panel_ScoreDetail", prt, new Color(0.06f, 0.06f, 0.06f, 0.1f));
        SetRect(detail, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(0, 50));
        detail.SetActive(false);

        GameObject txtFileName = CreateText("Text_FileName", detail.transform, "文件名", 16, TextAlignmentOptions.Left);
        SetRect(txtFileName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -6), new Vector2(280, 22));

        GameObject txtFilePath = CreateText("Text_FilePath", detail.transform, "C:\\...", 11, TextAlignmentOptions.Left);
        SetRect(txtFilePath, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -28), new Vector2(360, 18));

        GameObject txtMeta = CreateText("Text_Meta", detail.transform, "风格 | 小节 | 时间", 12, TextAlignmentOptions.Left);
        SetRect(txtMeta, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -44), new Vector2(260, 18));

        GameObject inputRemark = CreatePixelInput("Input_Remark", detail.transform, "备注...");
        SetRect(inputRemark, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(8, 4), new Vector2(-220, 22));

        GameObject btnSaveRemark = CreatePixelButton("Btn_SaveRemark", detail.transform, "保存", new Color(0.15f, 0.4f, 0.2f, 0.1f));
        SetRect(btnSaveRemark, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-72, 4), new Vector2(60, 22));

        GameObject btnPlay = CreatePixelButton("Btn_PlayThis", detail.transform, "演奏", new Color(0.1f, 0.5f, 0.25f, 0.1f));
        SetRect(btnPlay, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(50, 22));

        GameObject btnFolder = CreatePixelButton("Btn_OpenFolder", detail.transform, "位置", new Color(0.3f, 0.3f, 0.35f, 0.1f));
        SetRect(btnFolder, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -6), new Vector2(50, 22));

        GameObject btnRelocate = CreatePixelButton("Btn_Relocate", detail.transform, "定位", new Color(0.3f, 0.3f, 0.35f, 0.1f));
        SetRect(btnRelocate, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -6), new Vector2(50, 22));

        GameObject btnRename = CreatePixelButton("Btn_Rename", detail.transform, "改名", new Color(0.3f, 0.3f, 0.5f, 0.1f));
        SetRect(btnRename, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-176, -6), new Vector2(50, 22));

        GameObject btnDelete = CreatePixelButton("Btn_Delete", detail.transform, "删除", new Color(0.5f, 0.15f, 0.15f, 0.1f));
        SetRect(btnDelete, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-232, -6), new Vector2(50, 22));

        // 选中并提示
        Selection.activeGameObject = panel;
        EditorUtility.DisplayDialog("生成完成", "乐谱库面板已生成（700×300，Alpha=0.1）。\n请取消勾选 Panel_ScoreLibrary 的 Active 设为默认隐藏，并将 ScoreItem_Example 拖成 Prefab。", "确定");
    }

    // ==================== 辅助函数 ====================

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateText(string name, Transform parent, string content, int fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tmp.raycastTarget = false;
        return go;
    }

    static GameObject CreatePixelButton(string name, Transform parent, string label, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bgColor;
        img.type = Image.Type.Sliced;

        var txt = CreateText("Text", go.transform, label, 13, TextAlignmentOptions.Center);
        var trt = txt.GetComponent < RectTransform > ();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject CreatePixelInput(string name, Transform parent, string placeholder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.1f);

        GameObject ta = new GameObject("Text Area", typeof(RectTransform));
        ta.transform.SetParent(go.transform, false);
        var tart = ta.GetComponent < RectTransform > ();
        tart.anchorMin = Vector2.zero;
        tart.anchorMax = Vector2.one;
        tart.offsetMin = new Vector2(4, 2);
        tart.offsetMax = new Vector2(-4, -2);

        GameObject txt = CreateText("Text", ta.transform, "", 12, TextAlignmentOptions.Left);
        txt.GetComponent < RectTransform > ().anchorMin = Vector2.zero;
        txt.GetComponent < RectTransform > ().anchorMax = Vector2.one;
        txt.GetComponent < RectTransform > ().offsetMin = Vector2.zero;
        txt.GetComponent < RectTransform > ().offsetMax = Vector2.zero;

        GameObject ph = CreateText("Placeholder", ta.transform, placeholder, 12, TextAlignmentOptions.Left);
        ph.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
        ph.GetComponent < RectTransform > ().anchorMin = Vector2.zero;
        ph.GetComponent < RectTransform > ().anchorMax = Vector2.one;
        ph.GetComponent < RectTransform > ().offsetMin = Vector2.zero;
        ph.GetComponent < RectTransform > ().offsetMax = Vector2.zero;

        var input = go.GetComponent<TMP_InputField>();
        input.textViewport = tart;
        input.textComponent = txt.GetComponent<TextMeshProUGUI>();
        input.placeholder = ph.GetComponent<TextMeshProUGUI>();
        return go;
    }

    static GameObject CreatePixelDropdown(string name, Transform parent, List<string> options)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 0.1f);
        img.type = Image.Type.Sliced;

        GameObject label = CreateText("Label", go.transform, options[0], 12, TextAlignmentOptions.Left);
        SetRect(label, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(6, 0), new Vector2(-22, 0));

        GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(go.transform, false);
        arrow.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);
        SetRect(arrow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(14, 14));

        // Template
        GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        template.transform.SetParent(go.transform, false);
        template.SetActive(false);
        template.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        var tRT = template.GetComponent < RectTransform > ();
        tRT.anchorMin = new Vector2(0, 0);
        tRT.anchorMax = new Vector2(1, 0);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.sizeDelta = new Vector2(0, 100);
        tRT.anchoredPosition = Vector2.zero;

        GameObject tViewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        tViewport.transform.SetParent(template.transform, false);
        var tvRT = tViewport.GetComponent < RectTransform > ();
        tvRT.anchorMin = Vector2.zero;
        tvRT.anchorMax = Vector2.one;
        tvRT.offsetMin = Vector2.zero;
        tvRT.offsetMax = Vector2.zero;

        GameObject tContent = new GameObject("Content", typeof(RectTransform));
        tContent.transform.SetParent(tViewport.transform, false);
        var tcRT = tContent.GetComponent < RectTransform > ();
        tcRT.anchorMin = new Vector2(0, 1);
        tcRT.anchorMax = new Vector2(1, 1);
        tcRT.pivot = new Vector2(0.5f, 1);
        tcRT.sizeDelta = new Vector2(0, 0);
        tcRT.anchoredPosition = Vector2.zero;

        var vlg = tContent.AddComponent < VerticalLayoutGroup > ();
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.spacing = 2;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        tContent.AddComponent < ContentSizeFitter > ().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle));
        item.transform.SetParent(tContent.transform, false);
        item.GetComponent < RectTransform > ().sizeDelta = new Vector2(0, 22);
        item.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.1f);
        var toggle = item.GetComponent<Toggle>();
        toggle.isOn = true;

        GameObject itemLabel = CreateText("Item Label", item.transform, "Option", 12, TextAlignmentOptions.Left);
        itemLabel.GetComponent < RectTransform > ().anchorMin = Vector2.zero;
        itemLabel.GetComponent < RectTransform > ().anchorMax = Vector2.one;
        itemLabel.GetComponent < RectTransform > ().offsetMin = new Vector2(6, 0);
        itemLabel.GetComponent < RectTransform > ().offsetMax = new Vector2(-6, 0);

        // Template Scrollbar
        GameObject tsbGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        tsbGO.transform.SetParent(template.transform, false);
        var tsbRT = tsbGO.GetComponent < RectTransform > ();
        tsbRT.anchorMin = new Vector2(1, 0);
        tsbRT.anchorMax = Vector2.one;
        tsbRT.pivot = Vector2.one;
        tsbRT.sizeDelta = new Vector2(14, 0);
        tsbGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.1f);
        var tsb = tsbGO.GetComponent < Scrollbar > ();
        tsb.direction = Scrollbar.Direction.BottomToTop;

        GameObject tsa = new GameObject("Sliding Area", typeof(RectTransform));
        tsa.transform.SetParent(tsbGO.transform, false);
        var tsaRT = tsa.GetComponent < RectTransform > ();
        tsaRT.anchorMin = Vector2.zero;
        tsaRT.anchorMax = Vector2.one;
        tsaRT.offsetMin = new Vector2(3, 3);
        tsaRT.offsetMax = new Vector3(-3, -3);

        GameObject th = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        th.transform.SetParent(tsa.transform, false);
        th.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.1f);
        tsb.handleRect = th.GetComponent < RectTransform > ();
        tsb.targetGraphic = th.GetComponent<Image>();

        var tSR = template.GetComponent < ScrollRect > ();
        tSR.content = tcRT;
        tSR.viewport = tvRT;
        tSR.verticalScrollbar = tsb;
        tSR.vertical = true;
        tSR.horizontal = false;
        tSR.movementType = ScrollRect.MovementType.Clamped;

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.targetGraphic = img;
        dd.template = tRT;
        dd.captionText = label.GetComponent<TextMeshProUGUI>();
        dd.itemText = itemLabel.GetComponent<TextMeshProUGUI>();
        dd.options = new List<TMP_Dropdown.OptionData>();
        foreach (var opt in options)
            dd.options.Add(new TMP_Dropdown.OptionData(opt));

        return go;
    }

    static GameObject CreatePixelScrollView(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.1f);

        var sr = go.GetComponent < ScrollRect > ();

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(go.transform, false);
        var vpRT = viewport.GetComponent < RectTransform > ();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-14, 0);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.GetComponent < RectTransform > ();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = new Vector2(0, 0);
        cRT.anchoredPosition = Vector2.zero;

        var vlg = content.AddComponent < VerticalLayoutGroup > ();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent < ContentSizeFitter > ().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = cRT;
        sr.viewport = vpRT;
        sr.vertical = true;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 20;

        GameObject sbGO = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(go.transform, false);
        var sbRT = sbGO.GetComponent < RectTransform > ();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = Vector2.one;
        sbRT.pivot = Vector2.one;
        sbRT.sizeDelta = new Vector2(14, 0);
        sbGO.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.1f);
        var sb = sbGO.GetComponent < Scrollbar > ();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject sa = new GameObject("Sliding Area", typeof(RectTransform));
        sa.transform.SetParent(sbGO.transform, false);
        var saRT = sa.GetComponent < RectTransform > ();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = new Vector2(3, 3);
        saRT.offsetMax = new Vector2(-3, -3);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sa.transform, false);
        handle.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.1f);
        sb.handleRect = handle.GetComponent < RectTransform > ();
        sb.targetGraphic = handle.GetComponent<Image>();

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return go;
    }

    static GameObject CreateScoreItemExample(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.1f);

        GameObject txtName = CreateText("Text_ScoreName", go.transform, "乐谱名称", 14, TextAlignmentOptions.Left);
        SetRect(txtName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -3), new Vector2(200, 18));

        GameObject txtPath = CreateText("Text_PathSummary", go.transform, "C:\\Users\\...", 10, TextAlignmentOptions.Left);
        SetRect(txtPath, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -22), new Vector2(300, 14));

        GameObject txtDate = CreateText("Text_SaveDate", go.transform, "05-15", 10, TextAlignmentOptions.Right);
        SetRect(txtDate, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-70, -3), new Vector2(60, 14));

        GameObject txtStyle = CreateText("Text_StyleTag", go.transform, "流行", 10, TextAlignmentOptions.Right);
        SetRect(txtStyle, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -3), new Vector2(60, 14));

        return go;
    }

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var rt = go.GetComponent < RectTransform > ();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }
}
#endif