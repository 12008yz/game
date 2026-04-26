using UnityEngine;
using UnityEngine.Rendering;

public class HitExplosionFx3D : MonoBehaviour
{
    static Material _fxMaterial;
    static readonly System.Collections.Generic.Stack<HitExplosionFx3D> _pool = new System.Collections.Generic.Stack<HitExplosionFx3D>(64);
    static bool _prewarmed;
    float _time;
    float _duration;
    Color _baseColor;
    Renderer _renderer;
    MaterialPropertyBlock _mpb;
    Vector3 _baseScale;

    public static void Spawn(Vector3 worldPos, float size = 0.42f, float duration = 0.12f)
    {
        EnsurePrewarmed();
        var fx = _pool.Count > 0 ? _pool.Pop() : CreateFx();
        var go = fx.gameObject;
        go.SetActive(true);
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
        fx._baseScale = go.transform.localScale;

        fx._duration = Mathf.Max(0.05f, duration);
        fx._baseColor = new Color(1f, 0.75f, 0.2f, 1f);
        fx._time = 0f;
    }

    void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / _duration);

        float scale = Mathf.Lerp(1f, 2.4f, t);
        transform.localScale = _baseScale * scale;

        if (_renderer != null)
        {
            Color c = _baseColor;
            c.a = 1f - t;
            _mpb.SetColor("_BaseColor", c);
            _renderer.SetPropertyBlock(_mpb);
        }

        if (_time >= _duration)
            ReleaseToPool();
    }

    static void EnsurePrewarmed()
    {
        if (_prewarmed) return;
        _prewarmed = true;
        for (int i = 0; i < 28; i++)
        {
            var fx = CreateFx();
            fx.gameObject.SetActive(false);
            _pool.Push(fx);
        }
    }

    static HitExplosionFx3D CreateFx()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HitFx";
        Object.Destroy(go.GetComponent<Collider>());
        var fx = go.AddComponent<HitExplosionFx3D>();

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (_fxMaterial == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _fxMaterial = new Material(sh);
                _fxMaterial.enableInstancing = true;
            }
            r.sharedMaterial = _fxMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return fx;
    }

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void ReleaseToPool()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        _pool.Push(this);
    }
}
