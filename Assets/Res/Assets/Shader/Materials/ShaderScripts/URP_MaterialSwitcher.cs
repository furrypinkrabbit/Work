using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class URP_MaterialSwitcher : MonoBehaviour
{
    [Header("原材质（模型原本的材质）")]
    public Material originalMaterial;

    [Header("BorderlandsGun 设置")]
    public Color BL_BaseColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color BL_ShadowColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    public Color BL_OutlineColor = Color.black;
    [Range(0, 0.05f)] public float BL_OutlineWidth = 0.01f;
    [Range(0, 1)] public float BL_LightThreshold = 0.5f;
    [Range(0, 1)] public float BL_Gloss = 0.2f;

    [Header("球体相交描边")]
    public Color sphereLineColor = Color.yellow;
    [Range(0.01f, 0.3f)] public float sphereLineWidth = 0.1f;

    private Renderer _render;
    private Material _switchMat;
    private static readonly int ShaderProp_MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int ShaderProp_BaseColor = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        _render = GetComponent<Renderer>();

        // 初始化：先显示原材质
        if (originalMaterial != null)
        {
            _render.material = originalMaterial;
        }
    }

    void Update()
    {
        float radius = Shader.GetGlobalFloat("_PurifySphereRadius");

        if (radius > 0.001f)
        {
            SwitchToCustomShader(radius);
        }
        else
        {
            // 恢复原材质
            if (originalMaterial != null && _render.material != originalMaterial)
            {
                _render.material = originalMaterial;
            }
        }
    }

    void SwitchToCustomShader(float currentRadius)
    {
        // 1. 创建切换材质
        if (_switchMat == null)
        {
            Shader shader = Shader.Find("Custom/URP_OriginalToBorderlands");
            if (shader == null)
            {
                Debug.LogError("找不到 Shader: Custom/URP_OriginalToBorderlands");
                return;
            }
            _switchMat = new Material(shader);
        }

        // 2. 自动复制原材质属性
        if (originalMaterial != null)
        {
            if (originalMaterial.HasProperty(ShaderProp_MainTex))
            {
                _switchMat.SetTexture(ShaderProp_MainTex, originalMaterial.GetTexture(ShaderProp_MainTex));
            }
            if (originalMaterial.HasProperty(ShaderProp_BaseColor))
            {
                _switchMat.SetColor(ShaderProp_BaseColor, originalMaterial.GetColor(ShaderProp_BaseColor));
            }
            // 兼容 Standard Shader
            else if (originalMaterial.HasProperty("_Color"))
            {
                _switchMat.SetColor(ShaderProp_BaseColor, originalMaterial.GetColor("_Color"));
            }
        }

        // 3. 设置 BorderlandsGun 属性
        _switchMat.SetColor("_BL_BaseColor", BL_BaseColor);
        _switchMat.SetColor("_BL_ShadowColor", BL_ShadowColor);
        _switchMat.SetColor("_BL_OutlineColor", BL_OutlineColor);
        _switchMat.SetFloat("_BL_OutlineWidth", BL_OutlineWidth);
        _switchMat.SetFloat("_BL_LightThreshold", BL_LightThreshold);
        _switchMat.SetFloat("_BL_Gloss", BL_Gloss);

        // 4. 设置球体描边属性
        _switchMat.SetColor("_SphereLineColor", sphereLineColor);
        _switchMat.SetFloat("_SphereLineWidth", sphereLineWidth);

        // 5. 应用材质
        _render.material = _switchMat;
    }

    void OnDestroy()
    {
        if (_switchMat != null) DestroyImmediate(_switchMat);
    }
}