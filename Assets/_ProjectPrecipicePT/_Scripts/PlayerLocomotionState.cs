using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerLocomotionState : MonoBehaviour
    {
        private const float MovementThresholdSqr = 0.0001f;
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        [Header("Movement")]
        [SerializeField, Tooltip("Base movement speed while walking on the ground.")]
        private float _walkSpeed = 4.5f;
        [SerializeField, Tooltip("Movement speed while sprint is held and the player is moving.")]
        private float _sprintSpeed = 7.5f;
        [SerializeField, Tooltip("Multiplier applied to horizontal movement while airborne.")]
        private float _airMoveMultiplier = 0.6f;
        [SerializeField, Tooltip("Target jump height reached when jumping from the ground.")]
        private float _jumpHeight = 1.4f;
        [SerializeField, Tooltip("Minimum time that must pass before another jump can start.")]
        private float _jumpCooldown = 0.15f;

        [Header("Coyote Timing")]
        [SerializeField, Tooltip("Whether jump coyote timing is enabled.")]
        private bool _useJumpCoyoteTime = true;
        [SerializeField, Min(0f), Tooltip("How long a jump remains valid after the player leaves the ground.")]
        private float _jumpCoyoteTime = 0.12f;

        [Header("Jump Buffering")]
        [SerializeField, Tooltip("Whether jump input buffering is enabled.")]
        private bool _useJumpBuffering = true;
        [SerializeField, Min(0f), Tooltip("How long a jump press stays queued before the player can jump.")]
        private float _jumpBufferTime = 0.12f;

        [Header("Vertical Movement")]
        [SerializeField, Tooltip("Downward acceleration applied while the player is airborne.")]
        private float _gravity = 25f;
        [SerializeField, Tooltip("Maximum downward speed while falling.")]
        private float _terminalVelocity = 53f;
        [SerializeField, Tooltip("Small downward force used to keep the controller grounded on slopes.")]
        private float _stickToGroundForce = 5f;

        [Header("Slope Sliding")]
        [SerializeField, Tooltip("Minimum ground angle, relative to up, where slope sliding begins.")]
        private float _minSlopeSlideAngle = 40f;
        [SerializeField, Tooltip("Maximum ground angle, relative to up, used when scaling the strongest slope slide.")]
        private float _maxSlopeSlideAngle = 70f;
        [SerializeField, Tooltip("Maximum downhill slide speed added while standing on a steep slope within the slide range.")]
        private float _maxSlopeSlideSpeed = 6f;

        [Header("Look")]
        [SerializeField, Tooltip("Horizontal mouse look sensitivity.")]
        private float _lookSensitivityX = 0.12f;
        [SerializeField, Tooltip("Vertical mouse look sensitivity.")]
        private float _lookSensitivityY = 0.12f;
        [SerializeField, Tooltip("Maximum up and down look angle while in normal locomotion.")]
        private float _maxLookPitch = 80f;

        private Player _player;
        private float _verticalVelocity;
        private float _nextJumpTime;
        private Timer _jumpCoyoteTimer;
        private Timer _jumpBufferTimer;
        private bool _wasGroundedLastFrame;

        public float LookSensitivityX => _lookSensitivityX;
        public float LookSensitivityY => _lookSensitivityY;
        public float MaxLookPitch => _maxLookPitch;
        public float VerticalVelocity => _verticalVelocity;
        public float TerminalVelocity => _terminalVelocity;

        private void Awake()
        {
            _player = GetComponent<Player>();
            InitializeJumpTimers();
            _wasGroundedLastFrame = _player.CharacterController != null && _player.CharacterController.isGrounded;
        }

        private void OnValidate()
        {
            InitializeJumpTimers();
        }

        // Normal movement update:
        // 1. read move input
        // 2. update vertical velocity (grounding / jump / gravity)
        // 3. move the controller
        public void Tick()
        {
            Vector2 moveInput = GameInput.Instance.GetMovementVector();
            bool isGrounded = _player.CharacterController.isGrounded;
            Vector3 moveDirection = GetMoveDirection(moveInput);

            UpdateJumpBufferTimer();
            UpdateJumpCoyoteTimer(isGrounded);
            UpdateVerticalVelocity(isGrounded);

            float moveSpeed = GetMoveSpeed(moveInput);
            float movementMultiplier = isGrounded ? 1f : _airMoveMultiplier;
            Vector3 horizontalVelocity = moveDirection * (moveSpeed * movementMultiplier);
            Vector3 slopeSlideVelocity = GetSlopeSlideVelocity(isGrounded);
            Vector3 totalVelocity = horizontalVelocity + (Vector3.up * _verticalVelocity);
            totalVelocity += slopeSlideVelocity;

            _player.CharacterController.Move(totalVelocity * Time.deltaTime);
        }

        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }

        public void ForceGroundedRecovery()
        {
            _verticalVelocity = -_stickToGroundForce;
        }

        private Vector3 GetMoveDirection(Vector2 moveInput)
        {
            Vector3 moveDirection = (_player.PlayerTransform.right * moveInput.x) + (_player.PlayerTransform.forward * moveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            return moveDirection;
        }

        private void UpdateVerticalVelocity(bool isGrounded)
        {
            if (isGrounded)
            {
                ApplyGroundedVerticalVelocity();
                return;
            }

            if (HasJumpCoyoteWindow() && TryStartJump())
            {
                StopJumpCoyoteTimer();
                return;
            }

            ApplyAirborneGravity();
        }

        private void ApplyGroundedVerticalVelocity()
        {
            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = -_stickToGroundForce;
            }

            TryStartJump();
        }

        private void ApplyAirborneGravity()
        {
            _verticalVelocity = Mathf.Max(_verticalVelocity - (_gravity * Time.deltaTime), -_terminalVelocity);
        }

        private bool CanStartJump()
        {
            return HasJumpInputBuffered() && Time.time >= _nextJumpTime && StaminaManager.Instance.CanStartJump();
        }

        private bool TryStartJump()
        {
            if (!CanStartJump())
            {
                return false;
            }

            if (StaminaManager.Instance != null && !StaminaManager.Instance.TryConsumeJumpStamina("Ground jump"))
            {
                return false;
            }

            _verticalVelocity = Mathf.Sqrt(_jumpHeight * 2f * _gravity);
            _nextJumpTime = Time.time + _jumpCooldown;
            ConsumeJumpInputBuffer();
            return true;
        }

        private void UpdateJumpBufferTimer()
        {
            if (_jumpBufferTimer == null)
            {
                return;
            }

            if (GameInput.Instance.IsJumpPressedThisFrame())
            {
                _jumpBufferTimer.Reset();
                _jumpBufferTimer.IsPaused = false;
            }

            _jumpBufferTimer.Tick(Time.deltaTime);
        }

        private void UpdateJumpCoyoteTimer(bool isGrounded)
        {
            if (_jumpCoyoteTimer == null)
            {
                _wasGroundedLastFrame = isGrounded;
                return;
            }

            if (isGrounded)
            {
                StopJumpCoyoteTimer();
                _wasGroundedLastFrame = true;
                return;
            }

            if (_wasGroundedLastFrame && _verticalVelocity <= 0f)
            {
                _jumpCoyoteTimer.Reset();
                _jumpCoyoteTimer.IsPaused = false;
            }

            _jumpCoyoteTimer.Tick(Time.deltaTime);
            _wasGroundedLastFrame = false;
        }

        private bool HasJumpCoyoteWindow()
        {
            return _useJumpCoyoteTime &&
                   _jumpCoyoteTimer != null &&
                   _jumpCoyoteTimer.RemainingSeconds > 0f;
        }

        private bool HasJumpInputBuffered()
        {
            if (_useJumpBuffering)
            {
                return _jumpBufferTimer != null && _jumpBufferTimer.RemainingSeconds > 0f;
            }

            return GameInput.Instance.IsJumpPressedThisFrame();
        }

        private void ConsumeJumpInputBuffer()
        {
            if (_jumpBufferTimer == null)
            {
                return;
            }

            _jumpBufferTimer.IsPaused = true;
            _jumpBufferTimer.RemainingSeconds = 0f;
        }

        private void StopJumpCoyoteTimer()
        {
            if (_jumpCoyoteTimer == null)
            {
                return;
            }

            _jumpCoyoteTimer.IsPaused = true;
            _jumpCoyoteTimer.RemainingSeconds = 0f;
        }

        private void InitializeJumpTimers()
        {
            InitializeJumpCoyoteTimer();
            InitializeJumpBufferTimer();
        }

        private void InitializeJumpCoyoteTimer()
        {
            if (!_useJumpCoyoteTime || _jumpCoyoteTime <= 0f)
            {
                _jumpCoyoteTimer = null;
                return;
            }

            _jumpCoyoteTimer = new Timer(_jumpCoyoteTime);
            _jumpCoyoteTimer.IsPaused = true;
            _jumpCoyoteTimer.RemainingSeconds = 0f;
        }

        private void InitializeJumpBufferTimer()
        {
            if (!_useJumpBuffering || _jumpBufferTime <= 0f)
            {
                _jumpBufferTimer = null;
                return;
            }

            _jumpBufferTimer = new Timer(_jumpBufferTime);
            _jumpBufferTimer.IsPaused = true;
            _jumpBufferTimer.RemainingSeconds = 0f;
        }

        private float GetMoveSpeed(Vector2 moveInput)
        {
            bool isTryingToMove = moveInput.sqrMagnitude > MovementThresholdSqr;
            bool canSprint = StaminaManager.Instance == null ||
                             StaminaManager.Instance.TrySustainSprint(GameInput.Instance.IsSprintPressed() && isTryingToMove, Time.deltaTime);

            return canSprint
                ? _sprintSpeed
                : _walkSpeed;
        }

        private Vector3 GetSlopeSlideVelocity(bool isGrounded)
        {
            if (!isGrounded || !TryGetGroundNormal(out Vector3 groundNormal))
            {
                return Vector3.zero;
            }

            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            if (slopeAngle < _minSlopeSlideAngle || slopeAngle > _maxSlopeSlideAngle)
            {
                return Vector3.zero;
            }

            Vector3 downhillDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
            if (downhillDirection.sqrMagnitude <= MinimumDirectionSqrMagnitude)
            {
                return Vector3.zero;
            }

            float slideStrength = Mathf.InverseLerp(_minSlopeSlideAngle, _maxSlopeSlideAngle, slopeAngle);
            float slideSpeed = slideStrength * _maxSlopeSlideSpeed;
            return downhillDirection.normalized * slideSpeed;
        }

        private bool TryGetGroundNormal(out Vector3 groundNormal)
        {
            CharacterController controller = _player.CharacterController;
            Vector3 bodyCenter = _player.GetBodyCenter(_player.PlayerTransform.position);
            float sphereRadius = Mathf.Max(0.05f, controller.radius - controller.skinWidth);
            float castDistance = (controller.height * 0.5f) - controller.radius + controller.skinWidth + 0.3f;
            Vector3 origin = bodyCenter + (Vector3.up * 0.05f);

            if (Physics.SphereCast(
                    origin,
                    sphereRadius,
                    Vector3.down,
                    out RaycastHit hit,
                    castDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                !IsOwnedCollider(hit.collider))
            {
                groundNormal = hit.normal;
                return true;
            }

            groundNormal = Vector3.up;
            return false;
        }

        private bool IsOwnedCollider(Collider collider)
        {
            return collider != null && collider.transform.IsChildOf(_player.PlayerTransform);
        }
    }
}
