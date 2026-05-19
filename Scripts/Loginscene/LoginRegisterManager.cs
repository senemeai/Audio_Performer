using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;


public class LoginRegisterManager : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelLogin;
    public GameObject panelRegister;

    [Header("登录面板组件")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public TextMeshProUGUI loginErrorText;
    public Button btnLogin;
    public Button btnGoRegister;

    [Header("注册面板组件")]
    public TMP_InputField regUsername;
    public TMP_InputField regPassword;
    public TMP_InputField regConfirmPassword;
    public TextMeshProUGUI regErrorText;
    public Button btnRegister;
    public Button btnGoLogin;

    private static string SAVE_KEY = "MusicPlayer_UserDB";
    public static UserData CurrentUser { get; private set; }
    public static void Logout() => CurrentUser = null;
    void Start()
    {
        if (panelLogin == null || panelRegister == null)
        {
            Debug.LogError("[LoginRegisterManager] panelLogin 或 panelRegister 未绑定！请在 Inspector 中拖入对应 Panel。");
            return;
        }

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);
        ClearErrors();

        btnLogin.onClick.AddListener(OnLogin);
        btnGoRegister.onClick.AddListener(() => SwitchPanel(true));
        btnRegister.onClick.AddListener(OnRegister);
        btnGoLogin.onClick.AddListener(() => SwitchPanel(false));
    }

    void SwitchPanel(bool toRegister)
    {
        panelLogin.SetActive(!toRegister);
        panelRegister.SetActive(toRegister);
        ClearErrors();
    }

    void ClearErrors()
    {
        if (loginErrorText != null) loginErrorText.text = "";
        if (regErrorText != null) regErrorText.text = "";
    }

    // ========== 登录 ==========
    void OnLogin()
    {
        string name = loginUsername.text.Trim();
        string pwd = loginPassword.text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pwd))
        {
            ShowError(loginErrorText, "用户名和密码不能为空");
            return;
        }

        UserData user = FindUser(name);
        if (user == null)
        {
            ShowError(loginErrorText, "用户不存在");
            return;
        }

        string inputHash = HashMD5(pwd);
        if (user.passwordHash != inputHash)
        {
            ShowError(loginErrorText, "密码错误");
            return;
        }

        CurrentUser = user;
        loginErrorText.text = "";
        Debug.Log($"[登录成功] 用户: {name}");

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // ========== 注册 ==========
    void OnRegister()
    {
        string name = regUsername.text.Trim();
        string pwd = regPassword.text;
        string confirm = regConfirmPassword.text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pwd))
        {
            ShowError(regErrorText, "用户名和密码不能为空");
            return;
        }

        if (pwd != confirm)
        {
            ShowError(regErrorText, "两次输入的密码不一致");
            return;
        }

        if (name.Length < 3 || name.Length > 16)
        {
            ShowError(regErrorText, "用户名长度需为 3~16 位");
            return;
        }

        if (pwd.Length < 6)
        {
            ShowError(regErrorText, "密码长度至少 6 位");
            return;
        }

        if (FindUser(name) != null)
        {
            ShowError(regErrorText, "该用户名已被注册");
            return;
        }

        string hash = HashMD5(pwd);
        UserData newUser = new UserData(name, hash);

        var users = LoadUserDatabase();
        users.Add(newUser);
        SaveUserDatabase(users);

        Debug.Log($"[注册成功] 用户: {name}");

        regUsername.text = "";
        regPassword.text = "";
        regConfirmPassword.text = "";
        SwitchPanel(false);
        loginUsername.text = name;
        loginPassword.text = "";
        loginPassword.Select();
    }

    // ========== 密码加密 ==========
    string HashMD5(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = MD5.Create().ComputeHash(bytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    // ========== 静态数据接口（供外部调用） ==========
    public static List<UserData> LoadUserDatabase()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
            return new List<UserData>();

        var db = JsonUtility.FromJson<UserDatabase>(json);
        return db?.users != null ? new List<UserData>(db.users) : new List<UserData>();
    }

    public static void SaveUserDatabase(List<UserData> users)
    {
        var db = new UserDatabase { users = users.ToArray() };
        string json = JsonUtility.ToJson(db);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static void UpdateCurrentUser()
    {
        if (CurrentUser == null) return;
        var users = LoadUserDatabase();
        for (int i = 0; i < users.Count; i++)
        {
            if (users[i].username == CurrentUser.username)
            {
                users[i] = CurrentUser;
                break;
            }
        }
        SaveUserDatabase(users);
    }

    // ========== 辅助方法 ==========
    static UserData FindUser(string username)
    {
        var users = LoadUserDatabase();
        foreach (var u in users)
            if (u.username == username)
                return u;
        return null;
    }

    void ShowError(TextMeshProUGUI textComp, string msg)
    {
        textComp.text = msg;
    }

    // ========== 调试工具 ==========
    [ContextMenu("清空所有用户")]
    void ClearAllUsers()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("已清空所有用户数据");
    }

    [ContextMenu("打印所有用户")]
    void PrintAllUsers()
    {
        var users = LoadUserDatabase();
        Debug.Log($"当前共有 {users.Count} 个用户:");
        foreach (var u in users)
            Debug.Log($"  - {u.username} (注册时间: {u.registerTime})");
    }
}