#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wolf.Protocol;

public static class WolfPlayModeTest
{
    const string ScenePath = "Assets/Scenes/BeatEmUpTest.unity";

    [MenuItem("Window/WOLF/Run Play Mode Smoke Test")]
    public static void RunSmokeTest()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void RunSmokeTestBatch()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
        EditorApplication.playModeStateChanged += OnPlayModeChangedBatch;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.delayCall += VerifyAndStopEditor;
    }

    static void OnPlayModeChangedBatch(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChangedBatch;
        EditorApplication.delayCall += VerifyAndStopBatch;
    }

    static void VerifyAndStopEditor()
    {
        if (!Verify()) EditorApplication.ExitPlaymode();
        else
        {
            Debug.Log("[WolfPlayModeTest] PASS — beat-em-up scene running with player, mode, and camera rig.");
            EditorApplication.ExitPlaymode();
        }
    }

    static void VerifyAndStopBatch()
    {
        Verify();
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(0);
    }

    static bool Verify()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        var mode = Object.FindAnyObjectByType<BeatEmUpMode>();
        var rig = Object.FindAnyObjectByType<CameraRig>();

        if (player == null || mode == null || rig == null)
        {
            Debug.LogError("[WolfPlayModeTest] FAIL — missing player, mode, or camera rig.");
            return false;
        }

        Debug.Log("[WolfPlayModeTest] PASS — beat-em-up scene running with player, mode, and camera rig.");
        return true;
    }
}
#endif
