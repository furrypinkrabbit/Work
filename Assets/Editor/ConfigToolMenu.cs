using UnityEditor;
using System.IO;

namespace NodeConfigTool
{
    public static class ConfigToolMenu
    {
        private const string MenuRoot = "配置工具/";

        /// <summary>
        /// 创建新配置菜单入口
        /// </summary>
        [MenuItem(MenuRoot + "创建配置", false, 1)]
        public static void CreateNewConfig()
        {
            NodeGraphEditorWindow.OpenNewConfigWindow();
        }

        /// <summary>
        /// 修改已有配置菜单入口
        /// </summary>
        [MenuItem(MenuRoot + "修改配置", false, 2)]
        public static void EditExistingConfig()
        {
            string defaultPath = XMLConfigSerializer.DefaultSavePath;
            if (!Directory.Exists(defaultPath))
            {
                defaultPath = "Assets";
            }

            string filePath = EditorUtility.OpenFilePanel("选择要修改的配置文件", defaultPath, "xml");
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                NodeGraphEditorWindow.OpenEditConfigWindow(filePath);
            }
        }
    }
}