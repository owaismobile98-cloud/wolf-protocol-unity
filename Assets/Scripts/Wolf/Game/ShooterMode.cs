using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Shooter genre stub: top-down camera + ShooterMotor aim/move skeleton.
    /// </summary>
    public class ShooterMode : GameMode
    {
        ShooterMotor _motor;

        protected override void OnPlayerBound()
        {
            if (Player == null) return;
            _motor = Player.GetComponent<ShooterMotor>();
            if (_motor == null) _motor = Player.gameObject.AddComponent<ShooterMotor>();
            if (CameraRig != null)
                CameraRig.CurrentMode = CameraRig.Mode.TopDown;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(16, 10, 400, 24), "SHOOTER MODE (stub) — WASD move, mouse aim", style);
            if (_motor != null)
                GUI.Label(new Rect(16, 30, 400, 24), $"Aim: ({_motor.AimDir.x:F2}, {_motor.AimDir.y:F2})", style);
        }
    }
}
