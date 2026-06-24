#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wolf.Protocol;

public static class WolfSceneBuilder
{
    const string ScenePath = "Assets/Scenes/BeatEmUpTest.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/razor.prefab";

    [MenuItem("Window/WOLF/Build Beat-Em-Up Test Scene")]
    public static void BuildBeatEmUpTestScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("BeatEmUpTest");
        var setup = root.AddComponent<WolfSceneSetup>();
        var mode = root.AddComponent<BeatEmUpMode>();
        setup.GameMode = mode;
        setup.ArenaCenter = new Vector2(320f, 200f);
        setup.ArenaSize = new Vector2(80f, 45f);
        setup.CameraMode = CameraRig.Mode.SideScroll2_5D;
        setup.CameraOrthographicSize = 200f;
        setup.InitialEnemyCount = 0;

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
            setup.PlayerPrefab = playerPrefab;
        else
            Debug.LogWarning($"Player prefab not found at {PlayerPrefabPath}; scene will use placeholder player.");

        if (playerPrefab != null)
            setup.EnemyPrefab = playerPrefab;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built test scene at {ScenePath}");
    }

    [MenuItem("Window/WOLF/Build Beat-Em-Up Test Scene", true)]
    static bool BuildBeatEmUpTestSceneValidate() => !Application.isPlaying;

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
                return;
        }

        var updated = new EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
            updated[i] = scenes[i];
        updated[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    public static void BuildBeatEmUpTestSceneBatch()
    {
        BuildBeatEmUpTestScene();
        EditorApplication.Exit(0);
    }
}
#endif
