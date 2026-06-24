using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Legacy no-scene bootstrap: builds arena, player, camera, and beat-em-up mode at runtime.
    /// </summary>
    public class WolfLegacyBootstrap : MonoBehaviour
    {
        void Start()
        {
            var built = WolfScene.Build(new WolfScene.Config
            {
                ArenaCenter = new Vector2(320f, 200f),
                ArenaSize = new Vector2(80f, 45f),
                EnemyCount = 0,
                CameraMode = CameraRig.Mode.SideScroll2_5D,
                CameraOrthographicSize = 200f,
                Parent = transform,
            });

            var mode = gameObject.AddComponent<BeatEmUpMode>();
            mode.BindPlayer(built.Player, built.CameraRig);
        }
    }
}
