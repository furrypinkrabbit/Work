#define DeBug
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    
    public PlayerMoveData playerMoveData;
    public LayerMask layerMask;

    private InputAction playerInput;

    private Vector3 MoveDir;
    private Vector2 MouseDelta;


    //初始化
    public void InitPlayerController(PlayerMoveData playerMoveData)
    {
        this.playerMoveData = playerMoveData;
    }

    void Awake()
    {
        InitPlayerController(new PlayerMoveData());

        if (this.GetComponent<Rigidbody>() == null) {
            this.gameObject.AddComponent<Rigidbody>();
        }
        playerMoveData.rb = gameObject.GetComponent<Rigidbody>();
       

    }

    void Start()
    {
        layerMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        MovePlayer();
    }

    void FixedUpdate()
    {
        GroundCheck();
    }

    //跳跃输入
    public void HandleJump(InputAction.CallbackContext ctx) {

        if (playerMoveData.isAir) {
            return;
        }

        if (ctx.performed && playerMoveData.CanJump) {
            playerMoveData.isAir = true;
            playerMoveData.rb.AddForce(Vector3.up * playerMoveData.AirImpulseForce,ForceMode.Impulse);
        }


    }

    //键盘移动输入
    public void HandleMove(InputAction.CallbackContext ctx) {
        if (ctx.performed && playerMoveData.CanMove) {
           Vector2 Dir = ctx.ReadValue<Vector2>();
            MoveDir = new Vector3(Dir.x, 0, Dir.y); 
        }

        if (ctx.canceled) {
            MoveDir = Vector3.zero;
        }
    }


    //地面检测
    void GroundCheck()
    {
        if (Physics.Raycast(playerMoveData.rb.position, Vector3.down, out var hit, playerMoveData.GroundRayCastDistance, layerMask))
        {
            playerMoveData.isAir = false;
            playerMoveData.CanJump = true;
        }
        else
        {
            playerMoveData.isAir = true;
            playerMoveData.CanJump = false;
        }

#if DeBug
        Debug.DrawLine(playerMoveData.rb.position, playerMoveData.rb.position + Vector3.down * playerMoveData.GroundRayCastDistance, Color.red, 0.1f);
#endif

    }


    //角色移动
    void MovePlayer()
    {

        Camera mainCam = Camera.main;
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * MoveDir.z + camRight * MoveDir.x;

        Vector3 targetVelocity = moveDirection * playerMoveData.MoveSpeed;
        targetVelocity.y = playerMoveData.rb.linearVelocity.y;

        playerMoveData.rb.linearVelocity = targetVelocity;
    }
}
