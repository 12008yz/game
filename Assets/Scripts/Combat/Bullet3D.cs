using UnityEngine;

public class Bullet3D : MonoBehaviour
{
    Vector3 _dir;
    float _speed;
    float _radius;

    public void Init(Vector3 direction, float speed)
    {
        _dir = direction.normalized;
        _speed = speed;
        _radius = Mathf.Max(0.06f, transform.localScale.x * 0.5f);
    }

    void Update()
    {
        Vector3 current = transform.position;
        Vector3 step = _dir * (_speed * Time.deltaTime);
        float distance = step.magnitude;

        if (distance > 0f)
        {
            if (Physics.SphereCast(current, _radius, _dir, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Collide))
            {
                if (ProcessHit(hit.collider))
                    return;
            }
        }

        transform.position = current + step;
    }

    void OnTriggerEnter(Collider other)
    {
        ProcessHit(other);
    }

    bool ProcessHit(Collider other)
    {
        if (other == null) return false;

        if (other.GetComponentInParent<PlayerController3D>() != null)
            return false;

        var enemy = other.GetComponentInParent<ChaserEnemy3D>();
        if (enemy != null)
        {
            enemy.TakeHit();
            Destroy(gameObject);
            return true;
        }

        if (other.GetComponent<WallTag>() != null)
        {
            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
