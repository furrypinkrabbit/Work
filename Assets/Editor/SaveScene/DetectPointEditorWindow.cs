using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DetectPointEditorWindow : EditorWindow
{
    private List<DetectPointData> _dataList = new List<DetectPointData>();
    private Vector2 _scrollPos;
    private bool _isSaveMode; // true=保存模式，false=加载模式
    private string _xmlPath = "Assets/DetectPointsConfig.xml"; // 默认XML路径
    private System.Action<List<DetectPointData>> _onConfirm; // 确认回调

    // 打开窗口（保存模式）
    public static DetectPointEditorWindow OpenSaveWindow(List<DetectPointData> dataList)
    {
        DetectPointEditorWindow window = GetWindow<DetectPointEditorWindow>("保存碰撞点配置");
        window._dataList = new List<DetectPointData>(dataList);
        window._isSaveMode = true;
        window._onConfirm = (list) =>
        {
            XmlSerializerTool.SaveDetectPointsToXml(list, window._xmlPath);
            EditorUtility.DisplayDialog("成功", "碰撞点配置已保存到XML！", "确定");
        };
        return window;
    }

    // 打开窗口（加载模式）
    public static DetectPointEditorWindow OpenLoadWindow(string xmlPath)
    {
        DetectPointEditorWindow window = GetWindow<DetectPointEditorWindow>("加载碰撞点配置");
        window._xmlPath = xmlPath;
        window._dataList = XmlSerializerTool.LoadDetectPointsFromXml(xmlPath);
        window._isSaveMode = false;
        window._onConfirm = (list) =>
        {
            SpawnDetectPointsFromData(list);
            EditorUtility.DisplayDialog("成功", "碰撞点已从XML还原到场景！", "确定");
        };
        return window;
    }

    private void OnGUI()
    {
        // 路径配置
        GUILayout.Label("XML路径:", EditorStyles.boldLabel);
        _xmlPath = EditorGUILayout.TextField(_xmlPath);

        // 列表显示
        GUILayout.Space(10);
        GUILayout.Label("碰撞点列表:", EditorStyles.boldLabel);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));

        for (int i = 0; i < _dataList.Count; i++)
        {
            DetectPointData data = _dataList[i];
            GUILayout.BeginHorizontal("box");

            // 基本信息
            GUILayout.Label($"名称: {data.name}", GUILayout.Width(120));
            GUILayout.Label($"位置: ({data.posX:F1},{data.posY:F1},{data.posZ:F1})", GUILayout.Width(180));
            GUILayout.Label($"旋转: ({data.rotX:F0},{data.rotY:F0},{data.rotZ:F0})", GUILayout.Width(150));
            GUILayout.Label($"缩放: ({data.scaleX:F1},{data.scaleY:F1},{data.scaleZ:F1})", GUILayout.Width(150));
            GUILayout.Label($"回调: {data.callbackFunction}", GUILayout.Width(120));
            GUILayout.Label($"类型: {data.type}", GUILayout.Width(80));

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 确认按钮
        GUILayout.Space(10);
        if (GUILayout.Button("确定", GUILayout.Height(40)))
        {
            _onConfirm?.Invoke(_dataList);
            Close();
        }
    }

    // 从数据生成碰撞点物体
    private static void SpawnDetectPointsFromData(List<DetectPointData> dataList)
    {
        // 清空原有SceneOnly标签的碰撞点（可选）
        GameObject[] oldPoints = GameObject.FindGameObjectsWithTag("SceneOnly");
        foreach (var obj in oldPoints)
        {
            if (obj.GetComponent<DetectPoint>() != null)
            {
                DestroyImmediate(obj);
            }
        }

        // 加载预制体（需提前将Cube/Sphere预制体放在Resources目录）
        GameObject cubePrefab = Resources.Load<GameObject>("DetectPointCube");
        GameObject spherePrefab = Resources.Load<GameObject>("DetectPointSphere");
        if (cubePrefab == null || spherePrefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请在Resources目录下放置DetectPointCube和DetectPointSphere预制体！", "确定");
            return;
        }

        // 生成新的碰撞点
        foreach (var data in dataList)
        {
            GameObject prefab = data.type == DetectPointType.Cube ? cubePrefab : spherePrefab;
            GameObject pointObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            pointObj.name = data.name;
            pointObj.tag = "SceneOnly";

            // 应用Transform数据
            data.ApplyToTransform(pointObj.transform);

            // 设置回调函数
            DetectPoint detectPoint = pointObj.GetComponent<DetectPoint>();
            if (detectPoint != null)
            {
                detectPoint.SetCallbackFunction(data.callbackFunction);
            }
        }
    }
}