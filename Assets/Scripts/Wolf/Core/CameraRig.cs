using Unity.Cinemachine;
using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Mode-switching camera rig backed by Cinemachine virtual cameras.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(1000)]
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

        const int ActivePriority = 10;
        const int InactivePriority = 0;

        Camera _cam;
        CinemachineBrain _brain;
        Transform _rigRoot;
        CinemachineCamera _vcamSide;
        CinemachineCamera _vcamTop;
        CinemachineCamera _vcamFollow;
        CinemachineCamera _vcamFixed;
        CinemachineFollow _followSide;
        CinemachineFollow _followTop;
        CinemachineFollow _followFollow;
        CinemachineBasicMultiChannelPerlin _noiseSide;
        CinemachineBasicMultiChannelPerlin _noiseTop;
        CinemachineBasicMultiChannelPerlin _noiseFollow;
        Vector2 _aimLead;
        float _shake;

        public Camera Camera => _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = FixedOrthographicSize;
            _cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
                gameObject.tag = "MainCamera";

            _brain = GetComponent<CinemachineBrain>() ?? gameObject.AddComponent<CinemachineBrain>();
            BuildVirtualCameras();
            ApplyMode();
        }

        void BuildVirtualCameras()
        {
            _rigRoot = new GameObject("CM_Rig").transform;
            _rigRoot.SetParent(transform);

            _vcamSide = CreateFollowVcam("CM_SideScroll2_5D", out _followSide, out _noiseSide);
            _vcamTop = CreateFollowVcam("CM_TopDown", out _followTop, out _noiseTop);
            _vcamFollow = CreateFollowVcam("CM_Follow", out _followFollow, out _noiseFollow);

            var fixedGo = new GameObject("CM_Fixed");
            fixedGo.transform.SetParent(_rigRoot);
            _vcamFixed = fixedGo.AddComponent<CinemachineCamera>();
            ConfigureLens(_vcamFixed);
            _vcamFixed.transform.position = BaseOffset;
        }

        CinemachineCamera CreateFollowVcam(
            string name,
            out CinemachineFollow follow,
            out CinemachineBasicMultiChannelPerlin noise)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rigRoot);
            var vcam = go.AddComponent<CinemachineCamera>();
            ConfigureLens(vcam);
            follow = go.AddComponent<CinemachineFollow>();
            follow.TrackerSettings.PositionDamping = Vector3.one * Mathf.Max(SmoothTime, 0.01f);
            follow.FollowOffset = BaseOffset;
            noise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
            noise.AmplitudeGain = 0f;
            return vcam;
        }

        void ConfigureLens(CinemachineCamera vcam)
        {
            var lens = vcam.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = FixedOrthographicSize;
            vcam.Lens = lens;
        }

        public void BindTarget(Transform target)
        {
            Target = target;
            _vcamSide.Follow = target;
            _vcamTop.Follow = target;
            _vcamFollow.Follow = target;
            ApplyFollowOffsets();
            if (CurrentMode == Mode.Fixed && target != null)
                _vcamFixed.transform.position = target.position + BaseOffset;
        }

        public void SetMode(Mode mode)
        {
            CurrentMode = mode;
            ApplyMode();
        }

        public void AddShake(float amount) => _shake = Mathf.Max(_shake, amount);

        public void SetAimLead(Vector2 lead)
        {
            _aimLead = lead;
            ApplyFollowOffsets();
        }

        void ApplyMode()
        {
            if (_brain == null) return;

            SetVcamPriority(_vcamSide, CurrentMode == Mode.SideScroll2_5D);
            SetVcamPriority(_vcamTop, CurrentMode == Mode.TopDown);
            SetVcamPriority(_vcamFollow, CurrentMode == Mode.Follow);
            SetVcamPriority(_vcamFixed, CurrentMode == Mode.Fixed);

            if (CurrentMode == Mode.Fixed)
                _vcamFixed.transform.position = BaseOffset;

            ApplyFollowOffsets();
            SyncLensSizes();
        }

        static void SetVcamPriority(CinemachineCamera vcam, bool active)
        {
            vcam.Priority = active ? ActivePriority : InactivePriority;
        }

        void ApplyFollowOffsets()
        {
            if (_followTop != null)
                _followTop.FollowOffset = BaseOffset;

            var lead = Vector3.zero;
            if (CurrentMode is Mode.SideScroll2_5D or Mode.Follow)
                lead = (Vector3)_aimLead;

            if (_followSide != null)
                _followSide.FollowOffset = BaseOffset + lead;
            if (_followFollow != null)
                _followFollow.FollowOffset = BaseOffset + lead;
        }

        void SyncLensSizes()
        {
            _cam.orthographicSize = FixedOrthographicSize;
            ConfigureLens(_vcamSide);
            ConfigureLens(_vcamTop);
            ConfigureLens(_vcamFollow);
            ConfigureLens(_vcamFixed);
        }

        void LateUpdate()
        {
            UpdateShake();

            if (CurrentMode == Mode.SideScroll2_5D && LockSideScrollX)
            {
                var p = transform.position;
                p.x = SideScrollLockX;
                transform.position = p;
            }
        }

        void UpdateShake()
        {
            var amp = _shake;
            if (_shake > 0f)
                _shake = Mathf.MoveTowards(_shake, 0f, 40f * Time.deltaTime);

            if (_noiseSide != null) _noiseSide.AmplitudeGain = CurrentMode == Mode.SideScroll2_5D ? amp : 0f;
            if (_noiseTop != null) _noiseTop.AmplitudeGain = CurrentMode == Mode.TopDown ? amp : 0f;
            if (_noiseFollow != null) _noiseFollow.AmplitudeGain = CurrentMode == Mode.Follow ? amp : 0f;
        }
    }
}
