using UnityEngine;
using System;
#if UNITY_STANDALONE_WIN
using System.Windows.Forms;
#endif

public static class FileDialogHelper
{
    public static string OpenFile(string title, string filter, string initialDir = "")
    {
#if UNITY_STANDALONE_WIN
        using (OpenFileDialog dlg = new OpenFileDialog())
        {
            dlg.Title = title;
            dlg.Filter = filter;
            if (!string.IsNullOrEmpty(initialDir)) dlg.InitialDirectory = initialDir;
            if (dlg.ShowDialog() == DialogResult.OK)
                return dlg.FileName;
        }
#elif UNITY_STANDALONE_OSX
        Debug.LogWarning("Mac平台请导入 StandaloneFileBrowser 插件以支持系统对话框");
#else
        Debug.LogWarning("当前平台不支持系统文件对话框");
#endif
        return "";
    }

    public static string SaveFile(string title, string filter, string defaultFileName = "", string initialDir = "")
    {
#if UNITY_STANDALONE_WIN
        using (SaveFileDialog dlg = new SaveFileDialog())
        {
            dlg.Title = title;
            dlg.Filter = filter;
            dlg.FileName = defaultFileName;
            if (!string.IsNullOrEmpty(initialDir)) dlg.InitialDirectory = initialDir;
            if (dlg.ShowDialog() == DialogResult.OK)
                return dlg.FileName;
        }
#elif UNITY_STANDALONE_OSX
        Debug.LogWarning("Mac平台请导入 StandaloneFileBrowser 插件以支持系统对话框");
#else
        Debug.LogWarning("当前平台不支持系统文件对话框");
#endif
        return "";
    }
}