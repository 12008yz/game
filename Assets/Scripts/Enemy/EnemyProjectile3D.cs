using UnityEngine;

public class EnemyProjectile3D : MonoBehaviour
{
    Vector3 _dir;
    float _speed;
    float _life;

    public static void Spawn(Vector3 position, Vector3 dir, float speed = 10f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnemyProjectile";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.2f;
        Object.Destroy(go.GetComponent<Collider>());
        var c = go.AddComponent<SphereCollider>();
        c.isTrigger = true;
        var p = go.AddComponent<EnemyProjectile3D>();
        p._dir = dir.normalized;
        p._speed = speed;
        p._life = 2.6f;
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, 0.3f, 0.25f));
            else m.color = new Color(1f, 0.3f, 0.25f);
            r.sharedMaterial = m;
        }
    }

    void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += _dir * (_speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<ChaserEnemy3D>() != null) return;
        if (other.GetComponentInParent<PlayerController3D>() != null)
        {
            if (GameManager3D.Instance != null)
                GameManager3D.Instance.NotifyPlayerDied();
            Destroy(gameObject);
            return;
        }

        if (other.GetComponentInParent<WallTag>() != null)
            Destroy(gameObject);
    }
}
