using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    [SerializeField] int width = 26;
    [SerializeField] int height = 16;
    [SerializeField] float cellSize = 1f;
    [SerializeField] float wallHeight = 1.4f;

    [SerializeField] int enemyCount = 5;

    void Awake()
    {
        BuildArena();
        EnsurePlayer();
        EnsureCamera();
        EnsureLight();
        SpawnEnemies();
    }

    void BuildArena()
    {
        var root = new GameObject("ArenaRoot").transform;
        root.SetParent(transform);

        float w = width * cellSize;
        float h = height * cellSize;
        Vector3 center = new Vector3(0f, -0.05f, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root);
        floor.transform.position = center;
        floor.transform.localScale = new Vector3(w, 0.1f, h);
        Paint(floor, new Color(0.1f, 0.12f, 0.18f));

        CreateWall(root, new Vector3(0f, wallHeight * 0.5f, h * 0.5f), new Vector3(w, wallHeight, 1f));
        CreateWall(root, new Vector3(0f, wallHeight * 0.5f, -h * 0.5f), new Vector3(w, wallHeight, 1f));
        CreateWall(root, new Vector3(-w * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, h));
        CreateWall(root, new Vector3(w * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, h));
    }

    void CreateWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Paint(wall, new Color(0.32f, 0.34f, 0.42f));
        wall.AddComponent<WallTag>();
    }

    static void Paint(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else m.color = c;
        r.sharedMaterial = m;
    }

    void EnsurePlayer()
    {
        if (FindFirstObjectByType<PlayerController3D>() != null) return;
        var p = new GameObject("Player");
        p.transform.position = new Vector3(0f, 0f, -3f);

        var cc = p.AddComponent<CharacterController>();
        cc.height = 1.2f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.6f, 0f);

        p.AddComponent<PlayerController3D>();
        p.AddComponent<PlayerVisualSetup3D>();
    }

    void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 50f;
        }

        var follow = cam.GetComponent<CameraFollow3D>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow3D>();
        var player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) follow.target = player.transform;
    }

    void EnsureLight()
    {
        var global2D = GameObject.Find("Global Light 2D");
        if (global2D != null) global2D.SetActive(false);

        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) return;

        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    void SpawnEnemies()
    {
        if (FindObjectsByType<ChaserEnemy3D>(FindObjectsSortMode.None).Length > 0) return;

        for (int i = 0; i < enemyCount; i++)
        {
            float x = Random.Range(-9f, 9f);
            float z = Random.Range(-5f, 5f);
            var e = new GameObject("Enemy");
            e.transform.position = new Vector3(x, 0.35f, z);
            e.AddComponent<ChaserEnemy3D>();
        }
    }
}

public class WallTag : MonoBehaviour { }
