using UnityEngine;

public class Add : MonoBehaviour
{
    public Font customFont;
    private int leftNumber = 1;
    private int rightNumber = 0;
    private int result;

    // Initialize numbers and calculate initial result
    void Start()
    {
        CalculateResult();
    }

    // Handle input and render UI
    void Update()
    {
        HandleInput();
    }

    // Render the three numbers on screen at 50% screen height
    void OnGUI()
    {
        if (customFont != null)
        {
            GUI.skin.label.font = customFont;
            GUI.skin.label.fontSize = Mathf.RoundToInt(Screen.height * 0.4f);
        }

        // Library name header at top
        GUI.Label(new Rect(10, 10, Screen.width - 20, 30), Core.libName);

        // Calculate size as 50% of screen height
        float numberSize = Screen.height * 0.5f;

        // Create styles
        GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle plusStyle = new GUIStyle(centeredStyle);
        plusStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.2f);

        GUIStyle clickableStyle = new GUIStyle(GUI.skin.box);
        clickableStyle.normal.background = null;

        // Draw large numbers
        GUI.Label(
            new Rect(
                Screen.width / 4 - numberSize / 2,
                Screen.height / 2 - numberSize / 2,
                numberSize,
                numberSize
            ),
            leftNumber.ToString(),
            centeredStyle
        );
        GUI.Label(
            new Rect(
                3 * Screen.width / 4 - numberSize / 2,
                Screen.height / 2 - numberSize / 2,
                numberSize,
                numberSize
            ),
            rightNumber.ToString(),
            centeredStyle
        );

        // Draw plus sign between numbers
        GUI.Label(
            new Rect(
                Screen.width / 2 - numberSize / 4,
                Screen.height / 2 - numberSize / 4,
                numberSize / 2,
                numberSize / 2
            ),
            "+",
            plusStyle
        );

        // Draw result
        GUI.Label(
            new Rect(
                Screen.width / 2 - numberSize / 2,
                Screen.height / 2 + numberSize / 4,
                numberSize,
                numberSize
            ),
            result.ToString(),
            centeredStyle
        );
    }

    // Check for screen clicks to increment numbers
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;

            if (mousePos.x < Screen.width / 2)
            {
                leftNumber = (leftNumber + 1) % 11;
                if (leftNumber == 11)
                    leftNumber = 0;
            }
            else
            {
                rightNumber = (rightNumber + 1) % 11;
                if (rightNumber == 11)
                    rightNumber = 0;
            }

            CalculateResult();
        }
    }

    // Calculate result using Core.add function
    private void CalculateResult()
    {
        result = Core.add(leftNumber, rightNumber);
    }
}
