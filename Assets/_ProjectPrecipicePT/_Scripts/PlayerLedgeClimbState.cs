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
        [SerializeField, Tooltip("How far above the player to check for more climbable wall before allowing a ledge climb.")]
        private float _ledgeContinuationCheckUpOffset = 0.65f;
        [SerializeField, Tooltip("How far forward from the climbed surface to check for continued climbable wall.")]
        private float _ledgeContinuationCheckForwardOffset = 0.1f;
        [SerializeField, Tooltip("Maximum distance used to check whether the climb should continue instead of mantling.")]
        private float _ledgeContinuationCheckDistance = 0.9f;

        [Header("Ledge Climb Sequence Settings")]
        [SerializeField, Tooltip("Total duration of the ledge climb sequence.")]
        private float _ledgeClimbDuration = 0.5f;
        [SerializeField, Tooltip("Additional downward look angle applied during the middle of the ledge climb sequence.")]
        private float _ledgeCameraLookDownAngle = 35f;
        [SerializeField, Tooltip("Normalized time when the camera should finish aligning toward the model-facing direction.")]
        private float _ledgeCameraAlignDuration = 0.2f;
        [SerializeField, Tooltip("Normalized time when the camera should start lifting back up toward a level view.")]
        private float _ledgeCameraLookUpStart = 0.65f;

        private Player _player;
        private PlayerLocomotionState _locomotionState;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;
        private Quaternion _startModelRotation;
        private Quaternion _targetModelRotation;
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
            _startModelRotation = _player.ModelTransform != null ? _player.ModelTransform.localRotation : Quaternion.identity;
            _targetModelRotation = Quaternion.identity;
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
            UpdateCameraSequence(t);

            if (t < 0.5f)
            {
                _player.PlayerTransform.rotation = _startRotation;
                ApplyModelRotation(_startModelRotation);
            }
            else
            {
                float rotationT = Mathf.InverseLerp(0.5f, 1f, t);
                float easedRotationT = Mathf.SmoothStep(0f, 1f, rotationT);
                _player.PlayerTransform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, easedRotationT);
                ApplyModelRotation(Quaternion.Slerp(_startModelRotation, _targetModelRotation, easedRotationT));
            }

            if (t >= 1f)
            {
                _player.PlayerTransform.position = _targetPosition;
                ApplyModelRotation(_targetModelRotation);
                _player.AlignRootToCameraYaw();
                _player.ResetClimbCamera();
                _locomotionState.ForceGroundedRecovery();
                _player.SetState(Player.PlayerStateType.Locomotion);
            }
        }

        private void UpdateCameraSequence(float normalizedTime)
        {
            if (_player.ModelTransform == null)
            {
                return;
            }

            Vector3 horizontalForward = Vector3.ProjectOnPlane(_player.ModelTransform.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude <= 0.0001f)
            {
                horizontalForward = Vector3.ProjectOnPlane(_player.PlayerTransform.forward, Vector3.up);
            }

            if (horizontalForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            horizontalForward.Normalize();

            float alignDuration = Mathf.Clamp(_ledgeCameraAlignDuration, 0.01f, 0.45f);
            float lookUpStart = Mathf.Clamp(_ledgeCameraLookUpStart, alignDuration + 0.05f, 0.95f);

            float lookDownBlend;
            if (normalizedTime <= alignDuration)
            {
                lookDownBlend = 0f;
            }
            else if (normalizedTime < lookUpStart)
            {
                lookDownBlend = Mathf.InverseLerp(alignDuration, lookUpStart, normalizedTime);
            }
            else
            {
                lookDownBlend = 1f - Mathf.InverseLerp(lookUpStart, 1f, normalizedTime);
            }

            lookDownBlend = Mathf.SmoothStep(0f, 1f, lookDownBlend);

            Vector3 rightAxis = Vector3.Cross(Vector3.up, horizontalForward).normalized;
            if (rightAxis.sqrMagnitude <= 0.0001f)
            {
                rightAxis = _player.PlayerTransform.right;
            }

            Quaternion lookOffset = Quaternion.AngleAxis(_ledgeCameraLookDownAngle * lookDownBlend, rightAxis);
            Vector3 targetLookDirection = lookOffset * horizontalForward;
            float cameraLerpT = 1f - Mathf.Exp(-10f * Time.deltaTime);
            _player.LerpCameraTowardWorldDirection(targetLookDirection, cameraLerpT);
        }

        private void ApplyModelRotation(Quaternion rotation)
        {
            if (_player.ModelTransform == null)
            {
                return;
            }

            _player.ModelTransform.localRotation = rotation;
        }

        private Quaternion GetTargetUprightRotation()
        {
            Vector3 desiredForward = Vector3.zero;

            if (_player.ModelTransform != null)
            {
                desiredForward = Vector3.ProjectOnPlane(_player.ModelTransform.forward, Vector3.up);
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
