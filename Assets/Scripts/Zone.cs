using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(BoxCollider))]
public class Zone : MonoBehaviour {
    private static readonly int BASE_COLOR = Shader.PropertyToID("emission_color");
    public bool passed;
    private Color color_Internal;

    [SerializeField] private BoxCollider box_collider;

    [SerializeField] private ParticleSystem visual_effect;

    [SerializeField] private GameController game_controller;

    public Color Color {
        get => color_Internal;
        set {
            // Set the shader to this color.
            color_Internal = value;
            if (visual_effect == null) return;

            visual_effect.GetComponent<Renderer>().material.SetColor(BASE_COLOR, Color);
        }
    }

    private void Awake() {
        if (box_collider == null && !TryGetComponent(out box_collider)) {
            Debug.LogError("Zone has no box collider", this);
            return;
        }

        if (visual_effect == null && !TryGetComponent(out visual_effect)) {
            Debug.LogError("Zone has no visual effect", this);
            return;
        }

        if (game_controller == null) game_controller = FindFirstObjectByType<GameController>();
    }

    private void Start() { }

    private void OnTriggerEnter(Collider other) {
        if (!other.gameObject.CompareTag("Ball") || passed) return;

        var ball = other.GetComponent<Ball>();
        if (ball.Color != Color) {
            Debug.Log($"Ball color not matching zone color. {ball.Color} != {Color}");
            return;
        }

        passed = true;
        game_controller.update_score();
    }
}