using UnityEngine;

public class Bullet3D : MonoBehaviour
{
    static readonly RaycastHit[] HitBuffer = new RaycastHit[16];
    static readonly Collider[] OverlapBuffer = new Collider[16];

    Vector3 _dir;
    float _speed;
    float _radius;
    float _lifeLeft;
    System.Action<Bullet3D> _releaseToPool;
    Collider _selfCollider;

    void Awake()
    {
        _selfCollider = GetComponent<Collider>();
    }

    public void Init(Vector3 direction, float speed, float lifeSeconds, System.Action<Bullet3D> releaseToPool)
    {
        _dir = direction.normalized;
        _speed = speed;
        _radius = Mathf.Max(0.06f, transform.localScale.x * 0.5f);
        _lifeLeft = Mathf.Max(0.1f, lifeSeconds);
        _releaseToPool = releaseToPool;
    }

    void Update()
    {
        _lifeLeft -= Time.deltaTime;
        if (_lifeLeft <= 0f)
        {
            Release();
            return;
        }

        Vector3 current = transform.position;
        if (CheckOverlaps(current))
            return;

        Vector3 step = _dir * (_speed * Time.deltaTime);
        float distance = step.magnitude;

        if (distance > 0f)
        {
            int hitCount = Physics.SphereCastNonAlloc(current, _radius, _dir, HitBuffer, distance, ~0, QueryTriggerInteraction.Collide);
            if (hitCount > 0 && ProcessCastHits(hitCount))
                return;
        }

        transform.position = current + step;
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = other != null ? other.ClosestPoint(transform.position) : transform.position;
        ProcessHit(other, hitPoint);
    }

    bool ProcessHit(Collider other, Vector3 hitPoint)
    {
        if (other == null) return false;
        if (_selfCollider != null && other == _selfCollider) return false;

        if (other.GetComponentInParent<PlayerController3D>() != null)
            return false;

        var enemy = other.GetComponentInParent<ChaserEnemy3D>();
        if (enemy != null)
        {
            HitExplosionFx3D.Spawn(hitPoint + Vector3.up * 0.1f);
            enemy.TakeHit();
            Release();
            return true;
        }

        if (other.GetComponentInParent<WallTag>() != null)
        {
            Release();
            return true;
        }

        return false;
    }

    bool ProcessCastHits(int hitCount)
    {
        int bestIdx = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            var h = HitBuffer[i];
            if (h.collider == null) continue;
            if (_selfCollider != null && h.collider == _selfCollider) continue;
            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                bestIdx = i;
            }
        }

        if (bestIdx < 0) return false;
        var bestHit = HitBuffer[bestIdx];
        return ProcessHit(bestHit.collider, bestHit.point);
    }

    bool CheckOverlaps(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(position, _radius, OverlapBuffer, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var c = OverlapBuffer[i];
            if (c == null) continue;
            if (_selfCollider != null && c == _selfCollider) continue;
            if (ProcessHit(c, c.ClosestPoint(position)))
                return true;
        }
        return false;
    }

    void Release()
    {
        if (_releaseToPool != null)
            _releaseToPool(this);
        else
            gameObject.SetActive(false);
    }
}
