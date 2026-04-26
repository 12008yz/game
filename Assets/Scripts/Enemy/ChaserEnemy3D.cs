using UnityEngine;

public class ChaserEnemy3D : MonoBehaviour
{
    [SerializeField] GameBalance3D balance;
    [SerializeField] float speed = 2.3f;
    [SerializeField] float turnSpeed = 12f;
    [SerializeField] int hp = 1;
    [SerializeField] float bodyRadius = 0.32f;
    [SerializeField] float bodyHeight = 1f;
    [SerializeField] float skin = 0.03f;
    [SerializeField] float attackAnimInterval = 0.35f;
    [SerializeField] float attackPreviewDistance = 2.2f;
    [SerializeField] string[] enemyResourcePaths =
    {
        "Assets/Ultimate Animated Character Pack - Nov 2019/FBX/Zombie_Male.fbx",
        "Assets/Ultimate Animated Character Pack - Nov 2019/FBX/Goblin_Male.fbx",
        "Assets/Ultimate Animated Character Pack - Nov 2019/FBX/Knight_Male.fbx",
        "Assets/Ultimate Animated Character Pack - Nov 2019/FBX/Wizard.fbx",
        "Assets/Ultimate Animated Character Pack - Nov 2019/FBX/Pirate_Male.fbx"
    };
    Transform _player;
    CapsuleCollider _bodyCollider;
    CharacterPlayableAnimator3D _visualAnimator;
    int _maxHp;
    Transform _hpRoot;
    Transform _hpFill;
    float _attackAnimTimer;
    float _nextOverlapResolveTime;
    float _overlapInterval;
    const float GroundY = 0f;

    void Awake()
    {
        if (balance == null)
            balance = Resources.Load<GameBalance3D>("GameBalance3D");
        if (balance != null)
        {
            speed = balance.enemyMoveSpeed;
            hp = balance.enemyHp;
        }
        _overlapInterval = Random.Range(0.08f, 0.16f);
        _nextOverlapResolveTime = Time.time + Random.Range(0f, _overlapInterval);
        EnsureVisual();
        _maxHp = Mathf.Max(1, hp);
        CreateHealthBar();
        _bodyCollider = gameObject.AddComponent<CapsuleCollider>();
        _bodyCollider.height = bodyHeight;
        _bodyCollider.radius = bodyRadius;
        _bodyCollider.center = new Vector3(0f, bodyHeight * 0.5f, 0f);

        var rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Start()
    {
        var p = FindFirstObjectByType<PlayerController3D>();
        _player = p != null ? p.transform : null;
    }

    void OnEnable()
    {
        EnemyRegistry3D.Register(this);
    }

    void OnDisable()
    {
        EnemyRegistry3D.Unregister(this);
    }

    void Update()
    {
        if (_player == null) return;
        if (GameManager3D.Instance != null && GameManager3D.Instance.GameOver) return;

        var p0 = transform.position;
        if (Mathf.Abs(p0.y - GroundY) > 0.001f)
        {
            p0.y = GroundY;
            transform.position = p0;
        }

        if (Time.time >= _nextOverlapResolveTime)
        {
            _nextOverlapResolveTime = Time.time + _overlapInterval;
            ResolveOverlaps();
        }

        Vector3 pos = transform.position;
        Vector3 target = _player.position;
        target.y = pos.y;
        Vector3 d = target - pos;
        if (d.sqrMagnitude > 0.0001f)
        {
            Vector3 step = d.normalized * (speed * Time.deltaTime);
            MoveWithCollision(step);
            RotateTowards(d);
            if (_visualAnimator != null)
                _visualAnimator.SetMoveAmount(1f);

            if (_visualAnimator != null && _attackAnimTimer <= 0f && d.sqrMagnitude <= attackPreviewDistance * attackPreviewDistance)
            {
                _visualAnimator.TriggerAttack();
                _attackAnimTimer = attackAnimInterval;
            }
        }

        UpdateHealthBar();
        if (_attackAnimTimer > 0f)
            _attackAnimTimer -= Time.deltaTime;

        // Keep enemies on navigation plane to avoid vertical "launch" artifacts.
        var pg = transform.position;
        if (Mathf.Abs(pg.y - GroundY) > 0.001f)
        {
            pg.y = GroundY;
            transform.position = pg;
        }
    }

    void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void MoveWithCollision(Vector3 step)
    {
        if (step.sqrMagnitude <= 0.000001f) return;

        Vector3 basePos = transform.position;
        Vector3 up = Vector3.up;
        float half = Mathf.Max(bodyHeight * 0.5f - bodyRadius, 0.01f);
        Vector3 p1 = basePos + up * (bodyRadius + skin);
        Vector3 p2 = p1 + up * (half * 2f);
        float dist = step.magnitude;
        Vector3 dir = step / dist;

        if (!Physics.CapsuleCast(p1, p2, bodyRadius, dir, out RaycastHit hit, dist + skin, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 next = basePos + step;
            next.y = GroundY;
            transform.position = next;
            return;
        }

        // Move up to obstacle and then try sliding along surface.
        float allowed = Mathf.Max(0f, hit.distance - skin);
        Vector3 moved = basePos + dir * allowed;
        Vector3 remain = step - dir * allowed;
        Vector3 slide = Vector3.ProjectOnPlane(remain, hit.normal);
        slide.y = 0f;

        if (slide.sqrMagnitude <= 0.000001f)
        {
            moved.y = GroundY;
            transform.position = moved;
            return;
        }

        Vector3 sdir = slide.normalized;
        float sdist = slide.magnitude;
        Vector3 sp1 = moved + up * (bodyRadius + skin);
        Vector3 sp2 = sp1 + up * (half * 2f);

        if (!Physics.CapsuleCast(sp1, sp2, bodyRadius, sdir, out RaycastHit sh, sdist + skin, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 next = moved + slide;
            next.y = GroundY;
            transform.position = next;
        }
        else
        {
            Vector3 next = moved + sdir * Mathf.Max(0f, sh.distance - skin);
            next.y = GroundY;
            transform.position = next;
        }
    }

    void ResolveOverlaps()
    {
        if (_bodyCollider == null) return;

        Vector3 up = Vector3.up;
        float half = Mathf.Max(bodyHeight * 0.5f - bodyRadius, 0.01f);
        Vector3 p1 = transform.position + up * bodyRadius;
        Vector3 p2 = p1 + up * (half * 2f);

        var overlaps = Physics.OverlapCapsule(p1, p2, bodyRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            var other = overlaps[i];
            if (other == null || other == _bodyCollider) continue;
            if (other.GetComponentInParent<PlayerController3D>() != null) continue;

            if (Physics.ComputePenetration(
                _bodyCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                Vector3 planar = new Vector3(dir.x, 0f, dir.z);
                if (planar.sqrMagnitude > 0.000001f)
                    transform.position += planar.normalized * (dist + skin);
            }
        }
    }

    void EnsureVisual()
    {
        var prefab = LoadRandomEnemyPrefab();
        if (prefab != null)
        {
            var m = Instantiate(prefab, transform);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one * 0.82f;
            foreach (var c in m.GetComponentsInChildren<Collider>(true))
                Destroy(c);
            if (m.GetComponent<Animator>() == null)
                m.AddComponent<Animator>();
            _visualAnimator = m.GetComponent<CharacterPlayableAnimator3D>();
            if (_visualAnimator == null)
                _visualAnimator = m.AddComponent<CharacterPlayableAnimator3D>();
            return;
        }

        var fallback = CreateFallbackModel();
        fallback.name = "Model";
        fallback.transform.SetParent(transform);
        fallback.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one * 0.5f;
        Destroy(fallback.GetComponent<Collider>());
    }

    GameObject LoadRandomEnemyPrefab()
    {
        if (enemyResourcePaths == null || enemyResourcePaths.Length == 0) return null;
        int start = Random.Range(0, enemyResourcePaths.Length);
        for (int i = 0; i < enemyResourcePaths.Length; i++)
        {
            string path = enemyResourcePaths[(start + i) % enemyResourcePaths.Length];
            if (string.IsNullOrWhiteSpace(path)) continue;
            var prefab = RuntimePrefabLoader3D.Load(path);
            if (prefab != null) return prefab;
        }
        return null;
    }

    GameObject CreateFallbackModel()
    {
        PrimitiveType primitive = PrimitiveType.Capsule;
        int roll = Random.Range(0, 3);
        if (roll == 1) primitive = PrimitiveType.Cube;
        else if (roll == 2) primitive = PrimitiveType.Sphere;

        var go = GameObject.CreatePrimitive(primitive);
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            Color[] palette =
            {
                new Color(0.6f, 0.2f, 0.2f),
                new Color(0.5f, 0.22f, 0.38f),
                new Color(0.45f, 0.34f, 0.18f),
                new Color(0.28f, 0.48f, 0.22f)
            };
            Color c = palette[Random.Range(0, palette.Length)];
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
            else material.color = c;
            renderer.sharedMaterial = material;
        }

        return go;
    }

    void CreateHealthBar()
    {
        _hpRoot = new GameObject("HpBar").transform;
        _hpRoot.SetParent(transform);
        _hpRoot.localPosition = new Vector3(0f, bodyHeight + 0.45f, 0f);
        _hpRoot.localRotation = Quaternion.identity;

        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "Bg";
        bg.transform.SetParent(_hpRoot);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.72f, 0.08f, 0.04f);
        Destroy(bg.GetComponent<Collider>());
        PaintHealthPart(bg, new Color(0.18f, 0.18f, 0.18f));

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "Fill";
        fill.transform.SetParent(_hpRoot);
        fill.transform.localPosition = new Vector3(-0.18f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(0.68f, 0.05f, 0.03f);
        Destroy(fill.GetComponent<Collider>());
        PaintHealthPart(fill, new Color(0.16f, 0.82f, 0.24f));
        _hpFill = fill.transform;
    }

    static void PaintHealthPart(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void UpdateHealthBar()
    {
        if (_hpRoot == null || _hpFill == null) return;
        var cam = Camera.main;
        if (cam != null)
            _hpRoot.rotation = cam.transform.rotation;

        float t = Mathf.Clamp01((float)hp / _maxHp);
        _hpFill.localScale = new Vector3(0.68f * t, 0.05f, 0.03f);
        _hpFill.localPosition = new Vector3((-0.34f + (0.68f * t) * 0.5f), 0f, -0.01f);
    }

    public void TakeHit()
    {
        hp--;
        UpdateHealthBar();
        if (hp > 0) return;
        if (GameManager3D.Instance != null) GameManager3D.Instance.NotifyEnemyKilled();
        Destroy(gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        var p = collision.gameObject.GetComponent<PlayerController3D>();
        if (p != null && GameManager3D.Instance != null)
        {
            if (_visualAnimator != null && _attackAnimTimer <= 0f)
            {
                _visualAnimator.TriggerAttack();
                _attackAnimTimer = attackAnimInterval;
            }
            GameManager3D.Instance.NotifyPlayerDied();
        }
    }
}
