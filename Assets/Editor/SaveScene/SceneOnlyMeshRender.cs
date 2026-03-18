using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneOnlyMeshRenderer
{
    // 编辑器启动时自动注册
    static SceneOnlyMeshRenderer()
    {
        // 监听场景/游戏状态变化
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        // 编辑器每帧刷新
        EditorApplication.update += UpdateEditor;

        // 初始化一次可见性
        UpdateVisibility();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        UpdateVisibility();
    }

    private static void UpdateEditor()
    {
        UpdateVisibility();
    }

    private static void UpdateVisibility()
    {
        // 查找所有带 SceneOnly 标签的物体
        var targets = Object.FindObjectsOfType<MeshRenderer>(includeInactive: true);

        foreach (var mr in targets)
        {
            if (mr.CompareTag("SceneOnly"))
            {
                // 编辑模式(Scene)显示 | 运行模式(Game)隐藏
                mr.enabled = !EditorApplication.isPlaying;
            }
        }
    }
}