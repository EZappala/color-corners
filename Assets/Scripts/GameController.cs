using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    [SerializeField] private List<Color> colors;

    [SerializeField] private GameObject ball_prefab;

    [SerializeField] private GameObject spawn_area;

    [SerializeField] private int starting_balls = 4;

    [SerializeField] private float ball_spawn_tollerance;

    private Vector3 spawn_area_extents;
    public Vector3 ball_extents;

    [SerializeField] private List<GameObject> zones;

    [SerializeField] private List<GameObject> balls;

    [SerializeField] private InGameUiController ui;

    private int score;
    private bool goal_achieved;

    [SerializeField] [InspectorName("Completion duration")] [Tooltip("Total seconds until level ends.")]
    private int num_secs_to_complete_level = 60;

    private TimeSpan duration;
    private static TimeSpan step = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 1);
    private static readonly int BASE_COLOR_FACTOR = Shader.PropertyToID("baseColorFactor");
    private bool time_expired = false;

    private void OnValidate() {
        if (ball_prefab != null) {
            var mesh_renderer = ball_prefab.GetComponent<MeshRenderer>();
            if (mesh_renderer == null) {
                Debug.LogError("Ball prefab has no MeshRenderer component", this);
                return;
            }

            ball_extents = mesh_renderer.bounds.extents;
        }

        if (ball_extents != null) return;

        Debug.LogWarning("Ball has no extents, defaulting to Vector3.zero");
        ball_extents = Vector3.zero;
    }

    private void Awake() {
        if (spawn_area != null) {
            var mesh_renderer = spawn_area.GetComponent<MeshRenderer>();
            if (mesh_renderer == null) {
                Debug.LogError("Spawn area has no MeshRenderer component", this);
                return;
            }

            spawn_area_extents = mesh_renderer.bounds.extents;
        }

        if (spawn_area_extents == null) {
            Debug.LogError("Extents of spawn area was null");
            return;
        }

        if (ball_prefab != null) {
            var mesh_renderer = ball_prefab.GetComponent<MeshRenderer>();
            if (mesh_renderer == null) {
                Debug.LogError("Ball prefab has no MeshRenderer component", this);
                return;
            }

            ball_extents = mesh_renderer.bounds.extents;
        }

        if (ball_extents == null) {
            Debug.LogWarning("Ball has no extents, defaulting to Vector3.zero");
            ball_extents = Vector3.zero;
        }

        zones = new List<GameObject>(GameObject.FindGameObjectsWithTag("Zone") ?? Array.Empty<GameObject>());
        if (zones.Count != starting_balls) {
            Debug.LogError("Not enough zones in scene");
            return;
        }

        if (colors != null && colors.Count != starting_balls) {
            Debug.LogError("Not enough colors set.", this);
            return;
        }


        if (ui == null) ui = FindFirstObjectByType<InGameUiController>();
        if (ui == null) {
            Debug.LogError("No InGameUiController found in scene", this);
            return;
        }

        ui.update_score(score, starting_balls);

        var unix_seconds = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        Random.InitState(unix_seconds);
    }

    private bool spawn_zones() {
        if (zones == null || zones.Count != starting_balls) {
            Debug.LogError("Zones list was null", this);
            return false;
        }

        for (var i = 0; i < starting_balls; i++) {
            if (zones[i] == null) {
                Debug.LogError($"Zone {i} was null", this);
                return false;
            }

            if (!zones[i].TryGetComponent<Zone>(out var zone)) {
                Debug.LogError($"Zone {i} had no Zone component", zones[i]);
                return false;
            }

            if (zone == null) {
                Debug.LogError("Expected zone, should have been set", this);
                return false;
            }

            if (colors != null) zone.Color = colors[i];
            else {
                Debug.Log("No colors set", this);
                return false;
            }
        }

        return true;
    }

    private void Start() {
        if (!spawn_zones()) return;
        if (!spawn_balls()) return;

        duration = new TimeSpan(hours: 0, minutes: 0, num_secs_to_complete_level);
        StartCoroutine(Timer());
    }

    private void OnDestroy() {
        StopAllCoroutines();
    }

    private bool spawn_balls() {
        var positions = new NativeArray<Vector3>(starting_balls, Allocator.Domain);

        while (balls != null && balls.Count < starting_balls) {
            positions[balls.Count] = make_pos(positions);
            if (balls == null) continue;

            var ball = Instantiate(
                ball_prefab,
                positions[balls.Count],
                Quaternion.identity
            );

            if (colors == null) {
                Debug.Log("No colors set", this);
                return false;
            }

            var color = colors[balls.Count];
            if (ball == null) {
                Debug.LogError("Failed to instantiate ball prefab", this);
                return false;
            }

            var mesh_renderer = ball.GetComponent<MeshRenderer>();

            if (mesh_renderer == null) {
                Debug.LogError("Ball prefab does not have a MeshRenderer component");
                return false;
            }

            var mats = mesh_renderer.materials;
            if (mats == null) {
                Debug.LogError("No materials found on ball mesh renderer");
                return false;
            }

            if (mats.Length != 2) Debug.LogWarning("Expected 2 materials for each ball");


            foreach (var mat in mats)
                if (mat != null)
                    mat.SetColor(BASE_COLOR_FACTOR, color);
                else {
                    Debug.LogWarning("Material was null on ball mesh renderer");
                    return false;
                }

            var rand_unit_sphere = Random.insideUnitSphere;
            var rb = ball.GetComponent<Rigidbody>();
            if (rb == null) {
                Debug.LogError("Ball prefab has no Rigidbody component", this);
                return false;
            }

            rb.AddForce(Vector3.Cross(rand_unit_sphere, spawn_area_extents), ForceMode.Impulse);

            var ball_comp = ball.GetComponent<Ball>();
            if (ball_comp == null) {
                Debug.LogError("Ball prefab has no Ball component", this);
                return false;
            }

            ball_comp.Color = color;
            balls.Add(ball);
        }

        return true;
    }

    private Vector3 make_pos(NativeArray<Vector3> positions) {
        while (true) {
            var rand_unit_circle = Random.insideUnitCircle;
            var random_loc = new Vector2(
                rand_unit_circle.x * spawn_area_extents.x,
                rand_unit_circle.y * spawn_area_extents.z
            );
            var pos = new Vector3(random_loc.x, ball_extents.y * 2f, random_loc.y);
            if (pos == Vector3.zero) continue;

            var is_valid = positions.All(c =>
                c == Vector3.zero || Vector3.Distance(c, pos) > ball_spawn_tollerance
            );

            if (is_valid) return pos;
        }
    }

    private IEnumerator Timer() {
        var prev = Time.time;
        while (duration > TimeSpan.Zero && !goal_achieved) {
            yield return null;

            var now = Time.time;
            var delta = now - prev;
            prev = now;
            duration -= TimeSpan.FromSeconds(delta);
            ui.update_timer(duration);
        }

        ui.update_timer(duration);
        if (goal_achieved) yield break;

        ui.set_game_over("You lose!\n(press ESCAPE)");
        InputSystem.DisableAllEnabledActions();
        var cont = InputSystem.actions.FindAction("Continue");
        cont.Enable();
        cont.performed += OnContinuePerformed;
    }

    internal void update_score() {
        score += 1;

        if (score == starting_balls) {
            ui.update_score(score, starting_balls);
            ui.set_game_over("You Win!\n(press ESCAPE)");
            goal_achieved = true;
            InputSystem.DisableAllEnabledActions();
            var cont = InputSystem.actions.FindAction("Continue");
            cont.Enable();
            cont.performed += OnContinuePerformed;

            return;
        }

        ui.update_score(score, starting_balls);
    }

    private void OnDisable() {
        var cont = InputSystem.actions.FindAction("Continue");
        cont.performed -= OnContinuePerformed;
        cont.Disable();
    }

    private void OnContinuePerformed(InputAction.CallbackContext context) {
        SceneManager.LoadScene("MainMenu");
    }

    public void player_picked_up_first_ball() {
        ui.toggle_help_text();
    }
}