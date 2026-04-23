using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class LevelBuilder : MonoBehaviour
{
    static Material _floorMat;
    static Material _wallMat;
    static Material _obstacleMat;
    static Material _buildingMat;
    static Material _roofMat;
    static Material _chestBodyMat;
    static Material _chestLidMat;

    [SerializeField] int width = 48;
    [SerializeField] int height = 32;
    [SerializeField] float cellSize = 1f;
    [SerializeField] float wallHeight = 2.2f;

    [SerializeField] int enemyCount = 10;
    public int BaseEnemyCount => enemyCount;

    [SerializeField] int obstacleCount = 11;
    [SerializeField] int buildingCount = 4;
    [SerializeField] int chestCount = 7;

    void Awake()
    {
        BuildArena();
        SpawnObstacles();
        SpawnBuildings();
        SpawnChests();
        EnsurePlayer();
        EnsureCamera();
        EnsureLight();
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
        Paint(floor, GetOrCreateMaterial(ref _floorMat, new Color(0.1f, 0.12f, 0.18f)));

        CreateWall(root, new Vector3(0f, wallHeight * 0.5f, h * 0.5f), new Vector3(w, wallHeight, 1.2f));
        CreateWall(root, new Vector3(0f, wallHeight * 0.5f, -h * 0.5f), new Vector3(w, wallHeight, 1.2f));
        CreateWall(root, new Vector3(-w * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1.2f, wallHeight, h));
        CreateWall(root, new Vector3(w * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1.2f, wallHeight, h));
    }

    void CreateWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Paint(wall, GetOrCreateMaterial(ref _wallMat, new Color(0.32f, 0.34f, 0.42f)));
        wall.AddComponent<WallTag>();
    }

    static Material GetOrCreateMaterial(ref Material cache, Color c)
    {
        if (cache != null) return cache;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        cache = new Material(shader);
        if (cache.HasProperty("_BaseColor")) cache.SetColor("_BaseColor", c);
        else cache.color = c;
        return cache;
    }

    static void Paint(GameObject go, Material m)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null || m == null) return;
        r.sharedMaterial = m;
    }

    static void DisableShadows(GameObject go)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
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
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.useOcclusionCulling = false;

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
        light.intensity = 1.05f;
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 0.7f;
        light.shadowBias = 0.08f;
        light.shadowNormalBias = 0.4f;
        go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    public void SpawnEnemyWave(int count)
    {
        int safeCount = Mathf.Max(1, count);
        var wavePositions = new List<Vector3>(safeCount);
        for (int i = 0; i < safeCount; i++)
        {
            Vector3 p = RandomEnemySpawn(2.5f, wavePositions);
            wavePositions.Add(p);
            var e = new GameObject("Enemy");
            e.transform.position = p;
            e.AddComponent<ChaserEnemy3D>();
        }
    }

    void SpawnObstacles()
    {
        var root = new GameObject("Obstacles").transform;
        root.SetParent(transform);

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 p = RandomGroundPos(2f);
            var go = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = "Obstacle";
            go.transform.SetParent(root);
            go.transform.position = new Vector3(p.x, 0.35f, p.z);
            float sx = Random.Range(0.6f, 1.2f);
            float sy = Random.Range(0.7f, 1.6f);
            float sz = Random.Range(0.6f, 1.2f);
            go.transform.localScale = new Vector3(sx, sy, sz);
            Paint(go, GetOrCreateMaterial(ref _obstacleMat, new Color(0.26f, 0.28f, 0.33f)));
            DisableShadows(go);
            go.AddComponent<WallTag>();
        }
    }

    void SpawnBuildings()
    {
        var root = new GameObject("Buildings").transform;
        root.SetParent(transform);

        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 p = RandomGroundPos(4.5f);

            var basePart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePart.name = "Building";
            basePart.transform.SetParent(root);
            float w = Random.Range(2.4f, 4.2f);
            float h = Random.Range(2.4f, 5.2f);
            float d = Random.Range(2.4f, 4.2f);
            basePart.transform.position = new Vector3(p.x, h * 0.5f, p.z);
            basePart.transform.localScale = new Vector3(w, h, d);
            Paint(basePart, GetOrCreateMaterial(ref _buildingMat, new Color(0.22f, 0.24f, 0.3f)));
            basePart.AddComponent<WallTag>();

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(basePart.transform);
            roof.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            roof.transform.localScale = new Vector3(1.1f, 0.12f, 1.1f);
            Paint(roof, GetOrCreateMaterial(ref _roofMat, new Color(0.38f, 0.4f, 0.45f)));
            DisableShadows(roof);
            roof.AddComponent<WallTag>();
        }
    }

    void SpawnChests()
    {
        var root = new GameObject("Chests").transform;
        root.SetParent(transform);

        for (int i = 0; i < chestCount; i++)
        {
            Vector3 p = RandomGroundPos(1.8f);

            var chest = new GameObject("Chest");
            chest.transform.SetParent(root);
            chest.transform.position = new Vector3(p.x, 0.22f, p.z);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(chest.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.7f, 0.4f, 0.5f);
            Paint(body, GetOrCreateMaterial(ref _chestBodyMat, new Color(0.42f, 0.26f, 0.12f)));
            DisableShadows(body);
            body.AddComponent<WallTag>();

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Lid";
            lid.transform.SetParent(chest.transform);
            lid.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            lid.transform.localScale = new Vector3(0.76f, 0.16f, 0.56f);
            Paint(lid, GetOrCreateMaterial(ref _chestLidMat, new Color(0.55f, 0.34f, 0.15f)));
            DisableShadows(lid);
            lid.AddComponent<WallTag>();
        }
    }

    Vector3 RandomGroundPos(float padding)
    {
        float halfW = width * cellSize * 0.5f - padding;
        float halfH = height * cellSize * 0.5f - padding;
        for (int i = 0; i < 35; i++)
        {
            float x = Random.Range(-halfW, halfW);
            float z = Random.Range(-halfH, halfH);
            var candidate = new Vector3(x, 0.2f, z);
            if (!Physics.CheckBox(candidate, new Vector3(0.65f, 0.65f, 0.65f)))
                return new Vector3(x, 0f, z);
        }
        return Vector3.zero;
    }

    Vector3 RandomEnemySpawn(float padding, List<Vector3> reservedPositions)
    {
        float radius = 0.32f;
        float height = 1f;
        float half = Mathf.Max(height * 0.5f - radius, 0.01f);
        var player = FindFirstObjectByType<PlayerController3D>();
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        float minDistanceFromPlayer = 6f;
        float minDistanceBetweenEnemies = 2.2f;
        float halfW = width * cellSize * 0.5f - padding;
        float halfH = height * cellSize * 0.5f - padding;

        for (int i = 0; i < 90; i++)
        {
            Vector3 p;
            if (Random.value < 0.75f)
            {
                bool alongX = Random.value < 0.5f;
                if (alongX)
                {
                    float x = (Random.value < 0.5f ? -halfW : halfW) + Random.Range(-1.8f, 1.8f);
                    float z = Random.Range(-halfH, halfH);
                    p = new Vector3(x, 0f, z);
                }
                else
                {
                    float x = Random.Range(-halfW, halfW);
                    float z = (Random.value < 0.5f ? -halfH : halfH) + Random.Range(-1.8f, 1.8f);
                    p = new Vector3(x, 0f, z);
                }
            }
            else
            {
                p = RandomGroundPos(padding);
            }

            p.x = Mathf.Clamp(p.x, -halfW, halfW);
            p.z = Mathf.Clamp(p.z, -halfH, halfH);

            if ((p - playerPos).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                continue;
            if (!IsFarFromReserved(p, reservedPositions, minDistanceBetweenEnemies))
                continue;

            Vector3 p1 = p + Vector3.up * radius;
            Vector3 p2 = p1 + Vector3.up * (half * 2f);
            if (!Physics.CheckCapsule(p1, p2, radius, ~0, QueryTriggerInteraction.Ignore))
                return p;
        }

        for (int i = 0; i < 40; i++)
        {
            Vector3 fallback = RandomGroundPos(padding);
            if ((fallback - playerPos).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer) continue;
            if (!IsFarFromReserved(fallback, reservedPositions, minDistanceBetweenEnemies)) continue;
            return fallback;
        }

        return playerPos + new Vector3(Random.Range(-halfW, halfW), 0f, Random.Range(-halfH, halfH));
    }

    static bool IsFarFromReserved(Vector3 p, List<Vector3> reserved, float minDistance)
    {
        if (reserved == null || reserved.Count == 0) return true;
        float sq = minDistance * minDistance;
        for (int i = 0; i < reserved.Count; i++)
        {
            if ((reserved[i] - p).sqrMagnitude < sq)
                return false;
        }
        return true;
    }
}

public class WallTag : MonoBehaviour { }
