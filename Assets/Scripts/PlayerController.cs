using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Vector2 force_dir;

    [
        SerializeField,
        InspectorName("Wheel base offset"),
        Tooltip("Offset from the center of the mesh to where the front axle is")
    ]
    private float wheel_base = 1.6f;

    [
        SerializeField,
        InspectorName("Max steering angle (deg)"),
        Tooltip("Maximum amount of 'turn' that the axel will move to the far left and right")
    ]
    private float max_steer_angle_degeres = 35f;

    [
        SerializeField,
        InspectorName("Steer response coefficient"),
        Tooltip("How long it takes the axel to reach its target rotation")
    ]
    private float steer_response = 6f;

    [SerializeField, InspectorName("Max forward speed")]
    private float max_fwd_speed = 6f;

    [SerializeField, InspectorName("Max reverse speed")]
    private float max_rev_speed = 3f;

    [SerializeField, InspectorName("Acceleration")]
    private float accel = 8f;

    [SerializeField, InspectorName("Brakeing acceleration"), Tooltip("a.k.a. Deceleration")]
    private float brake_accel = 14f;

    [
        SerializeField,
        InspectorName("Drag"),
        Tooltip("Drag coefficient of the tires to the driving surface")
    ]
    private float drag = 0.2f;

    [
        SerializeField,
        InspectorName("Lateral grip"),
        Tooltip("Total ammount of slippage to the left or right that the vehicle can endure")
    ]
    private float lateral_grip = 10f;

    [
        SerializeField,
        InspectorName("Speed alginment"),
        Tooltip("Time it takes the vehicle to blend toward its target forward vector.")
    ]
    private float speed_align = 8f;

    private float steer_angle_rad;
    private float speed;
    private float heading_rad;
    private bool grabbing;
    private GameObject grabbed_object;

    [SerializeField]
    private Rigidbody rigid_body;
    private InputAction move;
    private InputAction zoom;
    private InputAction grab;

    [SerializeField]
    private GameController game_controller;
    private Vector3 forklift_model_extents;
    private Vector3 ball_extents;

    [SerializeField]
    Transform holding_area;

    [SerializeField]
    private float pickup_range = 0.1f;

    [SerializeField]
    private float pickup_force = 150f;

    private void OnValidate()
    {
        if (game_controller == null)
        {
            game_controller = FindFirstObjectByType<GameController>();
        }

        ball_extents = game_controller.ball_extents * 2f;
    }

    private void Awake()
    {
        if (rigid_body == null && !TryGetComponent<Rigidbody>(out rigid_body))
        {
            Debug.Log("No rigid_body", this);
            return;
        }

        rigid_body.constraints |=
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        forklift_model_extents = GetComponentInChildren<MeshCollider>().bounds.extents;

        if (game_controller == null)
        {
            game_controller = FindFirstObjectByType<GameController>();
        }
        ball_extents = game_controller.ball_extents * 2f;

        if (holding_area == null)
        {
            holding_area = GameObject.FindGameObjectWithTag("HoldingArea").transform;
        }

        move = InputSystem.actions.FindAction("Move");
        zoom = InputSystem.actions.FindAction("Zoom");
        grab = InputSystem.actions.FindAction("Grab");
        if (move == null)
        {
            Debug.Log("No Move action on global IA", this);
            return;
        }

        if (zoom == null)
        {
            Debug.Log("No Zoom action on global IA", this);
            return;
        }

        if (grab == null)
        {
            Debug.Log("No Grab action on global IA", this);
            return;
        }

        zoom.performed += OnZoomPerformed;
        move.performed += OnMovePerformed;
        move.canceled += OnMoveCanceled;
        grab.performed += OnGrabPerformed;
    }

    private void OnEnable()
    {
        zoom.Enable();
        move.Enable();
        grab.Enable();
    }

    private void OnDisable()
    {
        zoom.Disable();
        move.Disable();
        grab.Disable();
    }

    private void OnZoomPerformed(CallbackContext ctx) { }

    private void OnMovePerformed(CallbackContext ctx) => force_dir = ctx.ReadValue<Vector2>();

    private void OnMoveCanceled(CallbackContext ctx) => force_dir = Vector2.zero;

    private void OnGrabPerformed(CallbackContext context) => grabbing = !grabbing;

    private void use_grab()
    {
        switch (grabbed_object, grabbing)
        {
            case (null, true):
            {
                RaycastHit hit;
                var tf = transform;
                Vector3 tf_fwd = tf.forward;

                Physics.Raycast(
                    tf.position,
                    tf_fwd,
                    out hit,
                    pickup_range,
                    LayerMask.GetMask("Balls")
                );
                Debug.DrawRay(tf.position, tf_fwd, Color.red, 1f);
                var hit_body = hit.rigidbody;
                if (hit_body != null)
                {
                    grabbed_object = hit_body.gameObject;

                    Rigidbody grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                    grabbed_rb.useGravity = false;
                    grabbed_rb.linearDamping = 10f;
                    grabbed_rb.angularDamping = 10f;
                    grabbed_rb.constraints = RigidbodyConstraints.FreezeRotation;
                    grabbed_rb.transform.parent = holding_area;

                    // grabbed_object.GetComponent<MeshCollider>().enabled = false;
                }
                else
                {
                    grabbing = false;
                }
                break;
            }
            case (not null, false):
            {
                Rigidbody grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                grabbed_rb.useGravity = true;
                grabbed_rb.linearDamping = 1f;
                grabbed_rb.angularDamping = 1f;
                grabbed_rb.constraints = RigidbodyConstraints.None;
                grabbed_rb.transform.parent = null;

                grabbed_object = null;
                break;
            }
            case (not null, true):
            {
                Rigidbody grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                if (
                    Vector3.Distance(grabbed_object.transform.position, holding_area.position)
                    > 0.1f
                )
                {
                    grabbed_rb.constraints = RigidbodyConstraints.FreezeRotation;
                    Vector3 move_dir = (holding_area.position - grabbed_object.transform.position);
                    grabbed_rb.AddForce(move_dir * pickup_force);
                }
                else
                {
                    grabbed_rb.constraints = RigidbodyConstraints.FreezeAll;
                }

                break;
            }
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        move_player(Time.deltaTime);
        use_grab();
    }

    private void move_player(float dt_fixed)
    {
        float steer_input = Mathf.Clamp(force_dir.x, -1f, 1f);
        float throttle = Mathf.Clamp(force_dir.y, -1f, 1f);

        float target_steer = steer_input * max_steer_angle_degeres * Mathf.Deg2Rad;
        steer_angle_rad = Mathf.MoveTowards(
            steer_angle_rad,
            target_steer,
            steer_response * dt_fixed
        );

        float target_max = throttle >= 0f ? max_fwd_speed : max_rev_speed;
        float target_speed = target_max * throttle;
        float a = Mathf.Sign(target_speed) == Mathf.Sign(speed) ? accel : brake_accel;
        speed = Mathf.MoveTowards(speed, target_speed, a * dt_fixed);
        speed -= speed * drag * dt_fixed;
        float yaw_rate =
            (wheel_base > 0.0001f) ? (speed / wheel_base) * Mathf.Tan(steer_angle_rad) : 0f;

        heading_rad += yaw_rate * dt_fixed;
        rigid_body.MoveRotation(Quaternion.AngleAxis(heading_rad * Mathf.Rad2Deg, Vector3.up));

        Vector3 forward = rigid_body.rotation * Vector3.forward;
        Vector3 delta = forward * (speed * dt_fixed);

        Vector3 local_v = transform.InverseTransformDirection(rigid_body.linearVelocity);
        local_v.x = 0f;
        rigid_body.linearVelocity = transform.TransformDirection(local_v);

        rigid_body.MovePosition(rigid_body.position + delta);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var p = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p, p + transform.forward * wheel_base);
        var nose = p + transform.forward * wheel_base;
        var steer_dir =
            Quaternion.AngleAxis(steer_angle_rad * Mathf.Rad2Deg, Vector3.up) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(nose, nose + steer_dir * 0.8f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(holding_area.position, ball_extents);
    }
#endif
}
