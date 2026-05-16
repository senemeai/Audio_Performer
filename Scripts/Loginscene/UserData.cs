using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public string username;
    public string passwordHash;      // MD5 哈希，不存明文
    public long registerTime;        // 注册时间戳
    public List<ScoreIndexEntry> scoreIndex; // 乐谱索引列表，为后续功能预留

    public UserData(string name, string hash)
    {
        username = name;
        passwordHash = hash;
        registerTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        scoreIndex = new List< ScoreIndexEntry > ();
    }
}

// 乐谱索引条目（后续乐谱库功能使用）
[Serializable]
public class ScoreIndexEntry
{
    public string displayName;       // 用户自定义显示名
    public string filePath;          // 绝对路径
    public string style;             // 风格
    public int totalBars;            // 小节数
    public long saveTime;            // 保存时间戳
    public string remark;            // 文字备注
}