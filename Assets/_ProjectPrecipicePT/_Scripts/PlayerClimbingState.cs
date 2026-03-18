using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerClimbingState : MonoBehaviour
    {
        private struct ClimbSurfaceSample
        {
            public bool HasSurface;
            public Vector3 AverageNormal;
            public Vector3 AnchorPoint;
            public int HitCount;
            public int TopHits;
            public int BottomHits;
            public int LeftHits;
            public int RightHits;
        }

        [Header("Climb Filters")]
        [SerializeField] private LayerMask _climbableLayers = 1;
        [SerializeField] private float _climbAttachRange = 1.1f;
        [SerializeField] private float _climbAttachMaxFacingAngle = 45f;
        [SerializeField] private float _climbableSurfaceMinAngle = 50f;
        [SerializeField] private float _climbableSurfaceMaxAngle = 115f;

        [Header("Climb Motion")]
        [SerializeField] private float _climbMoveSpeed = 2.2f;
        [SerializeField] private float _climbSurfaceOffset = 0.05f;
        [SerializeField] private float _climbTopContinuationMaxAngle = 55f;
        [SerializeField] private float _climbSideContinuationMaxAngle = 45f;
        [SerializeField] private float _climbDownContinuationMaxAngle = 65f;

        [Header("Climb Surface Probes")]
        [SerializeField] private float _surfaceProbeDistance = 1.2f;
        [SerializeField] private float _surfaceProbeStartOffset = 0.1f;
        [SerializeField] private float _surfaceProbeYawAngle = 14f;
        [SerializeField] private float _surfaceProbePitchAngle = 14f;
        [SerializeField] private int _minimumSurfaceHits = 3;
        [SerializeField] private float _surfaceNormalConsistencyAngle = 35f;

        [Header("Climb Look")]
        [SerializeField] private float _climbCameraYawLimit = 70f;
        [SerializeField] private float _climbCameraPitchUpLimit = 60f;
        [SerializeField] private float _climbCameraPitchDownLimit = 80f;

        private Player _player;
        private PlayerLocomotionState _locomotionState;
        private PlayerLedgeClimbState _ledgeClimbState;
        private Vector3 _currentClimbNormal = Vector3.back;

        public LayerMask ClimbableLayers => _climbableLayers;
        public float ClimbCameraYawLimit => _climbCameraYawLimit;
        public float ClimbCameraPitchUpLimit => _climbCameraPitchUpLimit;
        public float ClimbCameraPitchDownLimit => _climbCameraPitchDownLimit;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _locomotionState = GetComponent<PlayerLocomotionState>();
            _ledgeClimbState = GetComponent<PlayerLedgeClimbState>();
        }

        public bool TryEnterFromInput()
        {
            if (!GameInput.Instance.IsClimbingPressed())
            {
                return false;
            }

            Vector3 searchDirection = _player.CameraTransform != null ? _player.CameraTransform.forward : _player.PlayerTransform.forward;
            if (searchDirection.sqrMagnitude <= 0.0001f)
            {
                searchDirection = _player.PlayerTransform.forward;
            }

            if (!TryGetClosestClimbHit(
                    _player.GetBodyCenter(_player.PlayerTransform.position),
                    searchDirection.normalized,
                    _climbAttachRange,
                    out RaycastHit hit))
            {
                return false;
            }

            if (!IsSurfaceClimbable(hit.normal))
            {
                return false;
            }

            float facingAngle = Vector3.Angle(searchDirection.normalized, -hit.normal);
            if (facingAngle > _climbAttachMaxFacingAngle)
            {
                return false;
            }

            BeginClimbing(hit);
            return true;
        }

        public void Tick()
        {
            Vector2 moveInput = GameInput.Instance.GetMovementVector();

            if (!GameInput.Instance.IsClimbingPressed())
            {
                ExitClimbing();
                return;
            }

            if (!TrySampleSurface(_player.PlayerTransform.position, out ClimbSurfaceSample currentSample))
            {
                if (moveInput.y > 0.01f && TryBeginLedgeClimb())
                {
                    return;
                }

                ExitClimbing();
                return;
            }

            ApplySample(currentSample);

            Vector3 wallUp = GetWallUp(_currentClimbNormal);
            Vector3 wallRight = Vector3.Cross(_currentClimbNormal, wallUp).normalized;

            if (Mathf.Abs(moveInput.y) > 0.01f)
            {
                bool allowVertical = moveInput.y > 0f ? currentSample.TopHits > 0 : currentSample.BottomHits > 0;
                if (allowVertical)
                {
                    Vector3 verticalMove = wallUp * (moveInput.y * _climbMoveSpeed * Time.deltaTime);
                    TryApplyClimbDelta(verticalMove, moveInput.y > 0f ? _climbTopContinuationMaxAngle : _climbDownContinuationMaxAngle);
                }
            }

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                bool movingLeft = moveInput.x < 0f;
                bool allowHorizontal = movingLeft ? currentSample.LeftHits > 0 : currentSample.RightHits > 0;
                if (allowHorizontal)
                {
                    Vector3 horizontalMove = wallRight * (moveInput.x * _climbMoveSpeed * Time.deltaTime);
                    TryApplyClimbDelta(horizontalMove, _climbSideContinuationMaxAngle);
                }
            }

            if (moveInput.y > 0.01f && TryBeginLedgeClimb())
            {
                return;
            }

            if (!TrySampleSurface(_player.PlayerTransform.position, out currentSample))
            {
                ExitClimbing();
            }
        }

        private void BeginClimbing(RaycastHit hit)
        {
            _locomotionState.ResetVerticalVelocity();
            _player.SetState(Player.PlayerStateType.Climbing);
            _player.ResetClimbCamera();
            _currentClimbNormal = hit.normal;
            _player.PlayerTransform.rotation = Quaternion.LookRotation(-_currentClimbNormal, Vector3.up);
            SnapToSurface(hit.point, hit.normal);
        }

        private void ExitClimbing()
        {
            _player.AlignRootToCameraYaw();
            _player.ResetClimbCamera();
            _player.SetState(Player.PlayerStateType.Locomotion);
        }

        private bool TryApplyClimbDelta(Vector3 worldDelta, float maxNormalDelta)
        {
            if (worldDelta.sqrMagnitude <= 0f)
            {
                return false;
            }

            Vector3 candidatePosition = _player.PlayerTransform.position + worldDelta;
            if (!TrySampleSurface(candidatePosition, out ClimbSurfaceSample sample))
            {
                return false;
            }

            float continuationAngle = Vector3.Angle(_currentClimbNormal, sample.AverageNormal);
            if (continuationAngle > maxNormalDelta)
            {
                return false;
            }

            ApplySample(sample);
            return true;
        }

        private bool TrySampleSurface(Vector3 rootPosition, out ClimbSurfaceSample sample)
        {
            Vector3 castDirection = GetSurfaceSearchDirection(rootPosition);
            if (castDirection.sqrMagnitude <= 0.0001f)
            {
                sample = new ClimbSurfaceSample();
                return false;
            }

            castDirection.Normalize();
            Vector3 wallUp = GetWallUp(-castDirection);
            Vector3 wallRight = Vector3.Cross(-castDirection, wallUp).normalized;
            Vector3 bodyCenter = _player.GetBodyCenter(rootPosition);

            int hitCount = 0;
            int topHits = 0;
            int bottomHits = 0;
            int leftHits = 0;
            int rightHits = 0;
            Vector3 normalSum = Vector3.zero;
            float normalWeightSum = 0f;
            Vector3 pointSum = Vector3.zero;
            float pointWeightSum = 0f;
            bool hasCenterHit = false;
            Vector3 centerPoint = Vector3.zero;
            Vector3 referenceSurfaceNormal = -castDirection;

            for (int row = -1; row <= 1; row++)
            {
                for (int col = -1; col <= 1; col++)
                {
                    Quaternion yawRotation = Quaternion.AngleAxis(col * _surfaceProbeYawAngle, wallUp);
                    Quaternion pitchRotation = Quaternion.AngleAxis(-row * _surfaceProbePitchAngle, wallRight);
                    Vector3 rayDirection = (yawRotation * pitchRotation * castDirection).normalized;
                    Vector3 origin = bodyCenter + (rayDirection * _surfaceProbeStartOffset);

                    if (!TryGetClosestClimbHit(origin, rayDirection, _surfaceProbeDistance, out RaycastHit hit))
                    {
                        continue;
                    }

                    if (Vector3.Angle(hit.normal, referenceSurfaceNormal) > _surfaceNormalConsistencyAngle)
                    {
                        continue;
                    }

                    hitCount++;
                    float normalWeight = 1f / Mathf.Max(0.01f, hit.distance);
                    normalSum += hit.normal * normalWeight;
                    normalWeightSum += normalWeight;
                    pointSum += hit.point * normalWeight;
                    pointWeightSum += normalWeight;

                    if (row > 0)
                    {
                        topHits++;
                    }
                    else if (row < 0)
                    {
                        bottomHits++;
                    }

                    if (col < 0)
                    {
                        leftHits++;
                    }
                    else if (col > 0)
                    {
                        rightHits++;
                    }

                    if (row == 0 && col == 0)
                    {
                        hasCenterHit = true;
                        centerPoint = hit.point;
                    }
                }
            }

            if (hitCount < _minimumSurfaceHits)
            {
                sample = new ClimbSurfaceSample();
                return false;
            }

            Vector3 averageNormal = normalWeightSum > 0f
                ? (normalSum / normalWeightSum).normalized
                : -castDirection;
            Vector3 anchorPoint = hasCenterHit
                ? centerPoint
                : (pointWeightSum > 0f ? pointSum / pointWeightSum : bodyCenter + (castDirection * _surfaceProbeDistance));

            sample = new ClimbSurfaceSample
            {
                HasSurface = true,
                AverageNormal = averageNormal,
                AnchorPoint = anchorPoint,
                HitCount = hitCount,
                TopHits = topHits,
                BottomHits = bottomHits,
                LeftHits = leftHits,
                RightHits = rightHits
            };
            return true;
        }

        private bool TryBeginLedgeClimb()
        {
            Vector3 probeOrigin = _player.GetBodyCenter(_player.PlayerTransform.position) +
                                  (Vector3.up * _ledgeClimbState.LedgeProbeUpOffset) +
                                  (-_currentClimbNormal * _ledgeClimbState.LedgeProbeForwardOffset);

            if (!Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out RaycastHit topHit,
                    _ledgeClimbState.LedgeDownProbeDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (!CanStandOnSurface(topHit.normal))
            {
                return false;
            }

            Vector3 candidatePosition = topHit.point +
                                        (-_currentClimbNormal * _ledgeClimbState.LedgeStandForwardOffset) +
                                        (topHit.normal * (_player.GetStandingRootOffset() + _ledgeClimbState.LedgeStandHeightOffset));

            if (!_player.HasCharacterClearance(candidatePosition))
            {
                return false;
            }

            _ledgeClimbState.Begin(candidatePosition);
            return true;
        }

        private void ApplySample(ClimbSurfaceSample sample)
        {
            _currentClimbNormal = sample.AverageNormal;
            _player.PlayerTransform.rotation = Quaternion.LookRotation(-_currentClimbNormal, Vector3.up);
            SnapToSurface(sample.AnchorPoint, _currentClimbNormal);
        }

        private void SnapToSurface(Vector3 hitPoint, Vector3 surfaceNormal)
        {
            Vector3 desiredCenter = hitPoint + (surfaceNormal * (_player.CharacterController.radius + _climbSurfaceOffset));
            _player.PlayerTransform.position = desiredCenter - _player.CharacterController.center;
        }

        private Vector3 GetWallUp(Vector3 wallNormal)
        {
            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, wallNormal);
            return wallUp.sqrMagnitude > 0.0001f ? wallUp.normalized : Vector3.up;
        }

        private Vector3 GetSurfaceSearchDirection(Vector3 rootPosition)
        {
            Vector3 bodyCenter = _player.GetBodyCenter(rootPosition);

            if (_currentClimbNormal.sqrMagnitude > 0.0001f &&
                TryGetClosestClimbHit(bodyCenter + (_currentClimbNormal * _surfaceProbeStartOffset), -_currentClimbNormal, _surfaceProbeDistance, out RaycastHit currentNormalHit))
            {
                return (currentNormalHit.point - bodyCenter).normalized;
            }

            Vector3 playerForward = _player.PlayerTransform.forward;
            if (playerForward.sqrMagnitude > 0.0001f &&
                TryGetClosestClimbHit(bodyCenter, playerForward, _surfaceProbeDistance, out RaycastHit forwardHit))
            {
                return (forwardHit.point - bodyCenter).normalized;
            }

            return -_currentClimbNormal;
        }

        private bool TryGetClosestClimbHit(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit closestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, _climbableLayers, QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            closestHit = default;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsSurfaceClimbable(hits[i].normal))
                {
                    continue;
                }

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closestHit = hits[i];
                }
            }

            return closestDistance < float.MaxValue;
        }

        private bool IsSurfaceClimbable(Vector3 surfaceNormal)
        {
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            return surfaceAngle >= _climbableSurfaceMinAngle && surfaceAngle <= _climbableSurfaceMaxAngle;
        }

        private bool CanStandOnSurface(Vector3 surfaceNormal)
        {
            return Vector3.Angle(surfaceNormal, Vector3.up) <= _player.CharacterController.slopeLimit;
        }
    }
}
