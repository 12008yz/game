using UnityEngine;
using UnityEngine.Rendering;

public class BeamStripFx3D : MonoBehaviour
{
    Renderer _renderer;
    Material _material;
    bool _active;

    public static BeamStripFx3D Ensure(Transform parent)
    {
        if (parent == null) return null;
        var existing = parent.GetComponentInChildren<BeamStripFx3D>(true);
        if (existing != null) return existing;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BeamStripFx";
        go.transform.SetParent(parent);
        Object.Destroy(go.GetComponent<Collider>());
        var fx = go.AddComponent<BeamStripFx3D>();
        fx.SetupRenderer();
        fx.SetVisible(false);
        return fx;
    }

    public void SetBeam(Vector3 origin, Vector3 dir, float length, float width = 0.42f)
    {
        if (!_active) SetVisible(true);
        transform.position = origin + dir * (length * 0.5f);
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.localScale = new Vector3(width, 0.08f, Mathf.Max(0.2f, length));
    }

    public void StopBeam()
    {
        SetVisible(false);
    }

    void SetupRenderer()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null) return;
        var sh = Shader.Find("Universal RenderPipeline/Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        _material = new Material(sh);
        if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", new Color(0.2f, 1f, 0.95f, 0.85f));
        else _material.color = new Color(0.2f, 1f, 0.95f, 0.85f);
        _renderer.sharedMaterial = _material;
        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
    }

    void SetVisible(bool visible)
    {
        _active = visible;
        gameObject.SetActive(visible);
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
