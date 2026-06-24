using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Genre-agnostic game mode base. Subclasses wire mechanics and bootstrap gameplay.
    /// </summary>
    public abstract class GameMode : MonoBehaviour
    {
        protected PlayerController Player { get; private set; }
        protected CameraRig CameraRig { get; private set; }

        protected virtual void Awake()
        {
            EnsureServices();
        }

        protected virtual void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterMode(this);
            OnModeStarted();
        }

        public void BindPlayer(PlayerController player, CameraRig rig)
        {
            Player = player;
            CameraRig = rig;
            if (rig != null && player != null)
                rig.BindTarget(player.transform);
            OnPlayerBound();
        }

        protected static void EnsureServices()
        {
            if (Feel.Instance == null) new GameObject("Feel").AddComponent<Feel>();
            if (ScoreSystem.Instance == null) new GameObject("Score").AddComponent<ScoreSystem>();
            if (SfxManager.Instance == null) new GameObject("Sfx").AddComponent<SfxManager>();
            if (Director.Instance == null) new GameObject("Director").AddComponent<Director>();
            if (GameManager.Instance == null) new GameObject("WolfGame").AddComponent<GameManager>();
            ScoreSystem.Instance.EnterGame();
        }

        protected virtual void OnPlayerBound() { }
        protected virtual void OnModeStarted() { }

        public virtual void OnPlayerDied() { }
        public virtual void OnEnemyKilled() { }
        public virtual void OnBossKilled() { }
    }
}
