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
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();
            
            _playerInput.Player.Move.started += GameInput_OnMove;
            _playerInput.Player.Move.performed += GameInput_OnMove;
            _playerInput.Player.Move.canceled += GameInput_OnMove;
        }

        private void OnDestroy()
        {
            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(this, context);
        }
    }
}
