using UnityEngine;

namespace Wolf.Protocol
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameMode ActiveMode { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            EnsureServices();
        }

        public void RegisterMode(GameMode mode)
        {
            ActiveMode = mode;
        }

        void EnsureServices()
        {
            if (Feel.Instance == null) new GameObject("Feel").AddComponent<Feel>();
            if (ScoreSystem.Instance == null) new GameObject("Score").AddComponent<ScoreSystem>();
            if (SfxManager.Instance == null) new GameObject("Sfx").AddComponent<SfxManager>();
            if (Director.Instance == null) new GameObject("Director").AddComponent<Director>();
            ScoreSystem.Instance.EnterGame();
        }

        public void OnPlayerDied() => ActiveMode?.OnPlayerDied();
        public void OnEnemyKilled() => ActiveMode?.OnEnemyKilled();
        public void OnBossKilled() => ActiveMode?.OnBossKilled();
    }
}
