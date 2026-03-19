using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SpherePurifyController : MonoBehaviour
{
    [Header("【必设】模型原材质（完全保留原样）")]
    public Material originalMaterial;

    [Header("【必设】球内卡通材质（用上面的Shader创建）")]
    public Material borderlandsMaterial;

    private Renderer _renderer;
    private readonly Material[] _materials = new Material[2];

    void Start()
    {
        _renderer = GetComponent<Renderer>();

        // 安全校验
        if (originalMaterial == null || borderlandsMaterial == null)
        {
            Debug.LogError("请赋值原材质和卡通材质！", this);
            enabled = false;
            return;
        }

        // 材质分层：第一层原材质（完整渲染），第二层卡通材质（仅球内覆盖）
        _materials[0] = originalMaterial;
        _materials[1] = borderlandsMaterial;
        _renderer.materials = _materials;
    }

    // 可选：运行时动态修改卡通材质参数
    public void UpdateBorderlandsSetting(Color baseColor, Color shadowColor, float lightThreshold)
    {
        if (borderlandsMaterial == null) return;
        borderlandsMaterial.SetColor("_BaseColor", baseColor);
        borderlandsMaterial.SetColor("_ShadowColor", shadowColor);
        borderlandsMaterial.SetFloat("_LightThreshold", lightThreshold);
    }

    // 销毁时还原，避免材质泄漏
    void OnDestroy()
    {
        if (_renderer != null)
        {
            _renderer.materials = new Material[] { originalMaterial };
        }
    }
}