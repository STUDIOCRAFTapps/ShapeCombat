using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class EnnemyNavigator : MonoBehaviour {
    public Transform target;
    public float height = 0.25f;
    public float maxFollowDistance = 25f;
    public float maxAlternateTargetDistance = 10f;
    public float maxTimeSwitchTarget = 2f;
    public float maxMoveStepPerSecond = 2f;
    public float groundFriction = 2f;
    public float jumpForce = 8f;

    public bool isFreezed = false;

    public EnemyNavigatorState enemyNavigatorState { get; private set; }
    public bool isGoingRight { get; private set; }

    private NavigatorLogicState navigatorState = NavigatorLogicState.WaitingToSendRequest;
    private float3 pointToFollow;
    private int3 previousTargetPos;
    private int3 currentTargetPos;

    private List<Vector3> lastestResult;

    private Rigidbody rb;
    private Enemies enemy;

    public Vector3 velocity {
        get {
            return rb.velocity;
        }
        set {
            rb.velocity = value;
        }
    }

    private void Awake () {
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemies>();
    }

    private void FixedUpdate () {


        // Immediate turn offs
        if(isFreezed) {
            enemyNavigatorState = EnemyNavigatorState.Idle;
            RunSimulationWithFriction();
            return;
        }
        if(!HasGroundNearby()) {
            enemyNavigatorState = EnemyNavigatorState.Idle;
            RunSimulationWithFriction();
            return;
        }
        if(target == null) {
            enemyNavigatorState = EnemyNavigatorState.Idle;
            TryChangeTarget();
            RunSimulationWithFriction();
            return;
        }
        float distSq = math.lengthsq(target.position - transform.position);
        if(distSq > maxAlternateTargetDistance * maxAlternateTargetDistance) {
            TryChangeTarget();
        }
        if(distSq > maxFollowDistance * maxFollowDistance) {
            if(NetworkAssistant.IsServer) {
                target = null;
                WorldSync.SetEnemyTarget(enemy.id, -1);
            }
            RunSimulationWithFriction();
            return;
        }


        // Path logic
        if(navigatorState == NavigatorLogicState.WaitingToSendRequest) {
            navigatorState = NavigatorLogicState.WaitingForPathResults;
            SendRequest();
        } else {
            if(math.any(previousTargetPos != (int3)math.floor(target.position))) {
                previousTargetPos = (int3)math.floor(target.position);
                SendRequest();
            }

            if(math.any(currentTargetPos != (int3)math.floor(transform.position))) {
                currentTargetPos = (int3)math.floor(transform.position);
                SendRequest();
            }
        }


        // Path navigation
        Vector3 vel = rb.velocity;
        bool isGrounded = IsGrounded();

        if(navigatorState == NavigatorLogicState.FollowingPoint && distSq > 1.5f * 1.5f) {
            Vector3 nextPointDelta = (Vector3.MoveTowards(transform.position, pointToFollow + math.up() * height, maxMoveStepPerSecond * Time.deltaTime) - transform.position);
            nextPointDelta.z *= 2f;
            vel += nextPointDelta;
            if(isGrounded && (pointToFollow.y - transform.position.y) > 0.25f) {
                vel.y = jumpForce;
                enemy.OnNavigatorJump();
            }
            isGoingRight = (pointToFollow.x - transform.position.x) > 0f;
            float sqrVel = vel.sqrMagnitude;

            enemyNavigatorState = (sqrVel > 2.25f) ? EnemyNavigatorState.Walk : EnemyNavigatorState.Idle;
        } else {
            if(distSq <= 1.5f * 1.5f) {
                isGoingRight = (target.transform.position.x - transform.position.x) > 0f;
                enemy.OnNavigatorNearTarget();
            }
            enemyNavigatorState = EnemyNavigatorState.Idle;
        }
        

        vel.x *= (1f - Time.deltaTime * groundFriction);
        vel.z *= (1f - Time.deltaTime * groundFriction);
        rb.velocity = vel;
    }

    private void Update () {
        if(lastestResult != null) {
            for(int i = 0; i < lastestResult.Count - 1; i++) {
                Debug.DrawLine(lastestResult[i] + Vector3.one * 0.5f, lastestResult[i + 1] + Vector3.one * 0.5f, Color.magenta);
            }
        }
    }

    public int SetFirstTarget () {
        if(!NetworkAssistant.IsServer)
            return -1;

        PlayerGameObject closestPlayer = EnemyManager.GetClosestPlayer(transform.position, out float distance);
        target = closestPlayer.transform;
        return (int)closestPlayer.clientId;
    }

    float timeOfLastTargetChange;
    void TryChangeTarget () {
        if(!NetworkAssistant.IsServer)
            return;

        float timeSinceLastTargetSwitch = Time.fixedTime - timeOfLastTargetChange;
        if(timeSinceLastTargetSwitch > maxTimeSwitchTarget) {
            timeOfLastTargetChange = Time.fixedTime;

            PlayerGameObject closestPlayer = EnemyManager.GetClosestPlayer(transform.position, out float distance);
            target = closestPlayer.transform;
            WorldSync.SetEnemyTarget(enemy.id, (int)closestPlayer.clientId);
        }
    }

    void SendRequest () {
        PathfindingManager.inst.RequestPath((int3)math.floor(transform.position), (int3)math.floor(target.position), (valid, path, ncount) => {
            if(valid && ncount > 1) {
                previousTargetPos = (int3)math.floor(target.position);
                navigatorState = NavigatorLogicState.FollowingPoint;
                pointToFollow = path[ncount - 1] + new float3(0.5f, 0f, 0.5f);

                lastestResult = new List<Vector3>(ncount);
                for(int i = 0; i < ncount; i++) {
                    lastestResult.Add(path[i]);
                }
            } else {
                //navigatorState = NavigatorState.WaitingForTargetPosChange;
            }
        });
    }

    bool IsGrounded () {
        return Physics.CheckBox(transform.position, new Vector3(0.25f, 2 / 16f, 0.25f), Quaternion.identity, 1 << 9);
    }

    bool HasGroundNearby () {
        return Physics.CheckBox(transform.position - Vector3.up * 1f, new Vector3(0.25f, 2f, 0.25f), Quaternion.identity, 1 << 9);
    }

    void RunSimulationWithFriction () {
        Vector3 vel = rb.velocity;
        vel.x *= (1f - Time.deltaTime * groundFriction);
        vel.z *= (1f - Time.deltaTime * groundFriction);
        vel += Vector3.down * 25f * Time.deltaTime; // Extra Gravity
        rb.velocity = vel;
    }

    public void ApplyImpulse (Vector3 impulse) {
        rb.velocity += impulse * (1f/rb.mass);
    }
}

public enum NavigatorLogicState {
    WaitingToSendRequest,
    WaitingForPathResults,
    WaitingForTargetPosChange,
    FollowingPoint
}

public enum EnemyNavigatorState {
    Idle,
    Walk
}
