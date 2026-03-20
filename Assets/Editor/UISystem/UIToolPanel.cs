using UnityEditor;
using UnityEngine;
using System.IO;

namespace Assets.Editor.UISystem
{
    public class UIToolPanel
    {
        // 1. 创建UI配置
        [MenuItem("UI工具/创建UI配置", false, 100)]
        static void CreateUIConfig()
        {
            UIConfigInputWindow.ShowWindow("创建UI配置", (savePath, uiName) =>
            {
                // 生成初始UI模板
                UISerializer.CreateUITemplate();
                // 记录当前配置信息（方便后续保存时使用）
                EditorPrefs.SetString("CurrentUISavePath", savePath);
                EditorPrefs.SetString("CurrentUIName", uiName);
                EditorUtility.DisplayDialog("提示", "已在Scene中生成UI模板，请调整UI后点击【保存UI配置】", "确定");
            });
        }

        // 2. 保存UI配置
        [MenuItem("UI工具/保存UI配置 &S", false, 101)]
        static void SaveUIConfig()
        {
            // 获取缓存的路径和名称
            string savePath = EditorPrefs.GetString("CurrentUISavePath");
            string uiName = EditorPrefs.GetString("CurrentUIName");

            if (string.IsNullOrWhiteSpace(savePath) || string.IsNullOrWhiteSpace(uiName))
            {
                UIConfigInputWindow.ShowWindow("保存UI配置", (path, name) =>
                {
                    GameObject show = GameObject.Find("show");
                    if (show != null)
                    {
                        UISerializer.SaveUIConfig(show, path, name);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("保存失败", "Scene中未找到show根节点！", "确定");
                    }
                });
                return;
            }

            // 查找Scene中的show根节点
            GameObject showGo = GameObject.Find("show");
            if (showGo == null)
            {
                EditorUtility.DisplayDialog("保存失败", "Scene中未找到show根节点！", "确定");
                return;
            }

            UISerializer.SaveUIConfig(showGo, savePath, uiName);
            // 清空缓存
            EditorPrefs.DeleteKey("CurrentUISavePath");
            EditorPrefs.DeleteKey("CurrentUIName");
        }

        // 3. 拷贝UI配置
        [MenuItem("UI工具/拷贝UI配置", false, 102)]
        static void CopyUIConfig()
        {
            // 第一步：选择要拷贝的Prefab
            string sourcePrefabPath = EditorUtility.OpenFilePanel(
                "选择要拷贝的UI Prefab",
                "Assets/Res/Assets/UIConfig",
                "prefab");

            if (string.IsNullOrEmpty(sourcePrefabPath) || !sourcePrefabPath.Contains(Application.dataPath))
            {
                EditorUtility.DisplayDialog("提示", "未选择有效的Prefab文件！", "确定");
                return;
            }

            // 转换为Unity工程内路径
            string unityPrefabPath = "Assets" + sourcePrefabPath.Replace(Application.dataPath, "");

            // 第二步：输入新UI名称和保存路径
            UIConfigInputWindow.ShowWindow("拷贝UI配置", (newSavePath, newUiName) =>
            {
                // 从源Prefab生成Scene中的GameObject
                UISerializer.LoadPrefabToScene(unityPrefabPath, newUiName);
                // 缓存新配置信息
                EditorPrefs.SetString("CurrentUISavePath", newSavePath);
                EditorPrefs.SetString("CurrentUIName", newUiName);
                EditorUtility.DisplayDialog("提示", "已拷贝UI到Scene，请调整后点击【保存UI配置】", "确定");
            });
        }

        // 4. 删除UI配置
        [MenuItem("UI工具/删除UI配置", false, 103)]
        static void DeleteUIConfig()
        {
            // 选择要删除的Prefab
            string prefabPath = EditorUtility.OpenFilePanel(
                "选择要删除的UI Prefab",
                "Assets/Res/Assets/UIConfig",
                "prefab");

            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.Contains(Application.dataPath))
            {
                EditorUtility.DisplayDialog("提示", "未选择有效的Prefab文件！", "确定");
                return;
            }

            string unityPrefabPath = "Assets" + prefabPath.Replace(Application.dataPath, "");

            // 确认删除
            if (EditorUtility.DisplayDialog("确认删除", $"是否删除以下UI配置？\n{unityPrefabPath}", "确定", "取消"))
            {
                UISerializer.DeleteUIConfig(unityPrefabPath);
                EditorUtility.DisplayDialog("成功", "UI配置已删除！", "确定");
            }
        }
    }
}