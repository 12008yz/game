using UnityEngine;

public static class BulletFactory3D
{
    static Material _bulletMaterial;
    static readonly System.Collections.Generic.Stack<Bullet3D> _pool = new System.Collections.Generic.Stack<Bullet3D>(96);
    static readonly System.Collections.Generic.List<Bullet3D> _all = new System.Collections.Generic.List<Bullet3D>(96);
    const float BulletLifetimeSeconds = 3f;
    static bool _prewarmed;

    public static void Spawn(Vector3 position, Vector3 direction, float speed)
    {
        EnsurePrewarmed();
        var bullet = _pool.Count > 0 ? _pool.Pop() : CreateBullet();
        var go = bullet.gameObject;
        go.SetActive(true);
        go.transform.position = position;
        bullet.Init(direction, speed, BulletLifetimeSeconds, Release);
    }

    static void EnsurePrewarmed()
    {
        if (_prewarmed) return;
        _prewarmed = true;
        for (int i = 0; i < 48; i++)
        {
            var b = CreateBullet();
            b.gameObject.SetActive(false);
            _pool.Push(b);
        }
    }

    static Bullet3D CreateBullet()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Bullet";
        go.transform.localScale = Vector3.one * 0.2f;
        Object.Destroy(go.GetComponent<Collider>());

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var b = go.AddComponent<Bullet3D>();
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (_bulletMaterial == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _bulletMaterial = new Material(sh);
                _bulletMaterial.enableInstancing = true;
                if (_bulletMaterial.HasProperty("_BaseColor")) _bulletMaterial.SetColor("_BaseColor", new Color(1f, 0.92f, 0.2f));
                else _bulletMaterial.color = new Color(1f, 0.92f, 0.2f);
            }
            r.sharedMaterial = _bulletMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        _all.Add(b);
        return b;
    }

    static void Release(Bullet3D bullet)
    {
        if (bullet == null) return;
        var go = bullet.gameObject;
        if (!go.activeSelf) return;
        go.SetActive(false);
        _pool.Push(bullet);
    }
}
