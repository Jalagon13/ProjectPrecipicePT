using UnityEngine;

namespace ProjectPrecipicePT
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerLocomotionState))]
    [RequireComponent(typeof(PlayerClimbingState))]
    [RequireComponent(typeof(PlayerLedgeClimbState))]
    public class Player : MonoBehaviour
    {
        public enum PlayerStateType
        {
            Locomotion,
            Climbing,
            LedgeClimb
        }

        private CharacterController _characterController;
        private Transform _cameraYawPivot;
        private Transform _cameraTransform;
        private PlayerLocomotionState _locomotionState;
        private PlayerClimbingState _climbingState;
        private PlayerLedgeClimbState _ledgeClimbState;
        private PlayerStateType _state;
        private float _pitch;
        private float _climbCameraYaw;

        public CharacterController CharacterController => _characterController;
        public Transform PlayerTransform => transform;
        public Transform ModelTransform => transform.GetChild(1);
        public Transform CameraTransform => _cameraTransform;
        public PlayerStateType State => _state;
        public PlayerLocomotionState LocomotionState => _locomotionState;
        public PlayerClimbingState ClimbingState => _climbingState;
        public PlayerLedgeClimbState LedgeClimbState => _ledgeClimbState;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _locomotionState = GetComponent<PlayerLocomotionState>();
            _climbingState = GetComponent<PlayerClimbingState>();
            _ledgeClimbState = GetComponent<PlayerLedgeClimbState>();

            SetupCameraHierarchy();

            if (_cameraTransform != null)
            {
                _pitch = NormalizePitch(_cameraTransform.localEulerAngles.x);
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            if (GameInput.Instance == null)
            {
                return;
            }

            if (_state != PlayerStateType.LedgeClimb)
            {
                HandleLook();
            }

            switch (_state)
            {
                case PlayerStateType.Locomotion:
                    if (!_climbingState.TryEnterFromInput())
                    {
                        _locomotionState.Tick();
                    }
                    break;
                case PlayerStateType.Climbing:
                    _climbingState.Tick();
                    break;
                case PlayerStateType.LedgeClimb:
                    _ledgeClimbState.Tick();
                    break;
            }
        }

        public void SetState(PlayerStateType state)
        {
            _state = state;
        }

        public void ResetClimbCamera()
        {
            _climbCameraYaw = 0f;

            if (_cameraYawPivot != null)
            {
                _cameraYawPivot.localRotation = Quaternion.identity;
            }
        }

        public void AlignRootToCameraYaw()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            Vector3 desiredForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up);
            if (desiredForward.sqrMagnitude <= 0.0001f)
            {
                desiredForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            if (desiredForward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
            }
        }

        public Vector3 GetBodyCenter(Vector3 rootPosition)
        {
            return rootPosition + _characterController.center;
        }

        public float GetStandingRootOffset()
        {
            return (_characterController.height * 0.5f) - _characterController.center.y + _characterController.skinWidth;
        }

        public bool HasCharacterClearance(Vector3 rootPosition)
        {
            float castRadius = Mathf.Max(0.05f, _characterController.radius - (_characterController.skinWidth * 0.5f));
            float halfHeight = Mathf.Max(castRadius, (_characterController.height * 0.5f) - castRadius);
            Vector3 center = GetBodyCenter(rootPosition);
            Vector3 top = center + (Vector3.up * halfHeight);
            Vector3 bottom = center - (Vector3.up * halfHeight);

            return !Physics.CheckCapsule(top, bottom, castRadius, ~0, QueryTriggerInteraction.Ignore);
        }

        private void SetupCameraHierarchy()
        {
            _cameraTransform = GetComponentInChildren<Camera>()?.transform;

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            if (_cameraTransform == null)
            {
                return;
            }

            _cameraYawPivot = transform.Find("CameraYawPivot");
            if (_cameraYawPivot == null)
            {
                GameObject pivotObject = new GameObject("CameraYawPivot");
                _cameraYawPivot = pivotObject.transform;
                _cameraYawPivot.SetParent(transform, false);
                _cameraYawPivot.localPosition = Vector3.zero;
                _cameraYawPivot.localRotation = Quaternion.identity;
            }

            if (_cameraTransform.parent != _cameraYawPivot)
            {
                _cameraTransform.SetParent(_cameraYawPivot, true);
            }
        }

        private void HandleLook()
        {
            if (_cameraTransform == null || _cameraYawPivot == null)
            {
                return;
            }

            Vector2 lookInput = GameInput.Instance.GetLookVector();

            if (_state == PlayerStateType.Climbing)
            {
                _climbCameraYaw = Mathf.Clamp(
                    _climbCameraYaw + (lookInput.x * _locomotionState.LookSensitivityX),
                    -_climbingState.ClimbCameraYawLimit,
                    _climbingState.ClimbCameraYawLimit);

                _pitch = Mathf.Clamp(
                    _pitch - (lookInput.y * _locomotionState.LookSensitivityY),
                    -_climbingState.ClimbCameraPitchUpLimit,
                    _climbingState.ClimbCameraPitchDownLimit);

                _cameraYawPivot.localRotation = Quaternion.Euler(0f, _climbCameraYaw, 0f);
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                return;
            }

            transform.Rotate(Vector3.up, lookInput.x * _locomotionState.LookSensitivityX);
            ResetClimbCamera();
            _pitch = Mathf.Clamp(_pitch - (lookInput.y * _locomotionState.LookSensitivityY), -_locomotionState.MaxLookPitch, _locomotionState.MaxLookPitch);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}
