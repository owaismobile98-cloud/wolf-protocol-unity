using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Shooter movement skeleton: WASD move + mouse aim. Extend for twin-stick / cover shooter modes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShooterMotor : MonoBehaviour
    {
        public float MoveSpeed = 200f;

        public Vector2 AimDir { get; private set; } = Vector2.right;
        public int Facing => AimDir.x >= 0 ? 1 : -1;

        Rigidbody2D _rb;
        Camera _cam;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _cam = Camera.main;
        }

        void Update()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            var move = new Vector2(x, y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            _rb.linearVelocity = move * MoveSpeed;

            if (_cam != null)
            {
                var mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                var toMouse = (Vector2)mouseWorld - (Vector2)transform.position;
                if (toMouse.sqrMagnitude > 1f) AimDir = toMouse.normalized;
            }
        }
    }
}
