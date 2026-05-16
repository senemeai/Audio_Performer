#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CreateLoginRegisterUI : EditorWindow
{
    [MenuItem("Tools/创建登录注册界面")]
    public static void ShowWindow()
    {
        GetWindow<CreateLoginRegisterUI>("登录注册UI创建器");
    }

    void OnGUI()
    {
        GUILayout.Label("一键创建登录注册界面", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("此操作会创建完整的登录/注册界面布局\n包括登录面板、注册面板和对应的管理器", MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("创建登录注册界面", GUILayout.Height(40)))
        {
            CreateLoginRegisterUIComplete();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("仅创建登录面板", GUILayout.Height(30)))
        {
            CreateLoginPanelOnly();
        }

        if (GUILayout.Button("仅创建注册面板", GUILayout.Height(30)))
        {
            CreateRegisterPanelOnly();
        }
    }

    void CreateLoginRegisterUIComplete()
    {
        // 创建主Canvas
        Canvas canvas = FindOrCreateCanvas();

        // 创建主容器
        GameObject canvasObj = canvas.gameObject;
        Transform canvasTransform = canvas.transform;

        // 移除已有的同名面板
        Transform existingLogin = canvasTransform.Find("Panel_Login");
        if (existingLogin != null) DestroyImmediate(existingLogin.gameObject);

        Transform existingRegister = canvasTransform.Find("Panel_Register");
        if (existingRegister != null) DestroyImmediate(existingRegister.gameObject);

        Transform existingManager = canvasTransform.Find("LoginRegisterManager");
        if (existingManager != null) DestroyImmediate(existingManager.gameObject);

        // 创建登录面板
        GameObject loginPanel = CreateLoginPanel(canvasTransform);

        // 创建注册面板
        GameObject registerPanel = CreateRegisterPanel(canvasTransform);

        // 创建管理器
        CreateManager(canvasTransform, loginPanel, registerPanel);

        // 设置初始状态
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);

        Debug.Log("✅ 登录注册界面创建完成！");
        EditorUtility.DisplayDialog("完成", "登录注册界面已创建完成！\n\n请将 LoginRegisterManager.cs 脚本挂载到 LoginRegisterManager 对象上。", "好的");
    }

    Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas_LoginRegister");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在最上层

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // 添加EventSystem（如果没有）
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }
        return canvas;
    }

    GameObject CreateLoginPanel(Transform parent)
    {
        // 主面板
        GameObject panel = new GameObject("Panel_Login");
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color32(30, 30, 40, 255);

        // 内容容器（居中卡片）
        GameObject card = new GameObject("Card");
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(500, 550);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color32(45, 45, 55, 255);

        // 添加阴影效果
        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 128);
        shadow.effectDistance = new Vector2(5, -5);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 50, 50);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = card.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 标题
        TextMeshProUGUI title = CreateText(card.transform, "Text_Title", "🎹 登录", 36);
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 80);

        // 用户名输入框
        TMP_InputField usernameInput = CreateInputField(card.transform, "Input_Username", "用户名", false);
        RectTransform usernameRect = usernameInput.GetComponent<RectTransform>();
        usernameRect.sizeDelta = new Vector2(0, 60);

        // 密码输入框
        TMP_InputField passwordInput = CreateInputField(card.transform, "Input_Password", "密码", true);
        RectTransform passwordRect = passwordInput.GetComponent<RectTransform>();
        passwordRect.sizeDelta = new Vector2(0, 60);

        // 错误提示
        TextMeshProUGUI errorText = CreateText(card.transform, "Text_Error", "", 16);
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.color = new Color32(255, 68, 68, 255);
        RectTransform errorRect = errorText.GetComponent<RectTransform>();
        errorRect.sizeDelta = new Vector2(0, 30);

        // 登录按钮
        Button loginBtn = CreateButton(card.transform, "Btn_Login", "登录", new Color32(76, 175, 80, 255));
        RectTransform loginRect = loginBtn.GetComponent<RectTransform>();
        loginRect.sizeDelta = new Vector2(0, 55);

        // 去注册按钮（文本按钮）
        Button goRegisterBtn = CreateTextButton(card.transform, "Btn_GoRegister", "还没有账号？去注册", 16, new Color32(100, 100, 200, 255));
        RectTransform goRegisterRect = goRegisterBtn.GetComponent<RectTransform>();
        goRegisterRect.sizeDelta = new Vector2(0, 30);

        // 存储组件引用到 panel 上（供管理器使用）
        StoreReferences(panel, title, usernameInput, passwordInput, errorText, loginBtn, goRegisterBtn, null, null, null, null);

        return panel;
    }

    GameObject CreateRegisterPanel(Transform parent)
    {
        // 主面板
        GameObject panel = new GameObject("Panel_Register");
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color32(30, 30, 40, 255);

        // 内容容器（居中卡片）
        GameObject card = new GameObject("Card");
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(500, 650);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color32(45, 45, 55, 255);

        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 128);
        shadow.effectDistance = new Vector2(5, -5);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 50, 50);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = card.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 标题
        TextMeshProUGUI title = CreateText(card.transform, "Text_Title", "🎹 注册", 36);
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 80);

        // 用户名输入框
        TMP_InputField usernameInput = CreateInputField(card.transform, "Input_Username", "用户名 (3-16位)", false);
        RectTransform usernameRect = usernameInput.GetComponent<RectTransform>();
        usernameRect.sizeDelta = new Vector2(0, 60);

        // 密码输入框
        TMP_InputField passwordInput = CreateInputField(card.transform, "Input_Password", "密码 (至少6位)", true);
        RectTransform passwordRect = passwordInput.GetComponent<RectTransform>();
        passwordRect.sizeDelta = new Vector2(0, 60);

        // 确认密码输入框
        TMP_InputField confirmPasswordInput = CreateInputField(card.transform, "Input_ConfirmPassword", "确认密码", true);
        RectTransform confirmRect = confirmPasswordInput.GetComponent<RectTransform>();
        confirmRect.sizeDelta = new Vector2(0, 60);

        // 错误提示
        TextMeshProUGUI errorText = CreateText(card.transform, "Text_Error", "", 16);
        errorText.alignment = TextAlignmentOptions.Center;
        errorText.color = new Color32(255, 68, 68, 255);
        RectTransform errorRect = errorText.GetComponent<RectTransform>();
        errorRect.sizeDelta = new Vector2(0, 30);

        // 注册按钮
        Button registerBtn = CreateButton(card.transform, "Btn_Register", "注册", new Color32(76, 175, 80, 255));
        RectTransform registerRect = registerBtn.GetComponent<RectTransform>();
        registerRect.sizeDelta = new Vector2(0, 55);

        // 去登录按钮（文本按钮）
        Button goLoginBtn = CreateTextButton(card.transform, "Btn_GoLogin", "已有账号？去登录", 16, new Color32(100, 100, 200, 255));
        RectTransform goLoginRect = goLoginBtn.GetComponent<RectTransform>();
        goLoginRect.sizeDelta = new Vector2(0, 30);

        // 存储组件引用
        StoreReferences(panel, title, usernameInput, passwordInput, errorText, registerBtn, goLoginBtn, confirmPasswordInput, null, null, null);

        return panel;
    }

    void CreateManager(Transform parent, GameObject loginPanel, GameObject registerPanel)
    {
        GameObject manager = new GameObject("LoginRegisterManager");
        manager.transform.SetParent(parent, false);

        // 这里只创建空对象，脚本需要手动挂载
        // 添加一个临时组件来存储面板引用（供编辑器使用）
        var tempRef = manager.AddComponent<LoginRegisterTempRef>();
        tempRef.loginPanel = loginPanel;
        tempRef.registerPanel = registerPanel;

        // 获取按钮并设置切换事件
        Button loginGoRegister = loginPanel.GetComponent<LoginPanelRefs>()?.goRegisterBtn;
        Button registerGoLogin = registerPanel.GetComponent<RegisterPanelRefs>()?.goLoginBtn;

        if (loginGoRegister != null)
        {
            loginGoRegister.onClick.RemoveAllListeners();
            loginGoRegister.onClick.AddListener(() => {
                loginPanel.SetActive(false);
                registerPanel.SetActive(true);
            });
        }

        if (registerGoLogin != null)
        {
            registerGoLogin.onClick.RemoveAllListeners();
            registerGoLogin.onClick.AddListener(() => {
                registerPanel.SetActive(false);
                loginPanel.SetActive(true);
            });
        }
    }

    // 辅助创建方法
    TMP_InputField CreateInputField(Transform parent, string name, string placeholder, bool isPassword)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        // 背景
        Image bg = go.AddComponent<Image>();
        bg.color = new Color32(60, 60, 70, 255);

        // 输入框
        TMP_InputField input = go.AddComponent<TMP_InputField>();

        // 文本组件（用于显示输入）
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 5);
        textRect.offsetMax = new Vector2(-15, -5);

        input.textComponent = text;
        input.text = "";

        // 占位符
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 18;
        placeholderText.color = new Color32(150, 150, 150, 255);
        placeholderText.alignment = TextAlignmentOptions.Left;
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(15, 5);
        placeholderRect.offsetMax = new Vector2(-15, -5);

        input.placeholder = placeholderText;

        if (isPassword)
        {
            input.contentType = TMP_InputField.ContentType.Password;
        }

        return input;
    }

    Button CreateButton(Transform parent, string name, string text, Color32 color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();

        // 添加颜色过渡效果
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color32(
            (byte)(color.r + 20),
            (byte)(color.g + 20),
            (byte)(color.b + 20),
            255
        );
        colors.pressedColor = new Color32(
            (byte)(color.r - 20),
            (byte)(color.g - 20),
            (byte)(color.b - 20),
            255
        );
        btn.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Label", text, 20);
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return btn;
    }

    Button CreateTextButton(Transform parent, string name, string text, float fontSize, Color32 color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Button btn = go.AddComponent<Button>();

        TextMeshProUGUI label = CreateText(go.transform, "Label", text, fontSize);
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;

        // 悬停效果
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(200, 200, 255, 255);
        btn.colors = colors;

        return btn;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        return tmp;
    }

    void StoreReferences(GameObject panel, TextMeshProUGUI title, TMP_InputField username, TMP_InputField password,
                         TextMeshProUGUI error, Button actionBtn, Button switchBtn, TMP_InputField confirmPassword = null,
                         Button registerBtn = null, Button loginBtn = null, TextMeshProUGUI successText = null)
    {
        if (panel.name == "Panel_Login")
        {
            var refs = panel.AddComponent<LoginPanelRefs>();
            refs.title = title;
            refs.usernameInput = username;
            refs.passwordInput = password;
            refs.errorText = error;
            refs.loginBtn = actionBtn;
            refs.goRegisterBtn = switchBtn;
        }
        else if (panel.name == "Panel_Register")
        {
            var refs = panel.AddComponent<RegisterPanelRefs>();
            refs.title = title;
            refs.usernameInput = username;
            refs.passwordInput = password;
            refs.confirmPasswordInput = confirmPassword;
            refs.errorText = error;
            refs.registerBtn = actionBtn;
            refs.goLoginBtn = switchBtn;
        }
    }

    void CreateLoginPanelOnly()
    {
        Canvas canvas = FindOrCreateCanvas();
        CreateLoginPanel(canvas.transform);
        Debug.Log("✅ 登录面板创建完成");
    }

    void CreateRegisterPanelOnly()
    {
        Canvas canvas = FindOrCreateCanvas();
        CreateRegisterPanel(canvas.transform);
        Debug.Log("✅ 注册面板创建完成");
    }
}

// 临时引用组件（供编辑器使用）
public class LoginRegisterTempRef : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject registerPanel;
}

// 登录面板组件引用（供代码调用）
public class LoginPanelRefs : MonoBehaviour
{
    public TextMeshProUGUI title;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI errorText;
    public Button loginBtn;
    public Button goRegisterBtn;
}

// 注册面板组件引用（供代码调用）
public class RegisterPanelRefs : MonoBehaviour
{
    public TextMeshProUGUI title;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TextMeshProUGUI errorText;
    public Button registerBtn;
    public Button goLoginBtn;
}
#endif