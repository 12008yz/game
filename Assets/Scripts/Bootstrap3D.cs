using UnityEngine;

public static class Bootstrap3D
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSceneSetup()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 75;
        QualitySettings.antiAliasing = 0;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 24f;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;

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
