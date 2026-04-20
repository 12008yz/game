using UnityEngine;

public class GameHUD3D : MonoBehaviour
{
    GUIStyle _style;

    void Awake()
    {
        _style = new GUIStyle
        {
            fontSize = 18,
            normal = { textColor = Color.white }
        };
    }

    void OnGUI()
    {
        var gm = GameManager3D.Instance;
        if (gm == null) return;

        GUI.Label(new Rect(12, 10, 500, 30), $"Kills: {gm.Kills}", _style);
        GUI.Label(new Rect(12, 34, 900, 30), "WASD - move   Mouse - aim   LMB/Space - fire   R - restart", _style);

        if (!gm.GameOver) return;
        string text = gm.Win ? "YOU WIN" : "GAME OVER";
        GUI.Label(new Rect(12, 62, 500, 30), $"{text} (R to restart)", _style);
    }
}
