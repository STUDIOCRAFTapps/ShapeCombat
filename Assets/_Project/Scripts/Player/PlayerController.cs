using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    [Header("Parameters")]
    public float walkSpeed;
    public float walkAcceleration;
    public float groundFriction;
    public float gravity;
    public float jumpForce;

    new public Rigidbody rigidbody;

    void Start () {
        
    }
    
    void FixedUpdate () {
        Vector3 vel = rigidbody.velocity;

        Vector3 direction = Vector3.zero;
        if(Input.GetKey(KeyCode.S))
            direction += Vector3.back;
        if(Input.GetKey(KeyCode.W))
            direction += Vector3.forward;
        if(Input.GetKey(KeyCode.A))
            direction += Vector3.left;
        if(Input.GetKey(KeyCode.D))
            direction += Vector3.right;
        if(direction.sqrMagnitude > 1f)
            direction.Normalize();
        direction.z *= 1.5f;

        vel = AccelerateVelocity(vel, direction, walkSpeed, walkAcceleration);
        vel.x *= (1f - Time.deltaTime * groundFriction);
        vel.z*= (1f - Time.deltaTime * groundFriction);
        vel.y += -gravity * Time.deltaTime;

        bool isGrounded = IsGrounded();
        if(isGrounded && Input.GetKey(KeyCode.Space))
            vel += Vector3.up * jumpForce;

        rigidbody.velocity = vel;
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
        return Physics.CheckBox(transform.position, new Vector3(0.39f, 2/16f, 0.39f), Quaternion.identity, 1 << 9);
    }
}
