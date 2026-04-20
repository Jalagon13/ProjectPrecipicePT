using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectPrecipicePT
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnMove;
        public event EventHandler<InputAction.CallbackContext> OnToggleInventory;
        public event EventHandler<InputAction.CallbackContext> OnInteract;

        public event EventHandler<InputAction.CallbackContext> OnScrollWheel;
        public event EventHandler<InputAction.CallbackContext> OnSelectSlot;

        private PlayerInput _playerInput;
        private bool _isGameplayInputBlocked;

        public bool IsHoldingDownInteract { get; private set; }

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
            _playerInput.Player.Interact.started += GameInput_OnInteract;
            
            _playerInput.UI.ScrollWheel.performed += PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started += PlayerInput_OnSelectSlot;
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
            _playerInput.Player.Interact.started -= GameInput_OnInteract;

            _playerInput.UI.ScrollWheel.performed -= PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started -= PlayerInput_OnSelectSlot;


            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void GameInput_OnInteract(InputAction.CallbackContext context)
        {
            IsHoldingDownInteract = context.ReadValueAsButton();
            OnInteract?.Invoke(this, context);  
        }

        private void PlayerInput_OnScrollWheel(InputAction.CallbackContext context)
        {
            OnScrollWheel?.Invoke(this, context);
        }

        private void PlayerInput_OnSelectSlot(InputAction.CallbackContext context)
        {
            OnSelectSlot?.Invoke(this, context);
        }

        private void GameInput_OnToggleInventory(InputAction.CallbackContext context)
        {
            OnToggleInventory?.Invoke(this, context);
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(this, context);
        }

        public Vector2 GetMovementVector()
        {
            if (_isGameplayInputBlocked)
            {
                return Vector2.zero;
            }

            Vector2 movementVector = _playerInput.Player.Move.ReadValue<Vector2>();
            return movementVector.sqrMagnitude > 1f ? movementVector.normalized : movementVector;
        }

        public Vector2 GetLookVector()
        {
            if (_isGameplayInputBlocked)
            {
                return Vector2.zero;
            }

            return _playerInput.Player.Look.ReadValue<Vector2>();
        }

        public bool IsJumpPressedThisFrame()
        {
            if (_isGameplayInputBlocked)
            {
                return false;
            }

            return _playerInput.Player.Jump.WasPressedThisFrame();
        }

        public bool IsSprintPressed()
        {
            if (_isGameplayInputBlocked)
            {
                return false;
            }

            return _playerInput.Player.Sprint.IsPressed();
        }

        public bool IsClimbingPressed()
        {
            if (_isGameplayInputBlocked)
            {
                return false;
            }

            return _playerInput.Player.Climbing.IsPressed();
        }

        public void SetGameplayInputBlocked(bool isBlocked)
        {
            _isGameplayInputBlocked = isBlocked;
        }
    }
}
