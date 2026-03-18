using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerLocomotionState : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _sprintSpeed = 7.5f;
        [SerializeField] private float _airMoveMultiplier = 0.6f;
        [SerializeField] private float _jumpHeight = 1.4f;
        [SerializeField] private float _jumpCooldown = 0.15f;

        [Header("Vertical Movement")]
        [SerializeField] private float _gravity = 25f;
        [SerializeField] private float _terminalVelocity = 53f;
        [SerializeField] private float _stickToGroundForce = 5f;

        [Header("Look")]
        [SerializeField] private float _lookSensitivityX = 0.12f;
        [SerializeField] private float _lookSensitivityY = 0.12f;
        [SerializeField] private float _maxLookPitch = 80f;

        private Player _player;
        private float _verticalVelocity;
        private float _nextJumpTime;

        public float LookSensitivityX => _lookSensitivityX;
        public float LookSensitivityY => _lookSensitivityY;
        public float MaxLookPitch => _maxLookPitch;
        public float StickToGroundForce => _stickToGroundForce;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        public void Tick()
        {
            Vector2 moveInput = GameInput.Instance.GetMovementVector();
            bool isGrounded = _player.CharacterController.isGrounded;

            Vector3 moveDirection = (_player.PlayerTransform.right * moveInput.x) + (_player.PlayerTransform.forward * moveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            if (isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -_stickToGroundForce;
                }

                if (GameInput.Instance.IsJumpPressedThisFrame() && Time.time >= _nextJumpTime)
                {
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * 2f * _gravity);
                    _nextJumpTime = Time.time + _jumpCooldown;
                }
            }
            else
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity - (_gravity * Time.deltaTime), -_terminalVelocity);
            }

            float moveSpeed = GameInput.Instance.IsSprintPressed() && moveInput.sqrMagnitude > 0.0001f
                ? _sprintSpeed
                : _walkSpeed;

            float movementMultiplier = isGrounded ? 1f : _airMoveMultiplier;
            Vector3 horizontalVelocity = moveDirection * (moveSpeed * movementMultiplier);
            Vector3 velocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            _player.CharacterController.Move(velocity * Time.deltaTime);
        }

        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }

        public void ForceGroundedRecovery()
        {
            _verticalVelocity = -_stickToGroundForce;
        }
    }
}
