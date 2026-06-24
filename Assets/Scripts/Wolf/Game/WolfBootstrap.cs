using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wolf.Protocol
{
    public static class WolfBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            var go = new GameObject("WolfGame");
            go.AddComponent<GameManager>();
        }
    }
}
