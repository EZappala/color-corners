using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect), typeof(BoxCollider))]
public class Zone : MonoBehaviour
{
    public bool passed;
    private Color color_Internal;

    [SerializeField]
    private BoxCollider box_collider;

    [SerializeField]
    private VisualEffect visual_effect;

    [SerializeField]
    private GameController game_controller;

    public Color Color
    {
        get => color_Internal;
        set
        {
            // Set the shader to this color.
            color_Internal = value;
            if (visual_effect == null)
            {
                return;
            }

            visual_effect.SetVector4(Shader.PropertyToID("BaseColor"), Color);
        }
    }

    private void Awake()
    {
        if (box_collider == null && !TryGetComponent<BoxCollider>(out box_collider))
        {
            Debug.LogError("Zone has no box collider", this);
            return;
        }

        if (visual_effect == null && !TryGetComponent<VisualEffect>(out visual_effect))
        {
            Debug.LogError("Zone has no visual effect", this);
            return;
        }

        if (game_controller == null)
        {
            game_controller = GameObject.FindFirstObjectByType<GameController>();
        }
    }

    private void Start() { }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Ball") || passed)
        {
            return;
        }

        Ball ball = other.GetComponent<Ball>();
        if (ball.Color != Color)
        {
            Debug.Log($"Ball color not matching zone color. {ball.Color} != {Color}");
            return;
        }

        passed = true;
        game_controller.update_score();
    }
}
