using UnityEngine;

public class Bullet3D : MonoBehaviour
{
    Vector3 _dir;
    float _speed;
    float _radius;
    float _lifeLeft;
    System.Action<Bullet3D> _releaseToPool;

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
        Vector3 step = _dir * (_speed * Time.deltaTime);
        float distance = step.magnitude;

        if (distance > 0f)
        {
            if (Physics.SphereCast(current, _radius, _dir, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Collide))
            {
                if (ProcessHit(hit.collider, hit.point))
                    return;
            }
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

        if (other.GetComponent<WallTag>() != null)
        {
            Release();
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
