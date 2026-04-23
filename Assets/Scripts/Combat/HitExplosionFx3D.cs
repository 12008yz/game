using UnityEngine;
using UnityEngine.Rendering;

public class HitExplosionFx3D : MonoBehaviour
{
    static Material _fxMaterial;
    float _time;
    float _duration;
    Color _baseColor;

    public static void Spawn(Vector3 worldPos, float size = 0.42f, float duration = 0.12f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HitFx";
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
        Object.Destroy(go.GetComponent<Collider>());

        var fx = go.AddComponent<HitExplosionFx3D>();
        fx._duration = Mathf.Max(0.05f, duration);
        fx._baseColor = new Color(1f, 0.75f, 0.2f, 1f);

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (_fxMaterial == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _fxMaterial = new Material(sh);
            }

            var inst = new Material(_fxMaterial);
            if (inst.HasProperty("_BaseColor")) inst.SetColor("_BaseColor", fx._baseColor);
            else inst.color = fx._baseColor;
            r.sharedMaterial = inst;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / _duration);

        float scale = Mathf.Lerp(1f, 2.4f, t);
        transform.localScale *= (1f + Time.deltaTime * 10f);
        if (transform.localScale.x > scale) transform.localScale = Vector3.one * scale;

        var r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
        {
            Color c = _baseColor;
            c.a = 1f - t;
            if (r.sharedMaterial.HasProperty("_BaseColor")) r.sharedMaterial.SetColor("_BaseColor", c);
            else r.sharedMaterial.color = c;
        }

        if (_time >= _duration)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        var r = GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
            Destroy(r.sharedMaterial);
    }
}
