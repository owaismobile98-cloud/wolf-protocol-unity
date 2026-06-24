using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { SideScroll2_5D, TopDown, Follow, Fixed }

        public Mode CurrentMode = Mode.SideScroll2_5D;
        public Transform Target;
        public Vector3 BaseOffset = new Vector3(0f, 0f, -10f);
        public float SmoothTime = 0.12f;
        public float FixedOrthographicSize = 200f;
        public float SideScrollLockX;
        public bool LockSideScrollX = true;
        public float AimLeadStrength = 48f;

        Camera _cam;
        Vector3 _velocity;
        Vector2 _shake;
        Vector2 _aimLead;

        public Camera Camera => _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = FixedOrthographicSize;
            _cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
                gameObject.tag = "MainCamera";
        }

        public void BindTarget(Transform target)
        {
            Target = target;
            if (CurrentMode == Mode.Fixed && target != null)
                transform.position = target.position + BaseOffset;
        }

        public void AddShake(float amount)
        {
            _shake.x = Mathf.Max(_shake.x, amount);
            _shake.y = Mathf.Max(_shake.y, amount);
        }

        public void SetAimLead(Vector2 lead)
        {
            _aimLead = lead;
        }

        void LateUpdate()
        {
            _cam.orthographicSize = FixedOrthographicSize;

            if (Target == null && CurrentMode != Mode.Fixed)
            {
                DecayShake();
                return;
            }

            var lead = Vector2.zero;
            if (CurrentMode is Mode.SideScroll2_5D or Mode.Follow)
                lead = _aimLead;

            var jitter = Vector2.zero;
            if (_shake.sqrMagnitude > 0.0001f)
            {
                jitter = new Vector2(
                    Random.Range(-_shake.x, _shake.x),
                    Random.Range(-_shake.y, _shake.y));
                _shake = Vector2.MoveTowards(_shake, Vector2.zero, 40f * Time.deltaTime);
            }

            var desired = ComputeDesiredPosition(lead);
            var smoothed = Vector3.SmoothDamp(transform.position, desired + (Vector3)jitter, ref _velocity, SmoothTime);
            transform.position = smoothed;
        }

        Vector3 ComputeDesiredPosition(Vector2 lead)
        {
            switch (CurrentMode)
            {
                case Mode.SideScroll2_5D:
                {
                    var p = Target.position + BaseOffset;
                    if (LockSideScrollX) p.x = SideScrollLockX;
                    p += (Vector3)lead;
                    return p;
                }
                case Mode.TopDown:
                    return Target.position + BaseOffset;
                case Mode.Follow:
                    return Target.position + BaseOffset + (Vector3)lead;
                case Mode.Fixed:
                    return BaseOffset;
                default:
                    return transform.position;
            }
        }

        void DecayShake()
        {
            if (_shake.sqrMagnitude <= 0.0001f) return;
            _shake = Vector2.MoveTowards(_shake, Vector2.zero, 40f * Time.deltaTime);
        }
    }
}
