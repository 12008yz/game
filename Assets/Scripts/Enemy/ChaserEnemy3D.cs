using UnityEngine;

public class ChaserEnemy3D : MonoBehaviour
{
    [SerializeField] float speed = 2.3f;
    [SerializeField] float turnSpeed = 12f;
    [SerializeField] int hp = 2;
    [SerializeField] float bodyRadius = 0.32f;
    [SerializeField] float bodyHeight = 1f;
    [SerializeField] float skin = 0.03f;
    Transform _player;
    CapsuleCollider _bodyCollider;

    void Awake()
    {
        EnsureVisual();
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

    void Update()
    {
        if (_player == null) return;
        if (GameManager3D.Instance != null && GameManager3D.Instance.GameOver) return;

        ResolveOverlaps();

        Vector3 pos = transform.position;
        Vector3 target = _player.position;
        target.y = pos.y;
        Vector3 d = target - pos;
        if (d.sqrMagnitude > 0.0001f)
        {
            Vector3 step = d.normalized * (speed * Time.deltaTime);
            MoveWithCollision(step);
            RotateTowards(d);
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
            transform.position = basePos + step;
            return;
        }

        // Move up to obstacle and then try sliding along surface.
        float allowed = Mathf.Max(0f, hit.distance - skin);
        Vector3 moved = basePos + dir * allowed;
        Vector3 remain = step - dir * allowed;
        Vector3 slide = Vector3.ProjectOnPlane(remain, hit.normal);

        if (slide.sqrMagnitude <= 0.000001f)
        {
            transform.position = moved;
            return;
        }

        Vector3 sdir = slide.normalized;
        float sdist = slide.magnitude;
        Vector3 sp1 = moved + up * (bodyRadius + skin);
        Vector3 sp2 = sp1 + up * (half * 2f);

        if (!Physics.CapsuleCast(sp1, sp2, bodyRadius, sdir, out RaycastHit sh, sdist + skin, ~0, QueryTriggerInteraction.Ignore))
            transform.position = moved + slide;
        else
            transform.position = moved + sdir * Mathf.Max(0f, sh.distance - skin);
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
                transform.position += dir * (dist + skin);
            }
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
