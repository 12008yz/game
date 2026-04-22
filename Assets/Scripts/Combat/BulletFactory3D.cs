using UnityEngine;

public static class BulletFactory3D
{
    static Material _bulletMaterial;

    public static void Spawn(Vector3 position, Vector3 direction, float speed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Bullet";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.2f;
        Object.Destroy(go.GetComponent<Collider>());

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var b = go.AddComponent<Bullet3D>();
        b.Init(direction, speed);

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (_bulletMaterial == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _bulletMaterial = new Material(sh);
                if (_bulletMaterial.HasProperty("_BaseColor")) _bulletMaterial.SetColor("_BaseColor", new Color(1f, 0.92f, 0.2f));
                else _bulletMaterial.color = new Color(1f, 0.92f, 0.2f);
            }
            r.sharedMaterial = _bulletMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        Object.Destroy(go, 3f);
    }
}
