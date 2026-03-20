using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI管理器（单例）：按需加载UI Prefab/XML，管理UI显示/隐藏/释放
/// </summary>
public class UIManager : MonoBehaviour
{
    // 单例实例
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerGo = new GameObject("UIManager");
                _instance = managerGo.AddComponent<UIManager>();
                DontDestroyOnLoad(managerGo);
            }
            return _instance;
        }
    }

    // 缓存已加载的UI Prefab（key：UI名称，value：预制体）
    private Dictionary<string, GameObject> _cachedUIPrefabs = new Dictionary<string, GameObject>();
    // 缓存已实例化的UI对象（key：UI名称，value：实例化的GameObject）
    private Dictionary<string, GameObject> _cachedUIInstances = new Dictionary<string, GameObject>();
    // 场景中的Canvas（UI根节点）
    private Canvas _uiCanvas;

    private void Awake()
    {
        // 初始化Canvas（场景中没有则创建）
        InitUICanvas();
    }

    /// <summary>
    /// 初始化UI Canvas
    /// </summary>
    private void InitUICanvas()
    {
        _uiCanvas = GameObject.Find(ResourcePathConfig.UICanvasName)?.GetComponent<Canvas>();
        if (_uiCanvas == null)
        {
            GameObject canvasGo = new GameObject(ResourcePathConfig.UICanvasName);
            _uiCanvas = canvasGo.AddComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);
        }
    }

    /// <summary>
    /// 异步加载UI Prefab（优先读缓存）
    /// </summary>
    /// <param name="uiName">UI名称（Prefab文件名）</param>
    /// <param name="onLoaded">加载完成回调</param>
    public void LoadUIPrefabAsync(string uiName, System.Action<GameObject> onLoaded)
    {
        // 优先读缓存
        if (_cachedUIPrefabs.TryGetValue(uiName, out var cachedPrefab))
        {
            onLoaded?.Invoke(cachedPrefab);
            return;
        }

        // 异步加载Prefab
        StartCoroutine(LoadUIPrefabCoroutine(uiName, onLoaded));
    }

    /// <summary>
    /// 协程加载UI Prefab
    /// </summary>
    private IEnumerator LoadUIPrefabCoroutine(string uiName, System.Action<GameObject> onLoaded)
    {
        string fullPath = $"{ResourcePathConfig.UIPrefabRoot}{uiName}";
        var request = Resources.LoadAsync<GameObject>(fullPath);
        yield return request;

        if (request.asset == null)
        {
            Debug.LogError($"UI Prefab加载失败：{fullPath}");
            onLoaded?.Invoke(null);
            yield break;
        }

        GameObject prefab = request.asset as GameObject;
        _cachedUIPrefabs[uiName] = prefab; // 缓存Prefab
        onLoaded?.Invoke(prefab);
    }

    /// <summary>
    /// 加载UI布局XML（可选，用于动态调整布局）
    /// </summary>
    /// <param name="uiName">UI名称</param>
    /// <param name="onLoaded">加载完成回调（返回UIXmlNode）</param>
    public void LoadUILayoutXmlAsync(string uiName, System.Action<UIXmlNode> onLoaded)
    {
        StartCoroutine(LoadUILayoutXmlCoroutine(uiName, onLoaded));
    }

    private IEnumerator LoadUILayoutXmlCoroutine(string uiName, System.Action<UIXmlNode> onLoaded)
    {
        string fullPath = $"{ResourcePathConfig.UILayoutXmlRoot}{uiName}/layout";
        var request = Resources.LoadAsync<TextAsset>(fullPath);
        yield return request;

        if (request.asset == null)
        {
            Debug.LogError($"UI布局XML加载失败：{fullPath}");
            onLoaded?.Invoke(null);
            yield break;
        }

        // 解析XML（复用UISerializer的逻辑，需调整为支持内存流）
        TextAsset xmlAsset = request.asset as TextAsset;
        UIXmlNode rootNode = null;
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(UIXmlNode.UIXmlConfig));
            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlAsset.text)))
            {
                var config = serializer.Deserialize(ms) as UIXmlNode.UIXmlConfig;
                rootNode = config?.Nodes?[0];
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UI XML解析失败：{e.Message}");
        }

        onLoaded?.Invoke(rootNode);
    }

    /// <summary>
    /// 实例化并显示UI（按需加载）
    /// </summary>
    /// <param name="uiName">UI名称</param>
    /// <param name="onInstantiated">实例化完成回调</param>
    public void ShowUI(string uiName, System.Action<GameObject> onInstantiated = null)
    {
        // 已实例化则直接显示
        if (_cachedUIInstances.TryGetValue(uiName, out var existingUI))
        {
            existingUI.SetActive(true);
            onInstantiated?.Invoke(existingUI);
            return;
        }

        // 先加载Prefab，再实例化
        LoadUIPrefabAsync(uiName, (prefab) =>
        {
            if (prefab == null) return;

            // 实例化到Canvas下
            GameObject uiInstance = Instantiate(prefab, _uiCanvas.transform);
            uiInstance.name = uiName;
            _cachedUIInstances[uiName] = uiInstance; // 缓存实例

            // 可选：加载XML并动态调整布局
            LoadUILayoutXmlAsync(uiName, (rootNode) =>
            {
                if (rootNode != null)
                {
                    // 这里实现根据XML调整UI布局的逻辑（复用UISerializer的ConvertXmlNodeToGo）
                    // UISerializer.ConvertXmlNodeToGo(rootNode, uiInstance.transform);
                }
            });

            onInstantiated?.Invoke(uiInstance);
            Debug.Log($"UI {uiName} 已显示");
        });
    }

    /// <summary>
    /// 隐藏指定UI（不销毁，仅隐藏）
    /// </summary>
    /// <param name="uiName">UI名称</param>
    public void HideUI(string uiName)
    {
        if (_cachedUIInstances.TryGetValue(uiName, out var uiInstance))
        {
            uiInstance.SetActive(false);
            Debug.Log($"UI {uiName} 已隐藏");
        }
        else
        {
            Debug.LogWarning($"UI {uiName} 未实例化，无需隐藏");
        }
    }

    /// <summary>
    /// 释放指定UI资源（销毁实例+清空Prefab缓存）
    /// </summary>
    /// <param name="uiName">UI名称</param>
    public void ReleaseUI(string uiName)
    {
        // 销毁实例
        if (_cachedUIInstances.ContainsKey(uiName))
        {
            Destroy(_cachedUIInstances[uiName]);
            _cachedUIInstances.Remove(uiName);
        }

        // 释放Prefab缓存（可选，根据内存情况）
        if (_cachedUIPrefabs.ContainsKey(uiName))
        {
            _cachedUIPrefabs.Remove(uiName);
        }

        // 卸载未使用的Resources资源
        Resources.UnloadUnusedAssets();
        Debug.Log($"UI {uiName} 资源已释放");
    }
}