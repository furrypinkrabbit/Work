using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SaveScene
{
    public static class SaveSceneTool
    {
        private const string MenuName = "碰撞点工具/";
        private const string XmlDefaultPath = "Assets/Res/Assets/Config/DetectPointsConfig.xml";

        // 保存场景碰撞点
        [MenuItem(MenuName + "保存碰撞点配置", false, 1)]
        public static void SaveDetectPoints()
        {
            // 弹窗确认是否保存
            bool isSave = EditorUtility.DisplayDialog("确认保存", "是否保存当前场景中所有SceneOnly标签的碰撞点？", "是", "否");
            if (!isSave) return;

            // 收集所有SceneOnly标签的碰撞点数据
            List<DetectPointData> dataList = new List<DetectPointData>();
            GameObject[] sceneOnlyObjs = GameObject.FindGameObjectsWithTag("SceneOnly");
            foreach (var obj in sceneOnlyObjs)
            {
                DetectPoint detectPoint = obj.GetComponent<DetectPoint>();
                if (detectPoint == null) continue;

                // 判断类型（Cube/Sphere）
                DetectPointType type = DetectPointType.Cube;
                if (obj.GetComponent<SphereCollider>() != null)
                {
                    type = DetectPointType.Sphere;
                }

                // 构建数据
                DetectPointData data = new DetectPointData(
                    obj.transform,
                    detectPoint.callbackFunction,
                    type
                );
                dataList.Add(data);
            }

            // 打开可视化面板
            DetectPointEditorWindow.OpenSaveWindow(dataList);
        }

        // 加载碰撞点配置
        [MenuItem(MenuName + "加载碰撞点配置", false, 2)]
        public static void LoadDetectPoints()
        {
            // 选择XML文件
            string xmlPath = EditorUtility.OpenFilePanel("选择碰撞点配置XML", "Assets", "xml");
            if (string.IsNullOrEmpty(xmlPath)) return;

            // 转换为工程内路径
            xmlPath = FileUtil.GetProjectRelativePath(xmlPath);

            // 打开可视化面板
            DetectPointEditorWindow.OpenLoadWindow(xmlPath);
        }
    }
}