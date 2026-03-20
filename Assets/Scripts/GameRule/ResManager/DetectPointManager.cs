using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 碰撞点管理器（单例）：按需加载碰撞点配置，实例化碰撞点对象
/// </summary>
public class DetectPointManager : MonoBehaviour
{
    // 单例实例
    private static DetectPointManager _instance;
    public static DetectPointManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 场景中创建全局管理器节点
                GameObject managerGo = new GameObject("DetectPointManager");
                _instance = managerGo.AddComponent<DetectPointManager>();
                DontDestroyOnLoad(managerGo);
            }
            return _instance;
        }
    }

    // 缓存已加载的碰撞点数据（key：XML名称，value：碰撞点数据列表）
    private Dictionary<string, List<DetectPointData>> _cachedDetectPointData = new Dictionary<string, List<DetectPointData>>();
    // 缓存已实例化的碰撞点对象（key：XML名称/分组名，value：实例化的GameObject列表）
    private Dictionary<string, List<GameObject>> _cachedDetectPointObjs = new Dictionary<string, List<GameObject>>();

    /// <summary>
    /// 异步加载碰撞点XML数据（按需加载，优先读缓存）
    /// </summary>
    /// <param name="xmlName">碰撞点XML名称（不含后缀，如"Scene1_DetectPoints"）</param>
    /// <param name="onLoaded">加载完成回调（返回数据列表）</param>
    public void LoadDetectPointDataAsync(string xmlName, System.Action<List<DetectPointData>> onLoaded)
    {
        // 优先读缓存，避免重复加载
        if (_cachedDetectPointData.TryGetValue(xmlName, out var cachedData))
        {
            onLoaded?.Invoke(cachedData);
            return;
        }

        // 异步加载XML（避免主线程卡顿）
        StartCoroutine(LoadDetectPointDataCoroutine(xmlName, onLoaded));
    }

    /// <summary>
    /// 协程加载碰撞点XML
    /// </summary>
    private IEnumerator LoadDetectPointDataCoroutine(string xmlName, System.Action<List<DetectPointData>> onLoaded)
    {
        string fullPath = $"{ResourcePathConfig.DetectPointXmlRoot}{xmlName}";
        // 异步加载Resources下的XML文件（也可替换为Addressable）
        var request = Resources.LoadAsync<TextAsset>(fullPath);
        yield return request;

        if (request.asset == null)
        {
            Debug.LogError($"碰撞点XML加载失败：{fullPath}");
            onLoaded?.Invoke(new List<DetectPointData>());
            yield break;
        }

        // 解析XML数据（复用之前的XmlSerializerTool）
        TextAsset xmlAsset = request.asset as TextAsset;
        List<DetectPointData> dataList = null;
        // 临时写入文件（Resources加载的是TextAsset，需转成流解析，也可优化XmlSerializerTool支持内存流）
        string tempPath = Path.Combine(Application.persistentDataPath, $"{xmlName}_temp.xml");
        File.WriteAllText(tempPath, xmlAsset.text);
        dataList = XmlSerializerTool.LoadDetectPointsFromXml(tempPath);
        File.Delete(tempPath); // 删除临时文件

        // 缓存数据
        _cachedDetectPointData[xmlName] = dataList;
        onLoaded?.Invoke(dataList);
    }

    /// <summary>
    /// 实例化碰撞点到场景（按需加载）
    /// </summary>
    /// <param name="xmlName">碰撞点XML名称（用于缓存分组）</param>
    /// <param name="parentTrans">父节点（可选）</param>
    public void InstantiateDetectPoints(string xmlName, Transform parentTrans = null)
    {
        if (!_cachedDetectPointData.ContainsKey(xmlName))
        {
            Debug.LogError($"请先加载碰撞点XML：{xmlName}");
            return;
        }

        // 避免重复实例化
        if (_cachedDetectPointObjs.ContainsKey(xmlName))
        {
            Debug.LogWarning($"碰撞点{xmlName}已实例化，无需重复创建");
            return;
        }

        List<GameObject> objList = new List<GameObject>();
        List<DetectPointData> dataList = _cachedDetectPointData[xmlName];

        // 加载通用碰撞点预制体（如果有）
        GameObject detectPointPrefab = Resources.Load<GameObject>(ResourcePathConfig.DetectPointPrefabPath);
        if (detectPointPrefab == null)
        {
            Debug.LogError($"通用碰撞点预制体加载失败：{ResourcePathConfig.DetectPointPrefabPath}");
            return;
        }

        // 遍历数据实例化
        foreach (var data in dataList)
        {
            GameObject dpObj = Instantiate(detectPointPrefab, parentTrans);
            dpObj.name = data.name;

            // 应用位置/旋转/缩放
            data.ApplyToTransform(dpObj.transform);

            // 添加DetectPoint组件并设置回调
            DetectPoint dpComp = dpObj.GetComponent<DetectPoint>();
            if (dpComp != null)
            {
                dpComp.callbackFunction = data.callbackFunction;
            }

            // 添加对应Collider（Cube/Sphere）
            if (data.type == DetectPointType.Cube)
            {
                if (dpObj.GetComponent<BoxCollider>() == null)
                {
                    dpObj.AddComponent<BoxCollider>();
                }
               var sc = dpObj.GetComponent<SphereCollider>();
                if(sc==null)Destroy(this,0);
            }
            else
            {
                if (dpObj.GetComponent<SphereCollider>() == null)
                {
                    dpObj.AddComponent<SphereCollider>();
                }
                var bc = dpObj.GetComponent<BoxCollider>();
                    if(bc==null)Destroy(this,0);
            }

            objList.Add(dpObj);
        }

        // 缓存实例化的对象
        _cachedDetectPointObjs[xmlName] = objList;
        Debug.Log($"碰撞点{xmlName}实例化完成，数量：{objList.Count}");
    }

    /// <summary>
    /// 释放指定碰撞点资源（实例+数据）
    /// </summary>
    /// <param name="xmlName">碰撞点XML名称</param>
    public void ReleaseDetectPoints(string xmlName)
    {
        // 销毁实例化的对象
        if (_cachedDetectPointObjs.ContainsKey(xmlName))
        {
            foreach (var obj in _cachedDetectPointObjs[xmlName])
            {
                Destroy(obj);
            }
            _cachedDetectPointObjs.Remove(xmlName);
        }

        // 释放缓存的XML数据（可选，根据内存情况决定）
        if (_cachedDetectPointData.ContainsKey(xmlName))
        {
            _cachedDetectPointData.Remove(xmlName);
        }

        // 卸载Resources缓存（可选，谨慎使用）
        Resources.UnloadUnusedAssets();
        Debug.Log($"碰撞点{xmlName}资源已释放");
    }
}