using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    private const string SHADER_COLOR_PROP_NAME = "baseColorFactor";

    [SerializeField]
    private List<Color> colors;

    [SerializeField]
    private GameObject ball_prefab;

    [SerializeField]
    private GameObject spawn_area;

    [SerializeField]
    private int starting_balls = 4;

    [SerializeField]
    private float ball_spawn_tollerance;

    private Vector3 spawn_area_extents;
    public Vector3 ball_extents;

    [SerializeField]
    private List<GameObject> zones;

    [SerializeField]
    private List<GameObject> balls;

    [SerializeField]
    private InGameUiController ui;

    private int score;
    private bool goal_achieved;

    [
        SerializeField,
        InspectorName("Completion durration"),
        Tooltip("Total seconds until level ends.")
    ]
    private int num_secs_to_complete_level = 60;
    private TimeSpan duration;
    private static TimeSpan step = new TimeSpan(0, 0, 0, 0, 1);
    private bool time_expired = false;

    private void OnValidate()
    {
        ball_extents = ball_prefab.GetComponent<MeshRenderer>().bounds.extents;
        if (ball_extents == null)
        {
            Debug.LogWarning("Ball has no extents, defaulting to Vector3.zero");
            ball_extents = Vector3.zero;
        }
    }

    private void Awake()
    {
        spawn_area_extents = spawn_area.GetComponent<MeshRenderer>().bounds.extents;
        if (spawn_area_extents == null)
        {
            Debug.LogError("Extents of spawn area was null");
            return;
        }

        ball_extents = ball_prefab.GetComponent<MeshRenderer>().bounds.extents;
        if (ball_extents == null)
        {
            Debug.LogWarning("Ball has no extents, defaulting to Vector3.zero");
            ball_extents = Vector3.zero;
        }

        zones = new List<GameObject>(GameObject.FindGameObjectsWithTag("Zone"));
        if (zones.Count != starting_balls)
        {
            Debug.LogError("Not enough zones in scene");
            return;
        }

        if (colors.Count != starting_balls)
        {
            Debug.LogError("Not enough colors set.", this);
            return;
        }

        for (var i = 0; i < starting_balls; i++)
        {
            Zone zone;
            if (!zones[i].TryGetComponent<Zone>(out zone))
            {
                Debug.LogError($"Zone {i} had no Zone component", zones[i]);
                return;
            }

            if (zone == null)
            {
                Debug.LogError($"Expected zone, should have been set", this);
                return;
            }

            zone.Color = colors[i];
        }

        if (ui == null)
        {
            ui = GameObject.FindFirstObjectByType<InGameUiController>();
        }
        ui.update_score(score, starting_balls);

        int unix_seconds = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        Random.InitState(unix_seconds);
    }

    private void Start()
    {
        spawn_balls();
        duration = new TimeSpan(0, 0, num_secs_to_complete_level);
        StartCoroutine(Timer());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void spawn_balls()
    {
        NativeArray<Vector3> positions = new NativeArray<Vector3>(starting_balls, Allocator.Domain);

        while (balls.Count < starting_balls)
        {
            positions[balls.Count] = make_pos(positions);
            var ball = GameObject.Instantiate(
                ball_prefab,
                positions[balls.Count],
                Quaternion.identity
            );

            Color? color = colors[balls.Count];
            MeshRenderer mesh_renderer = ball.GetComponent<MeshRenderer>();
            if (!color.HasValue)
            {
                Debug.LogError($"No color @ idx {balls.Count}");
                return;
            }

            if (mesh_renderer == null)
            {
                Debug.LogError("Ball prefab does not have a MeshRenderer component");
                return;
            }

            var mats = mesh_renderer.materials;
            if (mats.Length != 2)
            {
                Debug.LogWarning("Expected 2 materials for each ball");
            }

            foreach (var mat in mats)
            {
                mat.SetColor(SHADER_COLOR_PROP_NAME, color.Value);
            }

            Vector3 rand_unit_sphere = Random.insideUnitSphere;
            ball.GetComponent<Rigidbody>()
                .AddForce(Vector3.Cross(rand_unit_sphere, spawn_area_extents), ForceMode.Impulse);

            ball.GetComponent<Ball>().Color = color.Value;
            balls.Add(ball);
        }
    }

    private Vector3 make_pos(NativeArray<Vector3> positions)
    {
        while (true)
        {
            Vector2 rand_unit_circle = Random.insideUnitCircle;
            Vector2 random_loc = new Vector2(
                rand_unit_circle.x * spawn_area_extents.x,
                rand_unit_circle.y * spawn_area_extents.z
            );
            var pos = new Vector3(random_loc.x, ball_extents.y * 2f, random_loc.y);
            if (pos == Vector3.zero)
            {
                continue;
            }

            var is_valid = positions.All(c =>
                c == null || c == Vector3.zero || Vector3.Distance(c, pos) > ball_spawn_tollerance
            );

            if (is_valid)
            {
                return pos;
            }
        }
    }

    private IEnumerator Timer()
    {
        var prev = Time.time;
        while (duration > TimeSpan.Zero && !goal_achieved)
        {
            yield return null;
            var now = Time.time;
            var delta = now - prev;
            prev = now;
            duration -= TimeSpan.FromSeconds(delta);
            ui.update_timer(duration);
        }

        ui.update_timer(duration);
        if (!goal_achieved)
        {
            ui.set_game_over("You lose!\n(press ESCAPE)");
            InputSystem.DisableAllEnabledActions();
            var cont = InputSystem.actions.FindAction("Continue");
            cont.Enable();
            cont.performed += OnContinuePerformed;
        }
    }

    internal void update_score()
    {
        score += 1;

        if (score == starting_balls)
        {
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

    private void OnDisable()
    {
        var cont = InputSystem.actions.FindAction("Continue");
        cont.performed -= OnContinuePerformed;
        cont.Disable();
    }

    private void OnContinuePerformed(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("MainMenu");
    }
}
