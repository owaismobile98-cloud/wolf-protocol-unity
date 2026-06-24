#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Wolf.Protocol;

public static class WolfPlayModeTest
{
    [MenuItem("Window/WOLF/Run Play Mode Smoke Test")]
    public static void RunSmokeTest()
    {
        EditorApplication.EnterPlaymode();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.delayCall += Verify;
    }

    static void Verify()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        var mode = Object.FindAnyObjectByType<BeatEmUpMode>();
        var rig = Object.FindAnyObjectByType<CameraRig>();

        if (player == null || mode == null || rig == null)
        {
            Debug.LogError("[WolfPlayModeTest] FAIL — missing player, mode, or camera rig.");
            EditorApplication.ExitPlaymode();
            return;
        }

        Debug.Log("[WolfPlayModeTest] PASS — beat-em-up scene running with player, mode, and camera rig.");
        EditorApplication.ExitPlaymode();
    }
}
#endif
