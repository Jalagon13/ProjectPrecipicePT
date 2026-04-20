using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerClimbJumpState : MonoBehaviour
    {
        private const float MinimumMoveSqrMagnitude = 0.0001f;

        [Header("Climb Jump")]
        [SerializeField, Tooltip("Total extra dash distance added by the climb jump.")]
        private float _climbJumpDistance = 3f;
        [SerializeField, Tooltip("How long the climb jump dash lasts before fully blending back into normal climbing.")]
        private float _climbJumpDuration = 0.3f;
        [SerializeField, Tooltip("How long the player must wait after a climb jump before another climb jump can begin.")]
        private float _climbJumpCooldown = 1f;

        private Player _player;
        private PlayerClimbingState _climbingState;
        private Vector3 _startJumpDirection;
        private float _timer;
        private float _nextAvailableJumpTime;
        private float _initialDashSpeed;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _climbingState = GetComponent<PlayerClimbingState>();
        }

        public void Begin(Vector3 jumpDirection)
        {
            _startJumpDirection = jumpDirection.normalized;
            _timer = 0f;
            _nextAvailableJumpTime = Time.time + _climbJumpCooldown;
            _initialDashSpeed = (2f * _climbJumpDistance) / Mathf.Max(0.01f, _climbJumpDuration);
            StaminaManager.Instance?.ForceEndSprint("Climb jump started");
            _player.SetState(Player.PlayerStateType.ClimbJump);
        }

        public bool CanBegin()
        {
            return Time.time >= _nextAvailableJumpTime;
        }

        // The jump keeps using climb-state movement checks, but the speed blends:
        // dash speed fades out while normal climb speed fades in from live input.
        public void Tick()
        {
            _timer += Time.deltaTime;

            float duration = Mathf.Max(0.01f, _climbJumpDuration);
            float normalizedTime = Mathf.Clamp01(_timer / duration);
            Vector3 movementDirection = GetMovementDirection(normalizedTime);
            float movementSpeed = GetDashSpeed(normalizedTime) + GetClimbInputSpeed(normalizedTime);
            float movementDistance = movementSpeed * Time.deltaTime;

            if (movementDistance > 0f &&
                movementDirection.sqrMagnitude > MinimumMoveSqrMagnitude &&
                !_climbingState.TryAdvanceClimbJump(movementDirection.normalized * movementDistance))
            {
                Finish();
                return;
            }

            if (_player.State != Player.PlayerStateType.ClimbJump)
            {
                return;
            }

            if (normalizedTime >= 1f)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (GameInput.Instance != null &&
                GameInput.Instance.IsClimbingPressed() &&
                _climbingState.TryResumeAfterClimbJump())
            {
                return;
            }

            _climbingState.ExitToLocomotion();
        }

        private Vector3 GetMovementDirection(float normalizedTime)
        {
            if (GameInput.Instance != null &&
                _climbingState.TryGetClimbJumpDirection(GameInput.Instance.GetMovementVector(), out Vector3 inputDirection))
            {
                return Vector3.Slerp(_startJumpDirection, inputDirection, normalizedTime).normalized;
            }

            return _startJumpDirection;
        }

        private float GetDashSpeed(float normalizedTime)
        {
            return Mathf.Lerp(_initialDashSpeed, 0f, normalizedTime);
        }

        private float GetClimbInputSpeed(float normalizedTime)
        {
            if (GameInput.Instance == null || !_climbingState.HasClimbMoveInput(GameInput.Instance.GetMovementVector()))
            {
                return 0f;
            }

            return Mathf.Lerp(0f, _climbingState.ClimbMoveSpeed, normalizedTime);
        }
    }
}
