using UnityEngine;

namespace Wolf.Protocol
{
    public static class WolfBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            if (Object.FindAnyObjectByType<WolfSceneSetup>() != null) return;
            if (Object.FindAnyObjectByType<GameMode>() != null) return;

            var go = new GameObject("WolfGame");
            go.AddComponent<GameManager>();
            go.AddComponent<WolfLegacyBootstrap>();
        }
    }
}
