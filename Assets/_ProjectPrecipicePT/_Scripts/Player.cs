using UnityEngine;

namespace ProjectPrecipicePT
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerLocomotionState))]
    [RequireComponent(typeof(PlayerClimbingState))]
    [RequireComponent(typeof(PlayerClimbJumpState))]
    [RequireComponent(typeof(PlayerLedgeClimbState))]
    [RequireComponent(typeof(PlayerDeathState))]
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
    
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        public enum PlayerStateType
        {
            Locomotion,
            Climbing,
            ClimbJump,
            LedgeClimb,
            Dead
        }

        [SerializeField] private Transform _cameraYawPivot;
        [SerializeField] private Canvas _playerCanvas;
        
        private CharacterController _characterController;
        private Transform _cameraTransform;
        private Transform _modelTransform;
        private PlayerLocomotionState _locomotionState;
        private PlayerClimbingState _climbingState;
        private PlayerClimbJumpState _climbJumpState;
        private PlayerLedgeClimbState _ledgeClimbState;
        private PlayerDeathState _deathState;
        private PlayerStateType _state;
        private float _pitch;
        private float _climbCameraYaw;
        private bool _isModelUprightBlendActive;
        private Quaternion _modelBlendStartRotation;
        private float _modelBlendTimer;
        private float _modelBlendDuration;
        private Vector3 _respawnPoint;

        public CharacterController CharacterController => _characterController;
        public Transform PlayerTransform => transform;
        public Transform ModelTransform => _modelTransform;
        public Transform CameraTransform => _cameraTransform;
        public PlayerStateType State => _state;
        public PlayerLocomotionState LocomotionState => _locomotionState;
        public PlayerClimbingState ClimbingState => _climbingState;
        public PlayerClimbJumpState ClimbJumpState => _climbJumpState;
        public PlayerLedgeClimbState LedgeClimbState => _ledgeClimbState;
        public PlayerDeathState DeathState => _deathState;

        private void Awake()
        {
            Instance = this;
            _playerCanvas.gameObject.SetActive(true);

            CacheReferences();
            SetupCameraHierarchy();
            InitializePitchFromCamera();

            _respawnPoint = transform.position;
        }

        private void Start()
        {
            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.OnDeath += Die;
                HealthManager.Instance.OnRespawn += HandleRespawn;
            }
        }

        private void OnDestroy()
        {
            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.OnDeath -= Die;
                HealthManager.Instance.OnRespawn -= HandleRespawn;
            }
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (GameInput.Instance == null)
            {
                return;
            }

            if (_state != PlayerStateType.Dead)
            {
                if (!InventoryManager.Instance.IsInventoryOpen)
                {
                    HandleLookIfAllowed();
                }
            }
            
            TickModelUprightBlend();
            TickCurrentState();
        }

        public void SetState(PlayerStateType state)
        {
            _state = state;
        }

        public void Die()
        {
            SetState(PlayerStateType.Dead);
            SetCursorLocked(false);
        }

        private void HandleRespawn()
        {
            _characterController.enabled = false;
            transform.position = _respawnPoint;
            _characterController.enabled = true;
            
            SetState(PlayerStateType.Locomotion);
            SetCursorLocked(true);
        }

        public void ResetClimbCamera()
        {
            _climbCameraYaw = 0f;

            if (_cameraYawPivot != null)
            {
                _cameraYawPivot.localRotation = Quaternion.identity;
            }
        }

        // Used by ledge climb to steer the player's view through the mantle sequence.
        public void LerpCameraTowardWorldDirection(Vector3 worldDirection, float t)
        {
            if (_cameraTransform == null || _cameraYawPivot == null || worldDirection.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                return;
            }

            Vector3 localDirection = Quaternion.Inverse(transform.rotation) * worldDirection.normalized;
            Quaternion targetLocalRotation = Quaternion.LookRotation(localDirection, Vector3.up);
            Vector3 targetEuler = targetLocalRotation.eulerAngles;
            float targetYaw = NormalizeAngle(targetEuler.y);
            float targetPitch = NormalizeAngle(targetEuler.x);

            _climbCameraYaw = Mathf.LerpAngle(_climbCameraYaw, targetYaw, t);
            _pitch = Mathf.LerpAngle(_pitch, targetPitch, t);
            ApplyCameraLocalRotation();
        }

        public void AlignRootToCameraYaw()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            RotateRootToHorizontalDirection(_cameraTransform.forward);
        }

        public void SnapDetachFacingToCurrentLook()
        {
            if (_cameraYawPivot != null)
            {
                RotateRootToHorizontalDirection(_cameraYawPivot.forward);
            }
            else if (_cameraTransform != null)
            {
                RotateRootToHorizontalDirection(_cameraTransform.forward);
            }

            ResetClimbCamera();
            ApplyCameraLocalRotation();
        }

        public void ApplyClimbFacing(Vector3 surfaceNormal, float rotationLerpSpeed, bool instant)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(-surfaceNormal, Vector3.up);
            if (flatForward.sqrMagnitude > MinimumDirectionSqrMagnitude)
            {
                Quaternion targetRootRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
                transform.rotation = instant
                    ? targetRootRotation
                    : Quaternion.Slerp(transform.rotation, targetRootRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
            }

            if (_modelTransform == null)
            {
                return;
            }

            Quaternion targetModelRotation = Quaternion.LookRotation(-surfaceNormal.normalized, Vector3.up);
            _modelTransform.rotation = instant
                ? targetModelRotation
                : Quaternion.Slerp(_modelTransform.rotation, targetModelRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
        }

        public void ResetModelRotation()
        {
            if (_modelTransform == null)
            {
                return;
            }

            _isModelUprightBlendActive = false;
            _modelTransform.localRotation = Quaternion.identity;
        }

        public void BeginModelUprightBlend(float duration)
        {
            if (_modelTransform == null)
            {
                return;
            }

            _isModelUprightBlendActive = true;
            _modelBlendStartRotation = _modelTransform.localRotation;
            _modelBlendTimer = 0f;
            _modelBlendDuration = Mathf.Max(0.01f, duration);
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

        private void CacheReferences()
        {
            _characterController = GetComponent<CharacterController>();
            _locomotionState = GetComponent<PlayerLocomotionState>();
            _climbingState = GetComponent<PlayerClimbingState>();
            _climbJumpState = GetComponent<PlayerClimbJumpState>();
            _ledgeClimbState = GetComponent<PlayerLedgeClimbState>();
            _modelTransform = transform.Find("Model");
        }

        private void InitializePitchFromCamera()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            _pitch = NormalizePitch(_cameraTransform.localEulerAngles.x);
        }

        private void SetCursorLocked(bool isLocked)
        {
            if (!isLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
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

            if (_cameraYawPivot == null)
            {
                Debug.LogWarning("Player is missing a CameraYawPivot reference.", this);
                return;
            }

            if (_cameraTransform.parent != _cameraYawPivot)
            {
                _cameraTransform.SetParent(_cameraYawPivot, true);
            }
        }

        private void HandleLookIfAllowed()
        {
            if (_state == PlayerStateType.LedgeClimb)
            {
                return;
            }

            HandleLook();
        }

        private void TickCurrentState()
        {
            switch (_state)
            {
                case PlayerStateType.Locomotion:
                    TickLocomotionState();
                    break;
                case PlayerStateType.Climbing:
                    _climbingState.Tick();
                    break;
                case PlayerStateType.ClimbJump:
                    _climbJumpState.Tick();
                    break;
                case PlayerStateType.LedgeClimb:
                    _ledgeClimbState.Tick();
                    break;
            }
        }

        private void TickLocomotionState()
        {
            if (!_climbingState.TryEnterFromInput())
            {
                _locomotionState.Tick();
            }
        }

        private void HandleLook()
        {
            if (_cameraTransform == null || _cameraYawPivot == null)
            {
                return;
            }

            Vector2 lookInput = GameInput.Instance.GetLookVector();

            if (_state == PlayerStateType.Climbing || _state == PlayerStateType.ClimbJump)
            {
                HandleClimbLook(lookInput);
                return;
            }

            HandleLocomotionLook(lookInput);
        }

        private void TickModelUprightBlend()
        {
            if (!_isModelUprightBlendActive || _modelTransform == null)
            {
                return;
            }

            _modelBlendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_modelBlendTimer / _modelBlendDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            _modelTransform.localRotation = Quaternion.Slerp(_modelBlendStartRotation, Quaternion.identity, easedT);

            if (t >= 1f)
            {
                _isModelUprightBlendActive = false;
                _modelTransform.localRotation = Quaternion.identity;
            }
        }

        private void HandleClimbLook(Vector2 lookInput)
        {
            _climbCameraYaw = Mathf.Clamp(
                _climbCameraYaw + (lookInput.x * _locomotionState.LookSensitivityX),
                -_climbingState.ClimbCameraYawLimit,
                _climbingState.ClimbCameraYawLimit);

            _pitch = Mathf.Clamp(
                _pitch - (lookInput.y * _locomotionState.LookSensitivityY),
                -_climbingState.ClimbCameraPitchUpLimit,
                _climbingState.ClimbCameraPitchDownLimit);

            ApplyCameraLocalRotation();
        }

        private void HandleLocomotionLook(Vector2 lookInput)
        {
            transform.Rotate(Vector3.up, lookInput.x * _locomotionState.LookSensitivityX);
            ResetClimbCamera();
            _pitch = Mathf.Clamp(
                _pitch - (lookInput.y * _locomotionState.LookSensitivityY),
                -_locomotionState.MaxLookPitch,
                _locomotionState.MaxLookPitch);
            ApplyCameraLocalRotation();
        }

        private void ApplyCameraLocalRotation()
        {
            if (_cameraYawPivot != null)
            {
                _cameraYawPivot.localRotation = Quaternion.Euler(0f, _climbCameraYaw, 0f);
            }

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void RotateRootToHorizontalDirection(Vector3 sourceDirection)
        {
            Vector3 desiredForward = Vector3.ProjectOnPlane(sourceDirection, Vector3.up);
            if (desiredForward.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                desiredForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            if (desiredForward.sqrMagnitude > MinimumDirectionSqrMagnitude)
            {
                transform.rotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
            }
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}
