using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public enum ForkliftSounds {
    Engine,
    Off,
    Lift,
    Reverse,
    HornA,
    HornB,
    HornC,
    HornD,
    PositionA,
    PositionB,
    PositionC
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    private Vector2 force_dir;

    [SerializeField]
    [InspectorName("Wheel base offset")]
    [Tooltip("Offset from the center of the mesh to where the front axle is")]
    private float wheel_base = 1.6f;

    [SerializeField]
    [InspectorName("Max steering angle (deg)")]
    [Tooltip("Maximum amount of 'turn' that the axel will move to the far left and right")]
    private float max_steer_angle_degeres = 35f;

    [SerializeField]
    [InspectorName("Steer response coefficient")]
    [Tooltip("How long it takes the axel to reach its target rotation")]
    private float steer_response = 6f;

    [SerializeField] [InspectorName("Max forward speed")]
    private float max_fwd_speed = 6f;

    [SerializeField] [InspectorName("Max reverse speed")]
    private float max_rev_speed = 3f;

    [SerializeField] [InspectorName("Acceleration")]
    private float accel = 8f;

    [SerializeField] [InspectorName("Braking acceleration")] [Tooltip("a.k.a. Deceleration")]
    private float brake_accel = 14f;

    [SerializeField] [InspectorName("Drag")] [Tooltip("Drag coefficient of the tires to the driving surface")]
    private float drag = 0.2f;

    [SerializeField]
    [InspectorName("Lateral grip")]
    [Tooltip("Total amount of slippage to the left or right that the vehicle can endure")]
    private float lateral_grip = 10f;

    [SerializeField]
    [InspectorName("Speed alignment")]
    [Tooltip("Time it takes the vehicle to blend toward its target forward vector.")]
    private float speed_align = 8f;

    private float steer_angle_rad;
    private float speed;
    private float heading_rad;
    private bool grabbing;
    private GameObject grabbed_object;

    [SerializeField] private Rigidbody rigid_body;
    private InputAction move;
    private InputAction zoom;
    private InputAction grab;

    [SerializeField] private GameController game_controller;
    private Vector3 forklift_model_extents;
    private Vector3 ball_extents;

    [SerializeField] private Transform holding_area;

    [SerializeField] private float pickup_range = 0.1f;

    [SerializeField] private float pickup_force = 150f;

    [SerializeField] private bool has_picked_up_first_ball;

    [SerializeField] private List<AudioSource> audios;

    private Dictionary<string, AudioSource> audio_dict;

    private void OnValidate() {
        if (game_controller == null) game_controller = FindFirstObjectByType<GameController>();
        if (game_controller == null) {
            Debug.Log("No game_controller", this);
            return;
        }

        ball_extents = game_controller.ball_extents * 2f;
    }

    private void Awake() {
        if (rigid_body == null && !TryGetComponent(out rigid_body)) {
            Debug.Log("No rigid_body", this);
            return;
        }

        if (rigid_body != null) {
            rigid_body.constraints |=
                RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        } else {
            Debug.Log("No rigid_body after TryGetComponent", this);
            return;
        }

        forklift_model_extents = GetComponentInChildren<MeshCollider>().bounds.extents;

        if (game_controller == null) game_controller = FindFirstObjectByType<GameController>();
        if (game_controller == null) {
            Debug.Log("No game_controller", this);
            return;
        }

        ball_extents = game_controller.ball_extents * 2f;

        if (holding_area == null) holding_area = GameObject.FindGameObjectWithTag("HoldingArea").transform;

        if (InputSystem.actions != null) {
            move = InputSystem.actions.FindAction("Move");
            zoom = InputSystem.actions.FindAction("Zoom");
            grab = InputSystem.actions.FindAction("Grab");
        } else {
            Debug.Log("No InputSystem actions found", this);
            return;
        }

        if (move == null) {
            Debug.Log("No Move action on global IA", this);
            return;
        }

        if (zoom == null) {
            Debug.Log("No Zoom action on global IA", this);
            return;
        }

        if (grab == null) {
            Debug.Log("No Grab action on global IA", this);
            return;
        }

        if (!validate_audios()) return;

        if (zoom != null) zoom.performed += OnZoomPerformed;
        else {
            Debug.Log("No zoom action found", this);
            return;
        }

        if (move != null) {
            move.performed += OnMovePerformed;
            move.canceled += OnMoveCanceled;
        } else {
            Debug.Log("No move action found", this);
            return;
        }

        if (grab != null)
            grab.performed += OnGrabPerformed;
        else
            Debug.Log("No grab action found", this);
    }

    private bool validate_audios() {
        if (audios is { Count: < 1 }) {
            Debug.Log("No audios loaded", this);
            return false;
        }

        if (audios is not { Count: 11 }) {
            Debug.Log("Not all audios added", this);
            return false;
        }

        if (audios.Any(static audio_source => audio_source == null)) {
            Debug.Log("Audios contains null elem");
            return false;
        }

        Debug.Assert(
            audios[index: 0]
                .resource.name.Contains("engine", System.StringComparison.CurrentCultureIgnoreCase)
        );

        audio_dict = new Dictionary<string, AudioSource>(capacity: 11);

        try {
            audio_dict.Add(nameof(ForkliftSounds.Engine), audios[index: 0]);
            Debug.Assert(
                audios[index: 1].name.Contains("off", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.Off), audios[index: 1]);
            Debug.Assert(
                audios[index: 2].name.Contains("lift", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.Lift), audios[index: 2]);
            Debug.Assert(
                audios[index: 3].name.Contains("reverse", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.Reverse), audios[index: 3]);
            Debug.Assert(
                audios[index: 4].name.Contains("horna", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.HornA), audios[index: 4]);
            Debug.Assert(
                audios[index: 5].name.Contains("hornb", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.HornB), audios[index: 5]);
            Debug.Assert(
                audios[index: 6].name.Contains("hornc", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.HornC), audios[index: 6]);
            Debug.Assert(
                audios[index: 7].name.Contains("hornd", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.HornD), audios[index: 7]);
            Debug.Assert(
                audios[index: 8].name.Contains("positioninga", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.PositionA), audios[index: 8]);
            Debug.Assert(
                audios[index: 9].name.Contains("positioningb", System.StringComparison.CurrentCultureIgnoreCase)
            );
            audio_dict.Add(nameof(ForkliftSounds.PositionB), audios[index: 9]);
            Debug.Assert(
                audios[index: 10].name.Contains("positioningc", System.StringComparison.CurrentCultureIgnoreCase));
            audio_dict.Add(nameof(ForkliftSounds.PositionC), audios[index: 10]);
        } catch {
            Debug.Log("Error adding audio sources to dictionary", this);
            return false;
        }

        return true;
    }

    private void OnEnable() {
        zoom?.Enable();
        move?.Enable();
        grab?.Enable();
    }

    private void OnDisable() {
        zoom?.Disable();
        move?.Disable();
        grab?.Disable();
    }

    private void OnZoomPerformed(CallbackContext ctx) { }

    private void OnMovePerformed(CallbackContext ctx) => force_dir = ctx.ReadValue<Vector2>();

    private void OnMoveCanceled(CallbackContext ctx) => force_dir = Vector2.zero;

    private void OnGrabPerformed(CallbackContext context) => grabbing = !grabbing;

    private void use_grab() {
        switch (grabbed_object, grabbing) {
            case (null, true): {
                var tf = transform;
                var tf_fwd = tf.forward;

                var position = tf.position;
                Physics.Raycast(
                    position,
                    tf_fwd,
                    out var hit,
                    pickup_range,
                    LayerMask.GetMask("Balls")
                );
                Debug.DrawRay(position, tf_fwd, Color.red, duration: 1f);
                var hit_body = hit.rigidbody;
                if (hit_body != null) {
                    grabbed_object = hit_body.gameObject;

                    var grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                    if (grabbed_rb == null) {
                        grabbed_object = null;
                        Debug.LogError("Grabbed object not found", this);
                        return;
                    }

                    grabbed_rb.useGravity = false;
                    grabbed_rb.linearDamping = 10f;
                    grabbed_rb.angularDamping = 10f;
                    grabbed_rb.constraints = RigidbodyConstraints.FreezeRotation;
                    grabbed_rb.transform.parent = holding_area;
                    if (!has_picked_up_first_ball) {
                        has_picked_up_first_ball = true;
                        if (game_controller == null) {
                            Debug.LogError("No game_controller in PlayerController", this);
                            return;
                        }

                        game_controller.player_picked_up_first_ball();
                    }
                } else
                    grabbing = false;

                break;
            }
            case (not null, false): {
                var grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                if (grabbed_rb == null) {
                    Debug.LogError("Grabbed object had no rigidbody", this);
                    return;
                }

                grabbed_rb.useGravity = true;
                grabbed_rb.linearDamping = 1f;
                grabbed_rb.angularDamping = 1f;
                grabbed_rb.constraints = RigidbodyConstraints.None;
                grabbed_rb.transform.parent = null;

                grabbed_object = null;
                break;
            }
            case (not null, true): {
                var grabbed_rb = grabbed_object.GetComponent<Rigidbody>();
                if (
                    holding_area != null && Vector3.Distance(grabbed_object.transform.position, holding_area.position)
                    > 0.1f
                ) {
                    if (grabbed_rb == null) {
                        Debug.LogError("Grabbed object had no rigidbody", this);
                        return;
                    }

                    grabbed_rb.constraints = RigidbodyConstraints.FreezeRotation;
                    var move_dir = holding_area.position - grabbed_object.transform.position;
                    grabbed_rb.AddForce(move_dir * pickup_force);
                } else {
                    if (grabbed_rb == null) {
                        Debug.LogError("Grabbed object had no rigidbody", this);
                        return;
                    }

                    grabbed_rb.constraints = RigidbodyConstraints.FreezeAll;
                }


                break;
            }
        }
    }

    private void FixedUpdate() {
        move_player(Time.deltaTime);
        use_grab();
    }

    private void move_player(float dt_fixed) {
        var steer_input = Mathf.Clamp(force_dir.x, min: -1f, max: 1f);
        var throttle = Mathf.Clamp(force_dir.y, min: -1f, max: 1f);

        var target_steer = steer_input * max_steer_angle_degeres * Mathf.Deg2Rad;
        steer_angle_rad = Mathf.MoveTowards(
            steer_angle_rad,
            target_steer,
            steer_response * dt_fixed
        );

        var target_max = throttle >= 0f ? max_fwd_speed : max_rev_speed;
        var target_speed = target_max * throttle;
        var a = Mathf.Approximately(Mathf.Sign(target_speed), Mathf.Sign(speed)) ? accel : brake_accel;
        speed = Mathf.MoveTowards(speed, target_speed, a * dt_fixed);
        speed -= speed * drag * dt_fixed;
        var yaw_rate =
            wheel_base > 0.0001f ? speed / wheel_base * Mathf.Tan(steer_angle_rad) : 0f;

        heading_rad += yaw_rate * dt_fixed;
        if (rigid_body == null) {
            Debug.Log("No rigid_body in move_player", this);
            return;
        }

        rigid_body.MoveRotation(Quaternion.AngleAxis(heading_rad * Mathf.Rad2Deg, Vector3.up));

        var forward = rigid_body.rotation * Vector3.forward;
        var delta = forward * (speed * dt_fixed);

        var local_v = transform.InverseTransformDirection(rigid_body.linearVelocity);
        local_v.x = 0f;
        rigid_body.linearVelocity = transform.TransformDirection(local_v);

        rigid_body.MovePosition(rigid_body.position + delta);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        var tf = transform;
        var p = tf.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p, p + tf.forward * wheel_base);
        var forward = transform.forward;
        var nose = p + forward * wheel_base;
        var steer_dir =
            Quaternion.AngleAxis(steer_angle_rad * Mathf.Rad2Deg, Vector3.up) * forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(nose, nose + steer_dir * 0.8f);

        Gizmos.color = Color.magenta;
        if (holding_area != null) Gizmos.DrawWireCube(holding_area.position, ball_extents);
    }
#endif
}