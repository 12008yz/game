using UnityEngine;
using UnityEngine.Rendering;

public class VictoryPortal3D : MonoBehaviour
{
    static Material _ringMat;
    static Material _coreMat;

    Transform _ringA;
    Transform _ringB;
    Transform _core;
    float _t;

    void Awake()
    {
        BuildVisual();

        var trigger = gameObject.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.height = 2.2f;
        trigger.radius = 0.9f;
        trigger.center = new Vector3(0f, 1.1f, 0f);
    }

    void BuildVisual()
    {
        _ringA = CreateRing("RingA", new Vector3(0f, 0.08f, 0f), new Vector3(1.8f, 0.08f, 1.8f), GetRingMaterial());
        _ringB = CreateRing("RingB", new Vector3(0f, 0.14f, 0f), new Vector3(1.2f, 0.06f, 1.2f), GetRingMaterial());
        _core = CreateCore();
        transform.localScale = Vector3.zero;
    }

    Transform CreateRing(string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        Destroy(go.GetComponent<Collider>());

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        return go.transform;
    }

    Transform CreateCore()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Core";
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f);
        Destroy(go.GetComponent<Collider>());

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = GetCoreMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        return go.transform;
    }

    static Material GetRingMaterial()
    {
        if (_ringMat != null) return _ringMat;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _ringMat = new Material(sh);
        if (_ringMat.HasProperty("_BaseColor")) _ringMat.SetColor("_BaseColor", new Color(0.1f, 0.95f, 0.35f));
        else _ringMat.color = new Color(0.1f, 0.95f, 0.35f);
        return _ringMat;
    }

    static Material GetCoreMaterial()
    {
        if (_coreMat != null) return _coreMat;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _coreMat = new Material(sh);
        if (_coreMat.HasProperty("_BaseColor")) _coreMat.SetColor("_BaseColor", new Color(0.45f, 1f, 0.6f, 0.75f));
        else _coreMat.color = new Color(0.45f, 1f, 0.6f, 0.75f);
        return _coreMat;
    }

    void Update()
    {
        _t += Time.deltaTime;

        // Smooth and cheap spawn animation.
        float appear = Mathf.Clamp01(_t / 0.6f);
        float pulse = 1f + Mathf.Sin(_t * 4.8f) * 0.06f;
        transform.localScale = Vector3.one * (appear * pulse);

        if (_ringA != null) _ringA.Rotate(0f, 65f * Time.deltaTime, 0f, Space.Self);
        if (_ringB != null) _ringB.Rotate(0f, -95f * Time.deltaTime, 0f, Space.Self);
        if (_core != null) _core.localPosition = new Vector3(0f, 0.7f + Mathf.Sin(_t * 3.2f) * 0.07f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<PlayerController3D>() == null) return;
        if (GameManager3D.Instance != null)
            GameManager3D.Instance.NotifyPortalReached();
    }
}
