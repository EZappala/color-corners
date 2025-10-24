using UnityEngine;

public class Ball : MonoBehaviour
{
    private Color color_Internal;

    public Color Color
    {
        get => color_Internal;
        set => color_Internal = value;
    }
}
