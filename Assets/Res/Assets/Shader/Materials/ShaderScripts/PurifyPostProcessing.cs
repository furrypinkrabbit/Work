using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PurifyPostProcess : MonoBehaviour
{
    [Header("–ßπ˚…Ë÷√")]
    public Color lineColor = Color.yellow;
    public float lineWidth = 0.15f;
    public float blendIntensity = 1f;
    public Texture purifiedTexture;

    [Header("≈Ú’Õ«Ú")]
    public Transform sphereCenter;
    public float expandSpeed = 3f;
    public float maxRadius = 15f;
    private float currentRadius;

    private Material postMat;
    private Shader postShader;

    void Awake()
    {
        postShader = Shader.Find("Hidden/PostProcessPurify");
        postMat = new Material(postShader);
    }

    void Update()
    {
        if (sphereCenter == null) return;

        if (currentRadius < maxRadius)
            currentRadius += expandSpeed * Time.deltaTime;

        Shader.SetGlobalVector("_PurifySphereCenter", sphereCenter.position);
        Shader.SetGlobalFloat("_PurifySphereRadius", currentRadius);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (postMat == null || sphereCenter == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        postMat.SetColor("_LineColor", lineColor);
        postMat.SetFloat("_LineWidth", lineWidth);
        postMat.SetFloat("_BlendIntensity", blendIntensity);
        postMat.SetTexture("_PurifiedTex", purifiedTexture);

        Graphics.Blit(source, destination, postMat);
    }
}