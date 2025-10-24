using System;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUiController : MonoBehaviour
{
    private UIDocument doc;
    private VisualElement root;
    private TextElement score;
    private TextElement timer;
    private VisualElement final;
    private TextElement final_text;

    [SerializeField]
    private GameController game_controller;

    private void Awake()
    {
        if (game_controller == null)
        {
            game_controller = GameObject.FindFirstObjectByType<GameController>();
        }
        if (doc == null && !TryGetComponent<UIDocument>(out doc))
        {
            Debug.LogError("No UIDocument component", this);
            return;
        }

        root = doc.rootVisualElement;
        score = root.Q<TextElement>("Score");
        timer = root.Q<TextElement>("Timer");
        final = root.Q<VisualElement>("Final");
        final_text = root.Q<TextElement>("FinalText");

        final.style.visibility = Visibility.Hidden;
    }

    internal void update_score(int new_score, int max_score = 4)
    {
        score.text = $"{new_score}/{max_score}";
    }

    internal void update_timer(TimeSpan new_time)
    {
        timer.text = new_time.ToString(@"mm\:ss\:ffff");
    }

    internal void set_game_over(string text)
    {
        final.style.visibility = Visibility.Visible;
        final_text.text = text;
    }
}
