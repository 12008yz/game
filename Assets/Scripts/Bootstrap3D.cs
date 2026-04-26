using UnityEngine;

public static class Bootstrap3D
{
    public enum RuntimeQualityTier
    {
        Low,
        Medium,
        High
    }

    static RuntimeQualityTier _currentTier = RuntimeQualityTier.Medium;
    public static RuntimeQualityTier CurrentTier => _currentTier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSceneSetup()
    {
        ApplyCrossPlatformQualityProfile(_currentTier);
        EnemyRegistry3D.EnsureExists();

        if (Object.FindFirstObjectByType<LevelBuilder>() == null)
        {
            var go = new GameObject("GeneratedLevel");
            go.AddComponent<LevelBuilder>();
        }

        if (Object.FindFirstObjectByType<GameManager3D>() == null)
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameManager3D>();
            go.AddComponent<GameHUDCanvas3D>();
            go.AddComponent<PerformanceProbe3D>();
            go.AddComponent<StressBenchmark3D>();
        }
    }

    public static void SetRuntimeQualityTier(RuntimeQualityTier tier)
    {
        _currentTier = tier;
        ApplyCrossPlatformQualityProfile(_currentTier);
        Debug.Log($"[Bootstrap3D] Runtime quality switched to {_currentTier}");
    }

    static void ApplyCrossPlatformQualityProfile(RuntimeQualityTier tier)
    {
        QualitySettings.vSyncCount = 0;
        bool mobile = Application.isMobilePlatform;

        int targetFps = mobile ? 60 : 90;
        int aa = mobile ? 0 : 2;
        int pixelLights = 1;
        ShadowResolution shadowResolution = ShadowResolution.Low;
        float shadowDistance = mobile ? 16f : 28f;

        if (tier == RuntimeQualityTier.Low)
        {
            targetFps = 60;
            aa = 0;
            pixelLights = 1;
            shadowResolution = ShadowResolution.Low;
            shadowDistance = mobile ? 12f : 18f;
        }
        else if (tier == RuntimeQualityTier.High)
        {
            targetFps = mobile ? 60 : 120;
            aa = mobile ? 0 : 4;
            pixelLights = 2;
            shadowResolution = ShadowResolution.Medium;
            shadowDistance = mobile ? 18f : 38f;
        }

        Application.targetFrameRate = targetFps;
        QualitySettings.antiAliasing = aa;
        QualitySettings.pixelLightCount = pixelLights;
        QualitySettings.shadowResolution = shadowResolution;
        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
    }
}
