using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction {
    Up, Down, Left, Right
}

public class PlayerController : MonoBehaviour {

    public PlayerGameObject playerObject;
    public InterpolationTransform interpTransfom;

    [Header("Parameters")]
    public float walkSpeed;
    public float walkAcceleration;
    public float groundFriction;
    public float gravity;
    public float jumpForce;

    new public CharacterController rigidbody;
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Direction visualDirection { get; private set; }
    [HideInInspector] public bool isMoving { get; private set; }
    [HideInInspector] public bool isGrounded { get; private set; }
    [HideInInspector] public Vector3 lastestDirection { get; private set; }

    private Vector3 noneSelfControlledDirection;

    void Start () {
        
    }
    
    void FixedUpdate () {
        Vector3 vel = velocity;

        bool doNotMove = playerObject.playerAnimator.isDrawing;
        bool doNotExecuteActions = playerObject.playerAnimator.isDrawing || !playerObject.isSelfControlled;

        if(playerObject.playerAnimator.isDrawing)
            visualDirection = Direction.Down;

        Vector3 direction = Vector3.zero;
        if(Input.GetKey(KeyCode.S) && !doNotMove)
            direction += Vector3.back;
        if(Input.GetKey(KeyCode.W) && !doNotMove)
            direction += Vector3.forward;
        if(Input.GetKey(KeyCode.A) && !doNotMove)
            direction += Vector3.left;
        if(Input.GetKey(KeyCode.D) && !doNotMove)
            direction += Vector3.right;

        if(playerObject.isSelfControlled) {
            lastestDirection = direction;
        } else {
            direction = noneSelfControlledDirection;
        }
        
        if(direction.z == -1 && direction.x == 0)
            visualDirection = Direction.Down;
        if(direction.z == 1 && direction.x == 0)
            visualDirection = Direction.Up;
        if(direction.z == 0 && direction.x == -1)
            visualDirection = Direction.Left;
        if(direction.z == 0 && direction.x == 1)
            visualDirection = Direction.Right;

        if(direction.sqrMagnitude > 0.25f) {
            isMoving = true;
        } else {
            isMoving = false;
        }

        if(direction.sqrMagnitude > 1f)
            direction.Normalize();
        direction.z *= 1.5f;

        vel = AccelerateVelocity(vel, direction, walkSpeed, walkAcceleration);
        vel.x *= (1f - Time.deltaTime * groundFriction);
        vel.z*= (1f - Time.deltaTime * groundFriction);
        
        isGrounded = IsGrounded() || rigidbody.isGrounded;
        if(!isGrounded) {
            vel.y += -gravity * Time.deltaTime;
        } else {
            vel.y = 0f;
        }
        if(isGrounded && Input.GetKey(KeyCode.Space) && !doNotExecuteActions) {
            vel += Vector3.up * jumpForce;
        }

        if(transform.position.y < 3f) {
            vel += Vector3.up * 50f * Time.deltaTime;
        }

        velocity = vel;

        rigidbody.Move(velocity * Time.deltaTime);
    }

    public void SetState (Vector3 position, Vector3 velocity, Vector3 direction) {
        interpTransfom.SetOffset(transform.position - position);

        rigidbody.enabled = false;
        transform.position = position;
        rigidbody.enabled = true;
        this.velocity = velocity;
        noneSelfControlledDirection = direction;
    }

    private static Vector3 AccelerateVelocity (Vector3 velocity, Vector3 direction, float maxSpeed, float acceleration) {
        Vector3 target = direction * maxSpeed;
        Vector3 impulse = direction * acceleration * Time.deltaTime;
        Vector3 v = velocity;

        if(target.x > 0f && v.x < target.x) {
            v.x = Mathf.Min(target.x, v.x + impulse.x);
        } else if(target.x < 0f && v.x > target.x) {
            v.x = Mathf.Max(target.x, v.x + impulse.x);
        }

        if(target.y > 0f && v.y < target.y) {
            v.y = Mathf.Min(target.y, v.y + impulse.y);
        } else if(target.y < 0f && v.y > target.y) {
            v.y = Mathf.Max(target.y, v.y + impulse.y);
        }

        if(target.z > 0f && v.z < target.z) {
            v.z = Mathf.Min(target.z, v.z + impulse.z);
        } else if(target.z < 0f && v.z > target.z) {
            v.z = Mathf.Max(target.z, v.z + impulse.z);
        }

        return v;
    }

    bool IsGrounded () {
        return Physics.CheckBox(transform.position, new Vector3(0.2f, 2/16f, 0.2f), Quaternion.identity, 1 << 9);
    }
}
