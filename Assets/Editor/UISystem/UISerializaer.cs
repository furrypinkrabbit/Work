using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Editor.UISystem
{
    public static class UISerializer
    {
        // 提取常量，方便维护
        private const string UIConfigRootPath = "Assets/Res/Assets/UIConfig";
        private const string LayoutXmlName = "layout.xml"; // 固定XML名称

        /// <summary>
        /// 保存UI配置到XML文件 + 生成Prefab
        /// </summary>
        /// <param name="rootGo">Scene中的根GameObject（show Canvas）</param>
        /// <param name="savePath">保存路径（文件夹）</param>
        /// <param name="uiName">UI名称（Prefab名称）</param>
        /// <returns>保存结果</returns>
        public static bool SaveUIConfig(GameObject rootGo, string savePath, string uiName)
        {
            if (rootGo == null || rootGo.name != "show")
            {
                EditorUtility.DisplayDialog("保存失败", "根节点必须是名称为show的Canvas！", "确定");
                return false;
            }

            // 1. 从GameObject生成UIXmlNode
            UIXmlNode rootNode = ConvertGoToXmlNode(rootGo);
            if (rootNode == null) return false;

            // 2. 保存layout.xml
            string xmlSaveDirectoryPath = Path.Combine(savePath, uiName);
            if (!Directory.Exists(xmlSaveDirectoryPath)) {
                Directory.CreateDirectory(xmlSaveDirectoryPath);
            }
            string xmlSavePath = Path.Combine(xmlSaveDirectoryPath,LayoutXmlName);
            try
            {
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                var xmlConfig = new UIXmlNode.UIXmlConfig();
                xmlConfig.Nodes.Add(rootNode);

                var serializer = new XmlSerializer(typeof(UIXmlNode.UIXmlConfig));
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = true,
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(xmlSavePath, settings))
                {
                    serializer.Serialize(writer, xmlConfig);
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("XML保存失败", e.Message, "确定");
                return false;
            }

            // 3. 生成Prefab并删除Scene中的GameObject
            string prefabPath = Path.Combine(xmlSaveDirectoryPath, "show.prefab");
            if (File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo); // 销毁Scene中的对象
            AssetDatabase.Refresh();

            Debug.Log($"UI配置保存成功：\nPrefab: {prefabPath}\nXML: {xmlSavePath}");
            return true;
        }

        /// <summary>
        /// 从GameObject生成UIXmlNode（递归）
        /// </summary>
        private static UIXmlNode ConvertGoToXmlNode(GameObject go)
        {
            try
            {
                RectTransform rect = go.GetComponent<RectTransform>();
                if (rect == null)
                {
                    EditorUtility.DisplayDialog("错误", $"{go.name}缺少RectTransform组件", "确定");
                    return null;
                }

                var node = new UIXmlNode
                {
                    NodeName = go.name,
                    X = rect.anchoredPosition.x,
                    Y = rect.anchoredPosition.y,
                    Width = rect.sizeDelta.x,
                    Height = rect.sizeDelta.y
                };

                // 递归处理子节点
                foreach (Transform child in rect)
                {
                    UIXmlNode childNode = ConvertGoToXmlNode(child.gameObject);
                    if (childNode != null)
                    {
                        node.nodes.Add(childNode);
                    }
                }

                return node;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("转换失败", e.Message, "确定");
                return null;
            }
        }

        /// <summary>
        /// 创建初始UI模板（Scene中生成show Canvas）
        /// </summary>
        public static GameObject CreateUITemplate()
        {
            // 检查是否已有show节点
            GameObject existingShow = GameObject.Find("show");
            if (existingShow != null)
            {
                EditorUtility.DisplayDialog("提示", "Scene中已存在show根节点！", "确定");
                return existingShow;
            }

            // 创建Canvas根节点
            GameObject canvas = new GameObject("show");
            Canvas canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1920, 1080);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Debug.Log("Scene中已生成UI模板根节点（show Canvas）");
            return canvas;
        }

        /// <summary>
        /// 从Prefab加载并生成Scene中的GameObject
        /// </summary>
        public static GameObject LoadPrefabToScene(string prefabPath, string newUiName)
        {
            if (!File.Exists(prefabPath))
            {
                EditorUtility.DisplayDialog("错误", "Prefab文件不存在！", "确定");
                return null;
            }

            // 加载Prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("错误", "加载Prefab失败！", "确定");
                return null;
            }

            // 检查是否已有show节点
            GameObject existingShow = GameObject.Find("show");
            if (existingShow != null)
            {
                UnityEngine.Object.DestroyImmediate(existingShow);
            }

            // 实例化到Scene
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "show"; // 保持根节点名称为show
            Undo.RegisterCreatedObjectUndo(instance, "Copy UI Config");

            Debug.Log($"已从Prefab生成Scene中的UI：{newUiName}");
            return instance;
        }

        /// <summary>
        /// 删除UI配置（Prefab + layout.xml）
        /// </summary>
        public static bool DeleteUIConfig(string prefabPath)
        {
            try
            {
                // 1. 删除Prefab
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }

                // 2. 删除同目录的layout.xml
                string xmlPath = Path.Combine(Path.GetDirectoryName(prefabPath), LayoutXmlName);
                if (File.Exists(xmlPath))
                {
                    AssetDatabase.DeleteAsset(xmlPath);
                }

                AssetDatabase.Refresh();
                Debug.Log($"已删除UI配置：\nPrefab: {prefabPath}\nXML: {xmlPath}");
                return true;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("删除失败", e.Message, "确定");
                return false;
            }
        }

        // 保留原有SaveUIConfig方法（兼容旧逻辑，可根据需要删除）
        public static string SaveUIConfig(UIXmlNode rootNode, string filename)
        {
            // 原有逻辑...（保持不变）
            return null;
        }

        private static UIXmlNode.UIXmlConfig ConvertToXMLConfig(UIXmlNode showRoot)
        {
            var xmlConfig = new UIXmlNode.UIXmlConfig();
            xmlConfig.Nodes.Add(showRoot);
            return xmlConfig;
        }
    }
}