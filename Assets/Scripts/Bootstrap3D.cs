using UnityEngine;

public static class Bootstrap3D
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSceneSetup()
    {
        if (Object.FindFirstObjectByType<LevelBuilder>() == null)
        {
            var go = new GameObject("GeneratedLevel");
            go.AddComponent<LevelBuilder>();
        }

        if (Object.FindFirstObjectByType<GameManager3D>() == null)
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameManager3D>();
            go.AddComponent<GameHUD3D>();
        }
    }
}
