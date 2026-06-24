#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Wolf.Protocol;

public static class WolfSceneVerifier
{
    const string ScenePath = "Assets/Scenes/BeatEmUpTest.unity";

    [MenuItem("Window/WOLF/Verify Beat-Em-Up Scene")]
    public static void VerifySceneMenu()
    {
        if (VerifyScene()) Debug.Log("[WolfSceneVerifier] PASS");
        else Debug.LogError("[WolfSceneVerifier] FAIL");
    }

    public static void VerifySceneBatch()
    {
        var ok = VerifyScene();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool VerifyScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (!scene.IsValid())
        {
            Debug.LogError($"[WolfSceneVerifier] Scene missing: {ScenePath}");
            return false;
        }

        var setup = Object.FindAnyObjectByType<WolfSceneSetup>();
        var mode = Object.FindAnyObjectByType<BeatEmUpMode>();
        if (setup == null || mode == null)
        {
            Debug.LogError("[WolfSceneVerifier] Missing WolfSceneSetup or BeatEmUpMode.");
            return false;
        }

        if (setup.PlayerPrefab == null)
            Debug.LogWarning("[WolfSceneVerifier] PlayerPrefab not assigned; runtime will use placeholder.");

        var built = WolfScene.Build(new WolfScene.Config
        {
            ArenaCenter = setup.ArenaCenter,
            ArenaSize = setup.ArenaSize,
            PlayerPrefab = setup.PlayerPrefab,
            EnemyPrefab = setup.EnemyPrefab,
            EnemyCount = 3,
            CameraMode = setup.CameraMode,
            CameraOrthographicSize = setup.CameraOrthographicSize,
        });

        if (built.Player == null || built.CameraRig == null)
        {
            Debug.LogError("[WolfSceneVerifier] WolfScene.Build failed.");
            Object.DestroyImmediate(built.Root);
            return false;
        }

        mode.BindPlayer(built.Player, built.CameraRig);
        var ySorted = built.Player.GetComponent<YSorter>() != null;
        Object.DestroyImmediate(built.Root);

        if (!ySorted)
        {
            Debug.LogError("[WolfSceneVerifier] Player missing YSorter.");
            return false;
        }

        Debug.Log("[WolfSceneVerifier] Scene wiring OK — WolfScene builds player, camera rig, enemies, Y-sort.");
        return true;
    }
}
#endif
