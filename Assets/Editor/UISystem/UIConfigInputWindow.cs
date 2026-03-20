using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UISystem
{
    public class UIConfigInputWindow : EditorWindow
    {
        // 回调委托
        public delegate void OnConfirm(string savePath, string uiName);

        // 窗口参数
        private string _uiName = "";
        private string _selectedPath = "Assets/Res/Assets/UIConfig";
        private OnConfirm _onConfirmCallback;

        // 打开窗口
        public static void ShowWindow(string title, OnConfirm onConfirm)
        {
            var window = GetWindow<UIConfigInputWindow>(true, title, true);
            window.minSize = new Vector2(400, 150);
            window.maxSize = new Vector2(400, 150);
            window._onConfirmCallback = onConfirm;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // UI名称输入
            GUILayout.Label("UI名称（作为Prefab名称）：", EditorStyles.boldLabel);
            _uiName = EditorGUILayout.TextField("名称", _uiName);

            GUILayout.Space(10);

            // 保存路径选择
            GUILayout.Label("保存路径：", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            _selectedPath = EditorGUILayout.TextField("路径", _selectedPath);
            if (GUILayout.Button("选择路径", GUILayout.Width(80)))
            {
                string selectPath = EditorUtility.OpenFolderPanel("选择保存路径", _selectedPath, "");
                if (!string.IsNullOrEmpty(selectPath) && selectPath.Contains(Application.dataPath))
                {
                    // 转换为Unity工程内的相对路径
                    _selectedPath = "Assets" + selectPath.Replace(Application.dataPath, "");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // 确认/取消按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("确定", GUILayout.Height(30)))
            {
                if (string.IsNullOrWhiteSpace(_uiName))
                {
                    EditorUtility.DisplayDialog("提示", "UI名称不能为空！", "确定");
                    return;
                }
                if (string.IsNullOrWhiteSpace(_selectedPath))
                {
                    EditorUtility.DisplayDialog("提示", "保存路径不能为空！", "确定");
                    return;
                }

                _onConfirmCallback?.Invoke(_selectedPath, _uiName);
                Close();
            }
            if (GUILayout.Button("取消", GUILayout.Height(30)))
            {
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}