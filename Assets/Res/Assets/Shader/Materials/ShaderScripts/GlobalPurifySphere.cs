using UnityEngine;

public class GlobalPurifySphere : MonoBehaviour
{
    [Header("膨胀球设置")]
    public float expandSpeed = 3f;
    public float maxRadius = 15f;
    private float currentRadius;

    // Shader 全局参数名称
    private readonly string centerID = "_PurifySphereCenter";
    private readonly string radiusID = "_PurifySphereRadius";

    void Start()
    {
        currentRadius = 0;
    }

    void Update()
    {
        // 膨胀
        if (currentRadius < maxRadius)
        {
            currentRadius += expandSpeed * Time.deltaTime;
        }

        // 向所有 Shader 发送球心 + 半径（全局生效）
        Shader.SetGlobalVector(centerID, transform.position);
        Shader.SetGlobalFloat(radiusID, currentRadius);
    }

    // 可以用动画/事件调用重置
    public void ResetSphere()
    {
        currentRadius = 0;
        Shader.SetGlobalFloat(radiusID, 0);
    }
}