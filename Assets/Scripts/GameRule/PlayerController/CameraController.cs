using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("相机设置")]
    public float mouseSensitivity = 100f;      // 鼠标灵敏度
    public float minClamp = -60f;              // 低头最小角度
    public float maxClamp = 70f;              // 抬头最大角度

    [Header("绑定对象")]
    public Transform playerBody;              //角色根节点

    private float xRotation = 0f;
    private Vector2 mouseDelta;

    void Awake()
    {
        // 隐藏并锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void HandleMouseMove(InputAction.CallbackContext ctx) {
        if (ctx.performed) {

            mouseDelta = ctx.ReadValue<Vector2>();
       }

        if (ctx.canceled) {
            mouseDelta = Vector2.zero;
        }
    }

    void LateUpdate()
    {
        // 计算旋转
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // 上下旋转（限制角度）
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minClamp, maxClamp);

        // 应用旋转
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}