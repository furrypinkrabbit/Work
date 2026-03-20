using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMoveData", menuName = "Scriptable Objects/PlayerMoveData")]
public class PlayerMoveData : ScriptableObject
{
    public float MoveSpeed = 12f;
    public bool isAir = false;
    public bool CanJump = true;
    public bool CanMove = true;
    public float AirImpulseForce = 7f;
    public Rigidbody rb;
    public float GroundRayCastDistance = 0.5f;
    public bool JumpPreRead = false;
    public float PreReadTime = 1f;
    public bool CanPreReadJump = false;

    public PlayerMoveData(float MoveSpeed = 12f) {
        this.MoveSpeed = MoveSpeed;

    }

}
