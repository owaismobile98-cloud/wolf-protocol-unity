using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Platformer genre stub: side-scroll camera + PlatformerMotor jump/move skeleton.
    /// </summary>
    public class PlatformerMode : GameMode
    {
        PlatformerMotor _motor;

        protected override void OnPlayerBound()
        {
            if (Player == null) return;
            _motor = Player.GetComponent<PlatformerMotor>();
            if (_motor == null) _motor = Player.gameObject.AddComponent<PlatformerMotor>();
            if (CameraRig != null)
            {
                CameraRig.CurrentMode = CameraRig.Mode.Follow;
                CameraRig.LockSideScrollX = false;
            }
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(16, 10, 480, 24), "PLATFORMER MODE (stub) — A/D move, Space jump", style);
        }
    }
}
