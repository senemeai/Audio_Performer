using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    [Header("打开设置入口")]
    public Button btnOpenSettings;

    [Header("设置面板引用")]
    public GameObject settingsPanel;

    [Header("用户信息")]
    public Image imgAvatar;
    public TextMeshProUGUI textUsername;
    public TextMeshProUGUI textRegisterTime;
    public Button btnEditAccount;

    [Header("编辑表单")]
    public GameObject editFormSection;
    public TMP_InputField inputNewUsername;
    public TextMeshProUGUI textUsernameHint;
    public TMP_InputField inputNewPassword;
    public TMP_InputField inputConfirmPassword;
    public TextMeshProUGUI textPasswordHint;
    public Button btnConfirmEdit;
    public Button btnCancelEdit;

    [Header("头像上传")]
    public Image imgEditAvatar;      // 编辑表单里的头像预览（60×60那个）
    public Button btnChangeAvatar;   // "更换头像"按钮
    
    [Header("反馈提示")]
    public TextMeshProUGUI textFeedback;

    [Header("退出游戏")]
    public Button btnExitGame;

    [Header("弹窗")]
    public GameObject modalOverlay;
    public TextMeshProUGUI textModalContent;
    public Button btnModalConfirm;
    public Button btnModalCancel;

    [Header("关闭按钮")]
    public Button btnClose;

    [Header("其他配置")]
    public Sprite defaultAvatar;
    public string loginSceneName = "LoginScene";

    private Action onModalConfirm;
    private Action onModalCancel;

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (btnOpenSettings != null)
            btnOpenSettings.onClick.AddListener(OpenSettings);

        btnClose.onClick.AddListener(CloseSettings);
        btnEditAccount.onClick.AddListener(() => ToggleEditForm(true));
        btnConfirmEdit.onClick.AddListener(OnClickConfirmEdit);
        btnCancelEdit.onClick.AddListener(CloseEditForm);
        if (btnChangeAvatar != null)
            btnChangeAvatar.onClick.AddListener(OnChangeAvatar);
        btnExitGame.onClick.AddListener(OnClickExitGame);

        btnModalConfirm.onClick.AddListener(() => { onModalConfirm?.Invoke(); });
        btnModalCancel.onClick.AddListener(() => { onModalCancel?.Invoke(); });

        inputConfirmPassword.onValueChanged.AddListener(OnConfirmPasswordChanged);

        editFormSection.SetActive(false);
        modalOverlay.SetActive(false);
        textFeedback.gameObject.SetActive(false);

        RefreshUserInfo();
    }
    void Awake()
    {
        Instance = this;
    }
    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("[SettingsManager] settingsPanel 字段为空！请拖到 Inspector 里！");
            return;
        }

        settingsPanel.SetActive(true);  // ← 改这里，原来是 gameObject.SetActive(true)
        editFormSection.SetActive(false);
        textFeedback.gameObject.SetActive(false);
        RefreshUserInfo();
    }
    void RefreshUserInfo()
    {
        var user = LoginRegisterManager.CurrentUser;
        if (user == null) return;

        textUsername.text = user.username;
        textRegisterTime.text = "注册时间：" + DateTimeOffset.FromUnixTimeSeconds(user.registerTime).ToString("yyyy-MM-dd HH:mm");

        // 加载头像
        if (!string.IsNullOrEmpty(user.avatarPath) && File.Exists(user.avatarPath))
            LoadAvatar(user.avatarPath, imgAvatar);
        else
            imgAvatar.sprite = defaultAvatar;
    }

    void ToggleEditForm(bool show)
    {
        editFormSection.SetActive(show);

        if (show)
        {
            inputNewUsername.text = LoginRegisterManager.CurrentUser?.username ?? "";
            inputNewPassword.text = "";
            inputConfirmPassword.text = "";
            textUsernameHint.gameObject.SetActive(false);
            textPasswordHint.gameObject.SetActive(false);

            // 加载当前头像到编辑预览
            var user = LoginRegisterManager.CurrentUser;
            if (user != null && !string.IsNullOrEmpty(user.avatarPath) && File.Exists(user.avatarPath))
                LoadAvatar(user.avatarPath, imgEditAvatar);
            else if (imgEditAvatar != null)
                imgEditAvatar.sprite = defaultAvatar;
        }
    }
    void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    void CloseEditForm()
    {
        editFormSection.SetActive(false);
        inputNewUsername.text = "";
        inputNewPassword.text = "";
        inputConfirmPassword.text = "";
        textUsernameHint.gameObject.SetActive(false);
        textPasswordHint.gameObject.SetActive(false);
    }
    void OnChangeAvatar()
    {
        string path = FileDialogHelper.OpenFile("选择头像", "图片文件|*.png;*.jpg;*.jpeg");
        if (string.IsNullOrEmpty(path)) return;

        // 加载预览
        LoadAvatar(path, imgEditAvatar);
        LoadAvatar(path, imgAvatar);

        // 复制到持久化目录，防止原文件被删
        try
        {
            string avatarDir = Path.Combine(Application.persistentDataPath, "Avatars");
            System.IO.Directory.CreateDirectory(avatarDir);

            string ext = System.IO.Path.GetExtension(path).ToLower();
            string destPath = Path.Combine(avatarDir, LoginRegisterManager.CurrentUser.username + "_avatar" + ext);
            System.IO.File.Copy(path, destPath, true);

            // 保存到用户数据
            LoginRegisterManager.CurrentUser.avatarPath = destPath;
            LoginRegisterManager.UpdateCurrentUser();

            ShowFeedback("头像更换成功", true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SettingsManager] 保存头像失败: " + e.Message);
            ShowFeedback("头像保存失败", false);
        }
    }

    void LoadAvatar(string path, Image targetImage)
    {
        if (targetImage == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            targetImage.sprite = sprite;
        }
        catch
        {
            targetImage.sprite = defaultAvatar;
        }
    }
    void OnConfirmPasswordChanged(string value)
    {
        string pwd = inputNewPassword.text;
        string confirm = inputConfirmPassword.text;

        if (string.IsNullOrEmpty(pwd) && string.IsNullOrEmpty(confirm))
        {
            textPasswordHint.gameObject.SetActive(false);
            return;
        }

        if (pwd == confirm)
        {
            textPasswordHint.text = "密码一致";
            textPasswordHint.color = Color.green;
            textPasswordHint.gameObject.SetActive(true);
        }
        else
        {
            textPasswordHint.text = "密码不一致";
            textPasswordHint.color = Color.red;
            textPasswordHint.gameObject.SetActive(true);
        }
    }

    void OnClickConfirmEdit()
    {
        string newName = inputNewUsername.text.Trim();
        string newPwd = inputNewPassword.text;
        string confirmPwd = inputConfirmPassword.text;

        if (string.IsNullOrEmpty(newName))
        {
            ShowFeedback("用户名不能为空", false);
            return;
        }

        var current = LoginRegisterManager.CurrentUser;
        if (current == null) return;

        // 检测用户名唯一性（仅当用户名改变时）
        if (newName != current.username)
        {
            var allUsers = LoginRegisterManager.LoadUserDatabase();
            foreach (var u in allUsers)
            {
                if (u.username == newName)
                {
                    ShowFeedback("该用户名已被使用", false);
                    return;
                }
            }
        }

        // 密码校验
        if (!string.IsNullOrEmpty(newPwd))
        {
            if (newPwd != confirmPwd)
            {
                ShowFeedback("两次输入的密码不一致", false);
                return;
            }
            if (newPwd.Length < 6)
            {
                ShowFeedback("密码长度至少6位", false);
                return;
            }
        }

        // 二次确认弹窗
        ShowModal("确定要修改账号和密码吗？修改后需重新登录", () =>
        {
            // 执行修改
            var users = LoginRegisterManager.LoadUserDatabase();
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].username == current.username)
                {
                    users[i].username = newName;
                    if (!string.IsNullOrEmpty(newPwd))
                        users[i].passwordHash = HashMD5(newPwd);
                    break;
                }
            }
            LoginRegisterManager.SaveUserDatabase(users);

            // 注销并返回登录
            LoginRegisterManager.Logout();
            ShowFeedback("修改成功，请重新登录", true);
            StartCoroutine(DelayedReturnToLogin(1.5f));
        }, () =>
        {
            modalOverlay.SetActive(false);
        });
    }

    void OnClickExitGame()
    {
        ShowModal("确定要退出游戏吗？", () =>
        {
            LoginRegisterManager.Logout();
            SceneManager.LoadScene(loginSceneName);
        }, () =>
        {
            modalOverlay.SetActive(false);
        });
    }

    void ShowModal(string content, Action onConfirm, Action onCancel)
    {
        textModalContent.text = content;
        this.onModalConfirm = () =>
        {
            modalOverlay.SetActive(false);
            onConfirm?.Invoke();
        };
        this.onModalCancel = () =>
        {
            modalOverlay.SetActive(false);
            onCancel?.Invoke();
        };
        modalOverlay.SetActive(true);
    }

    void ShowFeedback(string msg, bool success)
    {
        textFeedback.text = msg;
        textFeedback.color = success ? Color.green : Color.red;
        textFeedback.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideFeedbackAfter(2f));
    }

    IEnumerator HideFeedbackAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        textFeedback.gameObject.SetActive(false);
    }

    IEnumerator DelayedReturnToLogin(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(loginSceneName);
    }

    static string HashMD5(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = MD5.Create().ComputeHash(bytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}