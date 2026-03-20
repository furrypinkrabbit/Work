using UnityEngine;

/// <summary>
/// 资源路径常量（统一管理，避免硬编码）
/// </summary>
public static class ResourcePathConfig
{
    // ========== 碰撞点相关 ==========
    /// <summary>碰撞点XML根路径（Resources目录下，支持Resources加载）</summary>
    public const string DetectPointXmlRoot = "DetectPoints/";
    /// <summary>碰撞点默认预制体路径（如果有通用碰撞点预制体）</summary>
    public const string DetectPointPrefabPath = "Prefabs/DetectPointBase";

    // ========== UI相关 ==========
    /// <summary>UI Prefab根路径（推荐用Addressable，这里先兼容Resources）</summary>
    public const string UIPrefabRoot = "UI/";
    /// <summary>UI布局XML根路径</summary>
    public const string UILayoutXmlRoot = "UIConfig/";

    // ========== 通用 ==========
    /// <summary>UI Canvas节点名称（场景中固定Canvas）</summary>
    public const string UICanvasName = "UICanvas";
}