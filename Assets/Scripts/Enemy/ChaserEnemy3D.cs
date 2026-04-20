using UnityEngine;

public class ChaserEnemy3D : MonoBehaviour
{
    [SerializeField] float speed = 2.3f;
    [SerializeField] int hp = 2;
    Transform _player;

    void Awake()
    {
        EnsureVisual();
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.height = 1f;
        col.radius = 0.32f;
        col.center = new Vector3(0f, 0.5f, 0f);

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

    void Update()
    {
        if (_player == null) return;
        if (GameManager3D.Instance != null && GameManager3D.Instance.GameOver) return;

        Vector3 pos = transform.position;
        Vector3 target = _player.position;
        target.y = pos.y;
        Vector3 d = target - pos;
        if (d.sqrMagnitude > 0.0001f)
        {
            Vector3 step = d.normalized * (speed * Time.deltaTime);
            transform.position += step;
        }
    }

    void EnsureVisual()
    {
        var prefab = Resources.Load<GameObject>("Kenney/ChaserModel");
        if (prefab != null)
        {
            var m = Instantiate(prefab, transform);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one * 0.45f;
            foreach (var c in m.GetComponentsInChildren<Collider>(true))
                Destroy(c);
            return;
        }

        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.name = "Model";
        cap.transform.SetParent(transform);
        cap.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        cap.transform.localScale = Vector3.one * 0.5f;
        Destroy(cap.GetComponent<Collider>());
    }

    public void TakeHit()
    {
        hp--;
        if (hp > 0) return;
        if (GameManager3D.Instance != null) GameManager3D.Instance.NotifyEnemyKilled();
        Destroy(gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        var p = collision.gameObject.GetComponent<PlayerController3D>();
        if (p != null && GameManager3D.Instance != null)
            GameManager3D.Instance.NotifyPlayerDied();
    }
}
