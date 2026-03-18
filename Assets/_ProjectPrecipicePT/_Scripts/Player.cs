using UnityEngine;

namespace ProjectPrecipicePT
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float airMoveMultiplier = 0.6f;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float jumpCooldown = 0.15f;

        [Header("Vertical Movement")]
        [SerializeField] private float gravity = 25f;
        [SerializeField] private float terminalVelocity = 53f;
        [SerializeField] private float stickToGroundForce = 5f;

        [Header("Look")]
        [SerializeField] private float lookSensitivityX = 0.12f;
        [SerializeField] private float lookSensitivityY = 0.12f;
        [SerializeField] private float maxLookPitch = 80f;

        private CharacterController _characterController;
        private Transform _cameraTransform;
        private float _verticalVelocity;
        private float _pitch;
        private float _nextJumpTime;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = GetComponentInChildren<Camera>()?.transform;

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
                _cameraTransform.SetParent(transform, true);
            }

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

            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            Vector2 lookInput = GameInput.Instance.GetLookVector();

            transform.Rotate(Vector3.up, lookInput.x * lookSensitivityX);

            _pitch = Mathf.Clamp(_pitch - (lookInput.y * lookSensitivityY), -maxLookPitch, maxLookPitch);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 moveInput = GameInput.Instance.GetMovementVector();
            bool isGrounded = _characterController.isGrounded;

            Vector3 moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            if (isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -stickToGroundForce;
                }

                if (GameInput.Instance.IsJumpPressedThisFrame() && Time.time >= _nextJumpTime)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
                    _nextJumpTime = Time.time + jumpCooldown;
                }
            }
            else
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity - (gravity * Time.deltaTime), -terminalVelocity);
            }

            float moveSpeed = GameInput.Instance.IsSprintPressed() && moveInput.sqrMagnitude > 0.0001f
                ? sprintSpeed
                : walkSpeed;

            float movementMultiplier = isGrounded ? 1f : airMoveMultiplier;
            Vector3 horizontalVelocity = moveDirection * (moveSpeed * movementMultiplier);
            Vector3 velocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            _characterController.Move(velocity * Time.deltaTime);
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
