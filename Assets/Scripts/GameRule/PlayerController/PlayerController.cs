#define DeBug
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerMoveData playerMoveData;
    public LayerMask layerMask;

    private InputAction playerInput;

    private Vector3 MoveDir;
    private Vector2 MouseDelta;

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

    public void HandleJump(InputAction.CallbackContext ctx) {

        if (playerMoveData.isAir) {
            return;
        }

        if (ctx.performed && playerMoveData.CanJump) {
            playerMoveData.isAir = true;
            playerMoveData.rb.AddForce(Vector3.up * playerMoveData.AirImpulseForce,ForceMode.Impulse);
        }


    }

    public void HandleMove(InputAction.CallbackContext ctx) {
        if (ctx.performed && playerMoveData.CanMove) {
           Vector2 Dir = ctx.ReadValue<Vector2>();
            MoveDir = new Vector3(Dir.x, 0, Dir.y); 
        }

        if (ctx.canceled) {
            MoveDir = Vector3.zero;
        }
    }


    public void InitPlayerController(PlayerMoveData playerMoveData)
    {
        this.playerMoveData = playerMoveData;
    }


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

    void MovePlayer()
    {
        Vector3 targetVelocity = MoveDir * playerMoveData.MoveSpeed;

        targetVelocity.y = playerMoveData.rb.linearVelocity.y;

        playerMoveData.rb.linearVelocity = targetVelocity;
    }
}
