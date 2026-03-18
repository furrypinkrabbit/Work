using UnityEngine;
using System;
using System.Reflection;
using Assets.Scripts.GameRule.DetectPointCallBack;

[RequireComponent(typeof(MeshRenderer))]
public class DetectPoint : MonoBehaviour
{
    [Header("碰撞回调配置")]
    [Tooltip("直接拖拽实现IDetectCallBack的组件（优先使用）")]
    public IDetectCallBack detectCallBack; // 改为可序列化显示的字段
    [Tooltip("兼容旧版：通过函数名反射调用（留空则使用接口默认方法）")]
    public string callbackFunction; // 改为可序列化显示的字段

    private Collider _collider;
    // 缓存反射方法，避免每次碰撞重复反射
    private MethodInfo _cachedCallbackMethod;

    private void Awake()
    {
        // 自动添加碰撞体（仅检测，无物理效果）
        AddColliderAuto();

        // 仅检测碰撞，关闭物理效果
        SetColliderPhysics(false);

        // 运行时隐藏Mesh（仅Scene可见）
        SetMeshRendererVisibility();

        // 提前反射缓存回调方法（优化性能）
        CacheCallbackMethod();
    }

    #region 初始化辅助方法
    /// <summary>
    /// 自动添加碰撞体（Cube/Sphere）
    /// </summary>
    private void AddColliderAuto()
    {
        if (TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
        {
            if (mf.sharedMesh.name.Contains("Cube"))
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            else if (mf.sharedMesh.name.Contains("Sphere"))
            {
                _collider = gameObject.AddComponent<SphereCollider>();
            }
        }
        else
        {
            // 兜底：根据Tag/名称判断
            if (gameObject.name.Contains("Cube"))
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            else if (gameObject.name.Contains("Sphere"))
            {
                _collider = gameObject.AddComponent<SphereCollider>();
            }
        }
    }

    /// <summary>
    /// 设置碰撞体物理属性（仅检测）
    /// </summary>
    private void SetColliderPhysics(bool isPhysical)
    {
        if (_collider == null) return;
        _collider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>
    /// 设置MeshRenderer可见性（编辑模式显示，运行模式隐藏）
    /// </summary>
    private void SetMeshRendererVisibility()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = !Application.isPlaying;
    }
    #endregion

    #region 回调方法缓存与执行
    /// <summary>
    /// 提前缓存回调方法（Awake阶段完成，仅执行一次）
    /// </summary>
    private void CacheCallbackMethod()
    {
        // 优先使用拖拽的detectCallBack组件
        if (detectCallBack == null)
        {
            // 兼容旧逻辑：查找当前物体上的IDetectCallBack组件
            detectCallBack = GetComponent<IDetectCallBack>();
            if (detectCallBack == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 未挂载实现IDetectCallBack的组件（如TestCallBack）");
                return;
            }
        }

        // 确定要调用的方法名（留空则用接口默认方法OnDetectCallBack）
        string targetMethodName = string.IsNullOrEmpty(callbackFunction)
            ? nameof(IDetectCallBack.OnDetectCallBack)
            : callbackFunction;

        // 反射获取方法并缓存
        Type callbackType = detectCallBack.GetType();
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        // 优先查找无参方法（接口标准）
        _cachedCallbackMethod = callbackType.GetMethod(targetMethodName, bindingFlags, null, Type.EmptyTypes, null);
        if (_cachedCallbackMethod == null)
        {
            // 备选：查找带Collider参数的重载方法
            _cachedCallbackMethod = callbackType.GetMethod(targetMethodName, bindingFlags, null, new[] { typeof(Collider) }, null);
            if (_cachedCallbackMethod == null)
            {
                Debug.LogError($"[{gameObject.name}] 在{callbackType.Name}中未找到方法：{targetMethodName}（无参/带Collider参数）");
            }
        }
    }

    /// <summary>
    /// 碰撞触发时执行回调
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_cachedCallbackMethod == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 回调方法未缓存，跳过执行");
            return;
        }

        try
        {
            // 根据方法参数类型执行
            if (_cachedCallbackMethod.GetParameters().Length == 0)
            {
                _cachedCallbackMethod.Invoke(detectCallBack, null);
                Debug.Log($"[{gameObject.name}] 执行无参回调：{_cachedCallbackMethod.Name}，碰撞对象: {other?.name}");
            }
            else
            {
                _cachedCallbackMethod.Invoke(detectCallBack, new object[] { other });
                Debug.Log($"[{gameObject.name}] 执行带参回调：{_cachedCallbackMethod.Name}，碰撞对象: {other?.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{gameObject.name}] 执行回调{_cachedCallbackMethod.Name}失败：{e.Message}");
        }
    }
    #endregion

    // 兼容旧工具的设置方法
    public void SetCallbackFunction(string funcName)
    {
        callbackFunction = funcName;
        // 重新缓存方法（工具调用时更新）
        CacheCallbackMethod();
    }
}