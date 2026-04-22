using UnityEngine;

public class PlayerVisualSetup3D : MonoBehaviour
{
    [SerializeField] string heroResourcePath = "Models/character-r";
    [SerializeField] string weaponResourcePath = "Models/blaster-e";
    [SerializeField] string heroTextureResourcePath = "Models/texture-r";

    [SerializeField] Vector3 heroPos = new Vector3(0f, 0f, 0f);
    [SerializeField] Vector3 heroRot = new Vector3(0f, 0f, 0f);
    [SerializeField] Vector3 heroScale = Vector3.one;

    [SerializeField] Vector3 weaponPos = new Vector3(0.16f, 0.88f, 0.18f);
    [SerializeField] Vector3 weaponRot = new Vector3(8f, 95f, -6f);
    [SerializeField] Vector3 weaponScale = new Vector3(0.62f, 0.62f, 0.62f);

    void Awake()
    {
        RemoveLegacy();

        var hero = Ensure("HeroModel", Resources.Load<GameObject>(heroResourcePath), transform, heroPos, heroRot, heroScale);
        var weaponParent = hero != null ? FindWeaponAnchor(hero.transform) : transform;
        Ensure("WeaponModel", Resources.Load<GameObject>(weaponResourcePath), weaponParent, weaponPos, weaponRot, weaponScale);

        if (hero != null)
        {
            ApplyHeroTexture(hero.transform);
            AlignHeroFeetToGround(hero.transform);
        }
    }

    void RemoveLegacy()
    {
        var legacy = transform.Find("Model");
        if (legacy != null) Destroy(legacy.gameObject);
    }

    static GameObject Ensure(string childName, GameObject prefab, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        if (prefab == null || parent == null) return null;
        var existing = parent.Find(childName);
        if (existing != null)
        {
            existing.localPosition = pos;
            existing.localRotation = Quaternion.Euler(euler);
            existing.localScale = scale;
            return existing.gameObject;
        }

        var go = Instantiate(prefab, parent);
        go.name = childName;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            Destroy(c);
        return go;
    }

    static Transform FindWeaponAnchor(Transform heroRoot)
    {
        if (heroRoot == null) return null;

        string[] preferredNames =
        {
            "righthand", "right_hand", "hand_r", "weapon", "gun", "arm_r", "rightarm"
        };

        var all = heroRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < preferredNames.Length; i++)
        {
            string needle = preferredNames[i];
            for (int j = 0; j < all.Length; j++)
            {
                var t = all[j];
                if (t == null || t == heroRoot) continue;
                if (t.name.ToLowerInvariant().Contains(needle))
                    return t;
            }
        }

        return heroRoot;
    }

    void ApplyHeroTexture(Transform heroRoot)
    {
        var tex = Resources.Load<Texture2D>(heroTextureResourcePath);
        if (tex == null) return;
        foreach (var r in heroRoot.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var m in r.materials)
            {
                if (m == null) continue;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                else m.color = Color.white;
            }
        }
    }

    void AlignHeroFeetToGround(Transform heroRoot)
    {
        var renderers = heroRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        float minY = float.PositiveInfinity;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (r.bounds.min.y < minY) minY = r.bounds.min.y;
        }

        if (float.IsPositiveInfinity(minY)) return;

        // Put model feet just above the floor to avoid z-fighting.
        float lift = 0.01f - minY;
        heroRoot.position += new Vector3(0f, lift, 0f);
    }
}
