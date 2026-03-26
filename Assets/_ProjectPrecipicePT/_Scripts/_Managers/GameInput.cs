using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectPrecipicePT
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnMove;

        private PlayerInput _playerInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _playerInput = new();
            _playerInput.Enable();
            
            _playerInput.Player.Move.started += GameInput_OnMove;
            _playerInput.Player.Move.performed += GameInput_OnMove;
            _playerInput.Player.Move.canceled += GameInput_OnMove;
            _playerInput.Player.ToggleInventory.started += GameInput_OnToggleInventory;
        }

        private void OnDestroy()
        {
            if (_playerInput == null)
            {
                return;
            }

            _playerInput.Player.Move.started -= GameInput_OnMove;
            _playerInput.Player.Move.performed -= GameInput_OnMove;
            _playerInput.Player.Move.canceled -= GameInput_OnMove;
            _playerInput.Player.ToggleInventory.started -= GameInput_OnToggleInventory;
            _playerInput.Disable();
            _playerInput.Dispose();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void GameInput_OnToggleInventory(InputAction.CallbackContext context)
        {
            
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(this, context);
        }

        public Vector2 GetMovementVector()
        {
            Vector2 movementVector = _playerInput.Player.Move.ReadValue<Vector2>();
            return movementVector.sqrMagnitude > 1f ? movementVector.normalized : movementVector;
        }

        public Vector2 GetLookVector()
        {
            return _playerInput.Player.Look.ReadValue<Vector2>();
        }

        public bool IsJumpPressedThisFrame()
        {
            return _playerInput.Player.Jump.WasPressedThisFrame();
        }

        public bool IsSprintPressed()
        {
            return _playerInput.Player.Sprint.IsPressed();
        }

        public bool IsClimbingPressed()
        {
            return _playerInput.Player.Climbing.IsPressed();
        }
    }
}
