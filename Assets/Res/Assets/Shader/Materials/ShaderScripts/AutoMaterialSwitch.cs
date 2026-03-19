using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AutoMaterialSwitch : MonoBehaviour
{
    [Header("你的两个材质（任意Shader都能用）")]
    public Material originalMat;
    public Material newMat;

    private Renderer _render;
    private MaterialPropertyBlock _block;
    private float _radius;

    void Start()
    {
        _render = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        _render.material = originalMat;
    }

    void Update()
    {
        _radius = Shader.GetGlobalFloat("_PurifySphereRadius");

        if (_radius > 0.001f)
        {
            SwitchToDualMaterial();
        }
        else
        {
            _render.material = originalMat;
        }
    }

    void SwitchToDualMaterial()
    {
        if (_render.material.shader.name != "Custom/AutoDualMaterialSphere")
        {
            Debug.LogError("Have to Create a new mat:");
            var mat = new Material(Shader.Find("Custom/AutoDualMaterialSphere"));
            _render.material = mat;
        }
    }
}