using UnityEngine;

public class GameHUDCanvas3D : MonoBehaviour
{
    GUIStyle _style;
    GUIStyle _titleStyle;
    GUIStyle _ammoStyle;
    GUIStyle _menuTitleStyle;
    GUIStyle _menuTextStyle;
    GUIStyle _buttonStyle;
    Texture2D _solidTex;

    void Awake()
    {
        _solidTex = new Texture2D(1, 1);
        _solidTex.SetPixel(0, 0, Color.white);
        _solidTex.Apply();

        _style = new GUIStyle { fontSize = 18, normal = { textColor = Color.white } };
        _titleStyle = new GUIStyle(_style) { fontSize = 22 };
        _ammoStyle = new GUIStyle(_style)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.92f, 0.2f) }
        };
        _menuTitleStyle = new GUIStyle(_style)
        {
            fontSize = 56,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        _menuTextStyle = new GUIStyle(_style)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.96f, 1f) }
        };
    }

    void OnGUI()
    {
        EnsureRuntimeGuiStyles();

        var gm = GameManager3D.Instance;
        if (gm == null) return;

        if (!gm.GameStarted)
        {
            DrawStartPanel(gm);
            return;
        }

        var player = FindFirstObjectByType<PlayerController3D>();
        int ammoRemaining = player != null ? player.AmmoRemaining : 0;
        int maxAmmo = player != null ? player.MaxAmmo : 0;

        DrawRect(new Rect(8, 6, 330, 112), new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(new Rect(12, 10, 500, 30), $"Kills: {gm.Kills}", _style);
        GUI.Label(new Rect(12, 34, 500, 30), $"Wave {gm.WaveIndex}  {gm.WaveKills}/{gm.WaveTarget}", _style);
        GUI.Label(new Rect(12, 62, 520, 50), $"AMMO {ammoRemaining}/{maxAmmo}", _ammoStyle);
        GUI.Label(new Rect(12, 98, 520, 30), $"Weapon: {(player != null ? player.ActiveWeaponName : "Blaster")}", _style);
        DrawRect(new Rect(8, Screen.height - 40, 760, 32), new Color(0f, 0f, 0f, 0.45f));
        GUI.Label(new Rect(12, Screen.height - 36, 1200, 30), "WASD move | Mouse aim | LMB/Space fire | 1..4 weapon | R restart", _style);
        GUI.Label(new Rect(12, Screen.height - 64, 1200, 28), "Benchmark: F6 start | F10 auto suite | Quality: F7 Low, F8 Medium, F9 High", _style);
        GUI.Label(new Rect(12, Screen.height - 92, 1200, 28), $"RunState: {gm.RunState} | Session currency: {gm.SessionCurrency} | Meta: {MetaProgression3D.Data.totalCurrency}", _style);

        if (gm.PortalSpawned)
        {
            DrawRect(new Rect(8, 116, 560, 30), new Color(0f, 0.16f, 0f, 0.48f));
            GUI.Label(new Rect(12, 116, 900, 30), "Green portal appeared! Reach it to win.", _style);
        }

        if (gm.RunState == RunState3D.UpgradeChoice)
            DrawUpgradePanel(gm);

        if (!gm.GameOver) return;
        string text = gm.Win ? $"YOU WIN (+{Mathf.Max(0, gm.SessionCurrency)} currency) (R to restart)" : "GAME OVER (R to restart)";
        GUI.Label(new Rect(12, 136, 500, 30), text, _titleStyle);
    }

    void DrawStartPanel(GameManager3D gm)
    {
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.62f));
        float panelW = Screen.width * 0.9f;
        float panelH = Screen.height * 0.86f;
        float x = (Screen.width - panelW) * 0.5f;
        float y = (Screen.height - panelH) * 0.5f;

        DrawRect(new Rect(x, y, panelW, panelH), new Color(0.03f, 0.05f, 0.08f, 0.9f));
        DrawRect(new Rect(x + 8f, y + 8f, panelW - 16f, panelH - 16f), new Color(0.07f, 0.1f, 0.16f, 0.9f));

        GUI.Label(new Rect(x + 40f, y + 42f, panelW - 80f, 72f), "MONSTER WAVE CHALLENGE", _menuTitleStyle);
        GUI.Label(new Rect(x + 70f, y + 150f, panelW - 140f, 100f), "You have only 20 bullets to survive the wave.", _menuTextStyle);
        GUI.Label(new Rect(x + 70f, y + 236f, panelW - 140f, 100f), "Clear waves, choose upgrades, beat boss wave, enter portal.", _menuTextStyle);

        float buttonW = Mathf.Min(420f, panelW * 0.46f);
        float buttonH = 88f;
        float bx = x + (panelW - buttonW) * 0.5f;
        float by = y + panelH - buttonH - 52f;
        DrawRect(new Rect(bx - 6f, by - 6f, buttonW + 12f, buttonH + 12f), new Color(0f, 0f, 0f, 0.45f));
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.18f, 0.72f, 0.26f, 1f);
        if (GUI.Button(new Rect(bx, by, buttonW, buttonH), "Играть", _buttonStyle))
            gm.StartGameFromMenu();
        GUI.backgroundColor = prev;
    }

    void DrawUpgradePanel(GameManager3D gm)
    {
        float w = Mathf.Min(900f, Screen.width * 0.86f);
        float h = 260f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.2f;
        DrawRect(new Rect(x, y, w, h), new Color(0f, 0f, 0f, 0.72f));
        GUI.Label(new Rect(x + 20f, y + 16f, w - 40f, 42f), "Choose upgrade: press 1 / 2 / 3", _titleStyle);
        GUI.Label(new Rect(x + 20f, y + 48f, w - 40f, 24f), "If you do nothing, upgrade #1 is auto-picked in ~12s", _style);
        string[] choices = gm.UpgradeChoices;
        if (choices == null || choices.Length < 3) return;
        for (int i = 0; i < 3; i++)
        {
            float bx = x + 20f + i * ((w - 60f) / 3f);
            float bw = (w - 80f) / 3f;
            DrawRect(new Rect(bx, y + 70f, bw, 150f), new Color(0.08f, 0.11f, 0.17f, 0.94f));
            GUI.Label(new Rect(bx + 12f, y + 96f, bw - 24f, 64f), $"{i + 1}. {choices[i]}", _menuTextStyle);
        }
    }

    void DrawRect(Rect rect, Color color)
    {
        if (_solidTex == null) return;
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _solidTex);
        GUI.color = prev;
    }

    void EnsureRuntimeGuiStyles()
    {
        if (_buttonStyle != null) return;
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    void OnDestroy()
    {
        if (_solidTex != null)
            Destroy(_solidTex);
    }
}
