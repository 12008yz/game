using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimePrefabLoader3D
{
    public static GameObject Load(string resourcePathOrAssetPath)
    {
        if (string.IsNullOrWhiteSpace(resourcePathOrAssetPath))
            return null;

        var fromResources = Resources.Load<GameObject>(resourcePathOrAssetPath);
        if (fromResources != null) return fromResources;

#if UNITY_EDITOR
        if (resourcePathOrAssetPath.StartsWith("Assets/"))
            return AssetDatabase.LoadAssetAtPath<GameObject>(resourcePathOrAssetPath);
#endif
        return null;
    }
}
