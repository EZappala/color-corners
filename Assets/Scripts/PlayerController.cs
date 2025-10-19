using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigid_body;
    private InputAction move;
    private InputAction zoom;

    private Vector2 force_dir;

    [SerializeField]
    float move_speed;

    [SerializeField]
    float max_speed;

    [SerializeField]
    float deceleration_speed;

    private void Awake()
    {
        if (rigid_body == null && TryGetComponent<Rigidbody>(out rigid_body))
        {
            Debug.Log("No rigid_body", this);
            return;
        }

        move = InputSystem.actions.FindAction("Move");
        zoom = InputSystem.actions.FindAction("Zoom");
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

        zoom.performed += OnZoomPerformed;
        move.performed += OnMovePerformed;
        move.canceled += OnMoveCanceled;
    }

    private void OnEnable()
    {
        zoom.Enable();
        move.Enable();
    }

    private void OnDisable()
    {
        zoom.Disable();
        move.Disable();
    }

    private void OnZoomPerformed(CallbackContext ctx) { }

    private void OnMovePerformed(CallbackContext ctx)
    {
        force_dir = ctx.ReadValue<Vector2>();
        // var dir = ctx.ReadValue<Vector2>();
        // rigid_body?.AddForce(dir, ForceMode.Force);
    }

    private void OnMoveCanceled(CallbackContext ctx)
    {
        force_dir.x = 0;
        force_dir.y = 0;
    }

    private void FixedUpdate()
    {
        // rigid_body.linearVelocity = Core.move_player(
        //     force_dir,
        //     move_speed,
        //     deceleration_speed,
        //     rigid_body.linearVelocity,
        //     Time.fixedDeltaTime
        // );

        if (force_dir != Vector2.zero)
        {
            var v = new Vector3(force_dir.x, 0f, force_dir.y) * move_speed;
            var current_v = rigid_body.linearVelocity;
            float mod = move_speed * Time.fixedDeltaTime;
            rigid_body.linearVelocity = Vector3.Lerp(current_v, v, mod);
        }
        else
        {
            var current_v = rigid_body.linearVelocity;
            float mod = deceleration_speed * Time.fixedDeltaTime;
            rigid_body.linearVelocity = Vector3.Lerp(current_v, Vector3.zero, mod);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 400f, 0f, 400f, Screen.height));
        GUILayout.Label($"force_dir: {this.force_dir}");
        GUILayout.Label($"lin_vel: {this.rigid_body.linearVelocity}");
        GUILayout.Label($"ang_vel: {this.rigid_body.angularVelocity}");
        GUILayout.EndArea();
    }
}
