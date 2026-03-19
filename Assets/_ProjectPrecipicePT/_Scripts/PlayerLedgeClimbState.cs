using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerLedgeClimbState : MonoBehaviour
    {
        [Header("Ledge Climb")]
        [SerializeField, Tooltip("How far above the player the ledge probe starts when searching for a standable top surface.")]
        private float _ledgeProbeUpOffset = 0.9f;
        [SerializeField, Tooltip("How far forward over the ledge the top-surface probe starts.")]
        private float _ledgeProbeForwardOffset = 0.55f;
        [SerializeField, Tooltip("Maximum downward distance used to find a valid standable ledge top.")]
        private float _ledgeDownProbeDistance = 1.8f;
        [SerializeField, Tooltip("How far back from the ledge edge the player is placed after the climb finishes.")]
        private float _ledgeStandForwardOffset = 0.45f;
        [SerializeField, Tooltip("Extra upward offset applied when placing the player on the ledge.")]
        private float _ledgeStandHeightOffset = 0.02f;
        [SerializeField, Tooltip("Total duration of the ledge climb sequence.")]
        private float _ledgeClimbDuration = 0.5f;
        [SerializeField, Tooltip("How far above the player to check for more climbable wall before allowing a ledge climb.")]
        private float _ledgeContinuationCheckUpOffset = 0.65f;
        [SerializeField, Tooltip("How far forward from the climbed surface to check for continued climbable wall.")]
        private float _ledgeContinuationCheckForwardOffset = 0.1f;
        [SerializeField, Tooltip("Maximum distance used to check whether the climb should continue instead of mantling.")]
        private float _ledgeContinuationCheckDistance = 0.9f;

        private Player _player;
        private PlayerLocomotionState _locomotionState;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;
        private float _timer;

        public float LedgeProbeUpOffset => _ledgeProbeUpOffset;
        public float LedgeProbeForwardOffset => _ledgeProbeForwardOffset;
        public float LedgeDownProbeDistance => _ledgeDownProbeDistance;
        public float LedgeStandForwardOffset => _ledgeStandForwardOffset;
        public float LedgeStandHeightOffset => _ledgeStandHeightOffset;
        public float LedgeClimbDuration => _ledgeClimbDuration;
        public float LedgeContinuationCheckUpOffset => _ledgeContinuationCheckUpOffset;
        public float LedgeContinuationCheckForwardOffset => _ledgeContinuationCheckForwardOffset;
        public float LedgeContinuationCheckDistance => _ledgeContinuationCheckDistance;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _locomotionState = GetComponent<PlayerLocomotionState>();
        }

        public void Begin(Vector3 targetPosition)
        {
            _startPosition = _player.PlayerTransform.position;
            _targetPosition = targetPosition;
            _startRotation = _player.PlayerTransform.rotation;
            _targetRotation = GetTargetUprightRotation();
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

            if (t < 0.5f)
            {
                _player.PlayerTransform.rotation = _startRotation;
            }
            else
            {
                float rotationT = Mathf.InverseLerp(0.5f, 1f, t);
                float easedRotationT = Mathf.SmoothStep(0f, 1f, rotationT);
                _player.PlayerTransform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, easedRotationT);
            }

            if (t >= 1f)
            {
                _player.PlayerTransform.position = _targetPosition;
                _player.PlayerTransform.rotation = _targetRotation;
                _player.ResetClimbCamera();
                _locomotionState.ForceGroundedRecovery();
                _player.SetState(Player.PlayerStateType.Locomotion);
            }
        }

        private Quaternion GetTargetUprightRotation()
        {
            Vector3 desiredForward = Vector3.forward;

            if (_player.CameraTransform != null)
            {
                desiredForward = Vector3.ProjectOnPlane(_player.CameraTransform.forward, Vector3.up);
            }

            if (desiredForward.sqrMagnitude <= 0.0001f)
            {
                desiredForward = Vector3.ProjectOnPlane(_player.PlayerTransform.forward, Vector3.up);
            }

            if (desiredForward.sqrMagnitude <= 0.0001f)
            {
                desiredForward = Vector3.forward;
            }

            return Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
        }
    }
}
