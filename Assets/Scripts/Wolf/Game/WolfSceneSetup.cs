using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Scene component that builds a Wolf arena at runtime and binds the active game mode.
    /// </summary>
    public class WolfSceneSetup : MonoBehaviour
    {
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public int InitialEnemyCount;
        public Vector2 ArenaCenter = new Vector2(320f, 200f);
        public Vector2 ArenaSize = new Vector2(80f, 45f);
        public CameraRig.Mode CameraMode = CameraRig.Mode.SideScroll2_5D;
        public float CameraOrthographicSize = 200f;
        public GameMode GameMode;

        WolfScene.Result _built;

        void Awake()
        {
            if (GameMode == null)
                GameMode = GetComponent<GameMode>();

            _built = WolfScene.Build(new WolfScene.Config
            {
                ArenaCenter = ArenaCenter,
                ArenaSize = ArenaSize,
                PlayerPrefab = PlayerPrefab,
                EnemyPrefab = EnemyPrefab,
                EnemyCount = InitialEnemyCount,
                CameraMode = CameraMode,
                CameraOrthographicSize = CameraOrthographicSize,
                Parent = transform,
            });

            if (GameMode != null)
                GameMode.BindPlayer(_built.Player, _built.CameraRig);
        }

        public WolfScene.Result Built => _built;
    }
}
