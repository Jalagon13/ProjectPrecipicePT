using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerLedgeClimbState : MonoBehaviour
    {
        [Header("Ledge Climb")]
        [SerializeField] private float _ledgeProbeUpOffset = 0.9f;
        [SerializeField] private float _ledgeProbeForwardOffset = 0.55f;
        [SerializeField] private float _ledgeDownProbeDistance = 1.8f;
        [SerializeField] private float _ledgeStandForwardOffset = 0.45f;
        [SerializeField] private float _ledgeStandHeightOffset = 0.02f;
        [SerializeField] private float _ledgeClimbDuration = 0.2f;

        private Player _player;
        private PlayerLocomotionState _locomotionState;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _timer;

        public float LedgeProbeUpOffset => _ledgeProbeUpOffset;
        public float LedgeProbeForwardOffset => _ledgeProbeForwardOffset;
        public float LedgeDownProbeDistance => _ledgeDownProbeDistance;
        public float LedgeStandForwardOffset => _ledgeStandForwardOffset;
        public float LedgeStandHeightOffset => _ledgeStandHeightOffset;
        public float LedgeClimbDuration => _ledgeClimbDuration;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _locomotionState = GetComponent<PlayerLocomotionState>();
        }

        public void Begin(Vector3 targetPosition)
        {
            _startPosition = _player.PlayerTransform.position;
            _targetPosition = targetPosition;
            _timer = 0f;
            _player.SetState(Player.PlayerStateType.LedgeClimb);
        }

        public void Tick()
        {
            _timer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, _ledgeClimbDuration);
            float t = Mathf.Clamp01(_timer / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            _player.PlayerTransform.position = Vector3.Lerp(_startPosition, _targetPosition, easedT);

            if (t >= 1f)
            {
                _player.PlayerTransform.position = _targetPosition;
                _player.AlignRootToCameraYaw();
                _player.ResetClimbCamera();
                _locomotionState.ForceGroundedRecovery();
                _player.SetState(Player.PlayerStateType.Locomotion);
            }
        }
    }
}
