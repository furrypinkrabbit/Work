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
    public float GroundRayCastDistance = 5f;



    public PlayerMoveData(float MoveSpeed = 12f) {
        this.MoveSpeed = MoveSpeed;

    }

}
