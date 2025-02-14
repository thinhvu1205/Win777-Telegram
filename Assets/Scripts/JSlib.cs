using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class JSlib : MonoBehaviour
{
    public static void OpenUrlInNewTab(string url)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
    OpenNewTab(url);
#endif
    }

    #region DllImport
    [DllImport("__Internal")] public static extern void createWebSocket();
    [DllImport("__Internal")] public static extern void closeWebSocket();
    [DllImport("__Internal")] public static extern void OpenNewTab(string url);
    #endregion
}
