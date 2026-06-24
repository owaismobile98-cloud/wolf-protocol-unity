using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Platformer movement skeleton: horizontal run + jump. Extend for full platformer mode.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlatformerMotor : MonoBehaviour
    {
        public float MoveSpeed = 180f;
        public float JumpForce = 320f;
        public LayerMask GroundMask = ~0;

        Rigidbody2D _rb;
        bool _grounded;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 2.5f;
            _rb.freezeRotation = true;
        }

        void Update()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            _rb.linearVelocity = new Vector2(x * MoveSpeed, _rb.linearVelocity.y);

            _grounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, GroundMask);
            if (_grounded && Input.GetKeyDown(KeyCode.Space))
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, JumpForce);
        }
    }
}
