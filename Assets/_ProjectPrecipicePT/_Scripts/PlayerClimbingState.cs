using UnityEngine;

namespace ProjectPrecipicePT
{
    public class PlayerClimbingState : MonoBehaviour
    {
        private struct ClimbSurfaceSample
        {
            public Vector3 AverageNormal;
            public Vector3 AnchorPoint;
            public bool UpHit;
            public bool DownHit;
            public bool LeftHit;
            public bool RightHit;
            public bool UpLeftHit;
            public bool UpRightHit;
            public bool DownLeftHit;
            public bool DownRightHit;
        }

        [Header("Climb Filters")]
        [SerializeField, Tooltip("Layers that the climb system treats as climbable surfaces.")]
        private LayerMask _climbableLayers = 1;
        [SerializeField, Tooltip("Maximum distance from the player center to attach to a climbable surface.")]
        private float _climbAttachRange = 1f;
        [SerializeField, Tooltip("How directly the player must be facing a wall to begin climbing.")]
        private float _climbAttachMaxFacingAngle = 45f;
        [SerializeField, Tooltip("Minimum surface angle, relative to up, that is considered climbable.")]
        private float _climbableSurfaceMinAngle = 50f;
        [SerializeField, Tooltip("Maximum surface angle, relative to up, that is considered climbable.")]
        private float _climbableSurfaceMaxAngle = 115f;

        [Header("Climb Motion")]
        [SerializeField, Tooltip("Movement speed while the player is attached to a climbable surface.")]
        private float _climbMoveSpeed = 2.2f;
        [SerializeField, Tooltip("Maximum duration of the entry lerp when the player first attaches to a climbable surface.")]
        private float _climbAttachMaxDuration = 0.35f;
        [SerializeField, Tooltip("Duration of the detach blend when the model returns from a climb tilt back to upright.")]
        private float _climbDetachDuration = 0.35f;
        [SerializeField, Tooltip("How far the player capsule stays offset away from the climbed surface.")]
        private float _climbSurfaceOffset = 0.225f;
        [SerializeField, Tooltip("How quickly the detected climb normal smooths toward the latest sampled surface.")]
        private float _climbNormalLerpSpeed = 10f;
        [SerializeField, Tooltip("How quickly the player rotates to face the smoothed climb normal.")]
        private float _climbRotationLerpSpeed = 12f;
        [SerializeField, Tooltip("Maximum surface angle change that still counts as a valid upward climb transition.")]
        private float _maxUpwardSurfaceTransitionAngle = 55f;
        [SerializeField, Tooltip("Maximum surface angle change that still counts as a valid downward climb transition.")]
        private float _maxDownwardSurfaceTransitionAngle = 65f;

        [Header("Fall Sliding")]
        [SerializeField, Tooltip("Minimum downward slide distance applied when the player catches a wall while falling.")]
        private float _fallGrabSlideMinDistance = 0.5f;
        [SerializeField, Tooltip("Maximum downward slide distance applied when the player catches a wall at terminal velocity.")]
        private float _fallGrabSlideMaxDistance = 2f;

        [Header("Climb Surface Probes")]
        [SerializeField, Tooltip("How far each climb sampling ray can search for a climbable surface.")]
        private float _surfaceProbeDistance = 1.2f;
        [SerializeField, Tooltip("How far forward from the player center each climb sampling ray begins.")]
        private float _surfaceProbeStartOffset = 0.1f;
        [SerializeField, Tooltip("Angle used for the eight outer climb rays around the center ray.")]
        private float _surfaceProbeRingAngle = 30f;
        [SerializeField, Tooltip("Minimum number of ray hits required for the current climb surface to remain valid.")]
        private int _minimumSurfaceHits = 3;
        [SerializeField, Tooltip("Maximum allowed normal difference between sampled hits and the current climbed surface.")]
        private float _surfaceNormalConsistencyAngle = 60f;
        [SerializeField, Tooltip("Draw the nine climb probe rays in the Scene view while climbing.")]
        private bool _drawDebugProbeRays = true;

        [Header("Climb Look")]
        [SerializeField, Tooltip("Maximum horizontal free-look angle while climbing.")]
        private float _climbCameraYawLimit = 70f;
        [SerializeField, Tooltip("Maximum upward look angle while climbing.")]
        private float _climbCameraPitchUpLimit = 60f;
        [SerializeField, Tooltip("Maximum downward look angle while climbing.")]
        private float _climbCameraPitchDownLimit = 80f;

        private static readonly Vector2 DiagonalUnit = new Vector2(0.70710677f, 0.70710677f);

        private Player _player;
        private PlayerLocomotionState _locomotionState;
        private PlayerLedgeClimbState _ledgeClimbState;
        private Vector3 _currentClimbNormal = Vector3.back;
        private bool _isAttaching;
        private Vector3 _attachStartPosition;
        private Vector3 _attachTargetPosition;
        private Vector3 _attachTargetNormal;
        private float _attachTimer;
        private float _attachDuration;
        private bool _attachIncludesSlide;

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

            if (!TryGetClosestClimbHit(_player.GetBodyCenter(_player.PlayerTransform.position), searchDirection.normalized, _climbAttachRange, out RaycastHit hit))
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
            if (!GameInput.Instance.IsClimbingPressed())
            {
                ExitClimbing();
                return;
            }

            if (_isAttaching)
            {
                TickAttach();
                return;
            }

            Vector2 moveInput = GameInput.Instance.GetMovementVector();
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

            if (moveInput.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            if (!HasMovementSupport(currentSample, moveInput))
            {
                if (moveInput.y > 0.01f)
                {
                    TryBeginLedgeClimb();
                }

                return;
            }

            Vector3 wallUp = GetWallUp(_currentClimbNormal);
            Vector3 wallRight = Vector3.Cross(_currentClimbNormal, wallUp).normalized;
            Vector3 climbDelta = ((wallRight * moveInput.x) + (wallUp * moveInput.y)).normalized * (_climbMoveSpeed * Time.deltaTime);

            if (Mathf.Abs(moveInput.y) > 0.01f &&
                IsVerticalMovementBlocked(wallUp * Mathf.Sign(moveInput.y), climbDelta.magnitude, moveInput.y > 0f ? _maxUpwardSurfaceTransitionAngle : _maxDownwardSurfaceTransitionAngle))
            {
                if (moveInput.y > 0.01f)
                {
                    TryBeginLedgeClimb();
                }

                return;
            }

            Vector3 candidatePosition = _player.PlayerTransform.position + climbDelta;

            if (!TrySampleSurface(candidatePosition, out ClimbSurfaceSample candidateSample))
            {
                if (moveInput.y > 0.01f)
                {
                    TryBeginLedgeClimb();
                }

                return;
            }

            ApplySample(candidateSample);
        }

        private void BeginClimbing(RaycastHit hit)
        {
            float fallSpeed = Mathf.Max(0f, -_locomotionState.VerticalVelocity);
            bool shouldSlideOnGrab = !_player.CharacterController.isGrounded && fallSpeed > 0.01f;

            _locomotionState.ResetVerticalVelocity();
            _player.SetState(Player.PlayerStateType.Climbing);
            _player.ResetClimbCamera();
            _currentClimbNormal = hit.normal;
            _attachStartPosition = _player.PlayerTransform.position;
            _attachTargetNormal = hit.normal;
            Vector3 snappedRootPosition = GetSnappedRootPosition(hit.point, hit.normal);
            _attachIncludesSlide = false;

            if (shouldSlideOnGrab)
            {
                float fallRatio = Mathf.Clamp01(fallSpeed / Mathf.Max(0.01f, _locomotionState.TerminalVelocity));
                float slideDistance = Mathf.Lerp(_fallGrabSlideMinDistance, _fallGrabSlideMaxDistance, fallRatio);
                _attachTargetPosition = snappedRootPosition - (GetWallUp(hit.normal) * slideDistance);
                _attachIncludesSlide = slideDistance > 0.0001f;
            }
            else
            {
                _attachTargetPosition = snappedRootPosition;
            }

            _attachTimer = 0f;

            float attachDistance = Vector3.Distance(_attachStartPosition, _attachTargetPosition);
            float normalizedAttachDistance = Mathf.Clamp01(attachDistance / Mathf.Max(0.01f, _climbAttachRange));
            _attachDuration = Mathf.Lerp(0.05f, Mathf.Max(0.05f, _climbAttachMaxDuration), normalizedAttachDistance);
            _isAttaching = true;
        }

        private void ExitClimbing()
        {
            _isAttaching = false;
            _player.SnapDetachFacingToCurrentLook();
            _player.BeginModelUprightBlend(_climbDetachDuration);
            _player.SetState(Player.PlayerStateType.Locomotion);
        }

        private void TickAttach()
        {
            _attachTimer += Time.deltaTime;
            float duration = Mathf.Max(0.01f, _attachDuration);
            float t = Mathf.Clamp01(_attachTimer / duration);
            float easedT = _attachIncludesSlide
                ? 1f - Mathf.Pow(1f - t, 3f)
                : Mathf.SmoothStep(0f, 1f, t);

            _player.PlayerTransform.position = Vector3.Lerp(_attachStartPosition, _attachTargetPosition, easedT);
            _player.ApplyClimbFacing(_attachTargetNormal, _climbRotationLerpSpeed, false);

            if (t < 1f)
            {
                return;
            }

            _player.PlayerTransform.position = _attachTargetPosition;
            _currentClimbNormal = _attachTargetNormal;
            _isAttaching = false;
        }

        private bool TrySampleSurface(Vector3 rootPosition, out ClimbSurfaceSample sample)
        {
            Vector3 bodyCenter = _player.GetBodyCenter(rootPosition);
            Vector3 wallNormal = _currentClimbNormal.sqrMagnitude > 0.0001f
                ? _currentClimbNormal.normalized
                : -_player.PlayerTransform.forward;
            Vector3 centerDirection = -wallNormal;
            Vector3 wallUp = GetWallUp(wallNormal);
            Vector3 wallRight = Vector3.Cross(wallNormal, wallUp).normalized;

            float ringAngleRadians = Mathf.Deg2Rad * _surfaceProbeRingAngle;
            float tangent = Mathf.Tan(ringAngleRadians);
            int hitCount = 0;
            Vector3 normalSum = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            float pointWeightSum = 0f;
            bool hasCenterHit = false;
            Vector3 centerPoint = Vector3.zero;
            bool upHit = false;
            bool downHit = false;
            bool leftHit = false;
            bool rightHit = false;
            bool upLeftHit = false;
            bool upRightHit = false;
            bool downLeftHit = false;
            bool downRightHit = false;

            Vector2[] offsets =
            {
                Vector2.zero,
                Vector2.up,
                Vector2.down,
                Vector2.left,
                Vector2.right,
                new Vector2(-DiagonalUnit.x, DiagonalUnit.y),
                new Vector2(DiagonalUnit.x, DiagonalUnit.y),
                new Vector2(-DiagonalUnit.x, -DiagonalUnit.y),
                new Vector2(DiagonalUnit.x, -DiagonalUnit.y)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 offset = offsets[i];
                Vector3 rayDirection = (centerDirection + (wallRight * offset.x * tangent) + (wallUp * offset.y * tangent)).normalized;
                Vector3 rayOrigin = bodyCenter + (rayDirection * _surfaceProbeStartOffset);

                if (!TryGetClosestSurfaceHit(rayOrigin, rayDirection, _surfaceProbeDistance, out RaycastHit hit))
                {
                    DrawProbeRay(rayOrigin, rayDirection, _surfaceProbeDistance, Color.red);
                    continue;
                }

                if (!IsSurfaceClimbable(hit.normal))
                {
                    DrawProbeRay(rayOrigin, rayDirection, hit.distance, new Color(0.45f, 0f, 0f));
                    continue;
                }

                if (Vector3.Angle(hit.normal, wallNormal) > _surfaceNormalConsistencyAngle)
                {
                    DrawProbeRay(rayOrigin, rayDirection, hit.distance, new Color(1f, 0.5f, 0f));
                    continue;
                }

                DrawProbeRay(rayOrigin, rayDirection, hit.distance, GetProbeRayColor(i));

                hitCount++;
                float pointWeight = 1f / Mathf.Max(0.01f, hit.distance);
                normalSum += hit.normal;
                pointSum += hit.point * pointWeight;
                pointWeightSum += pointWeight;

                switch (i)
                {
                    case 0:
                        hasCenterHit = true;
                        centerPoint = hit.point;
                        break;
                    case 1:
                        upHit = true;
                        break;
                    case 2:
                        downHit = true;
                        break;
                    case 3:
                        leftHit = true;
                        break;
                    case 4:
                        rightHit = true;
                        break;
                    case 5:
                        upLeftHit = true;
                        break;
                    case 6:
                        upRightHit = true;
                        break;
                    case 7:
                        downLeftHit = true;
                        break;
                    case 8:
                        downRightHit = true;
                        break;
                }
            }

            if (hitCount < _minimumSurfaceHits)
            {
                sample = default;
                return false;
            }

            sample = new ClimbSurfaceSample
            {
                AverageNormal = normalSum.normalized,
                AnchorPoint = hasCenterHit
                    ? centerPoint
                    : (pointWeightSum > 0f ? pointSum / pointWeightSum : bodyCenter + (centerDirection * _surfaceProbeDistance)),
                UpHit = upHit,
                DownHit = downHit,
                LeftHit = leftHit,
                RightHit = rightHit,
                UpLeftHit = upLeftHit,
                UpRightHit = upRightHit,
                DownLeftHit = downLeftHit,
                DownRightHit = downRightHit
            };
            return true;
        }

        private void DrawProbeRay(Vector3 origin, Vector3 direction, float distance, Color color)
        {
            if (!_drawDebugProbeRays || _player.State != Player.PlayerStateType.Climbing)
            {
                return;
            }

            Debug.DrawRay(origin, direction * distance, color, 0f, false);
        }

        private static Color GetProbeRayColor(int rayIndex)
        {
            return rayIndex switch
            {
                0 => Color.white,
                1 => Color.blue,
                2 => Color.yellow,
                3 => Color.green,
                4 => Color.magenta,
                5 => Color.cyan,
                6 => new Color(0.5f, 0f, 1f),
                7 => new Color(1f, 0.5f, 0.5f),
                8 => new Color(0.5f, 1f, 0.5f),
                _ => Color.white
            };
        }

        private bool HasMovementSupport(ClimbSurfaceSample sample, Vector2 moveInput)
        {
            bool movingUp = moveInput.y > 0.01f;
            bool movingDown = moveInput.y < -0.01f;
            bool movingLeft = moveInput.x < -0.01f;
            bool movingRight = moveInput.x > 0.01f;

            if (movingUp && !sample.UpHit)
            {
                return false;
            }

            if (movingDown && !sample.DownHit)
            {
                return false;
            }

            if (movingLeft && !sample.LeftHit)
            {
                return false;
            }

            if (movingRight && !sample.RightHit)
            {
                return false;
            }

            if (movingUp && movingLeft && !sample.UpLeftHit)
            {
                return false;
            }

            if (movingUp && movingRight && !sample.UpRightHit)
            {
                return false;
            }

            if (movingDown && movingLeft && !sample.DownLeftHit)
            {
                return false;
            }

            if (movingDown && movingRight && !sample.DownRightHit)
            {
                return false;
            }

            return true;
        }

        private bool TryBeginLedgeClimb()
        {
            if (!TryFindLedgeTop(out RaycastHit topHit))
            {
                return false;
            }

            if (HasClimbableContinuationAbove(topHit.point.y))
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

        private bool TryFindLedgeTop(out RaycastHit topHit)
        {
            Vector3 bodyCenter = _player.GetBodyCenter(_player.PlayerTransform.position);
            Vector3 probeBase = bodyCenter + (Vector3.up * _ledgeClimbState.LedgeProbeUpOffset);
            float surfaceClearance = _player.CharacterController.radius + _climbSurfaceOffset;
            float firstForwardOffset = Mathf.Max(_ledgeClimbState.LedgeProbeForwardOffset, surfaceClearance + 0.05f);
            float secondForwardOffset = firstForwardOffset + _ledgeClimbState.LedgeStandForwardOffset;
            float thirdForwardOffset = secondForwardOffset + (_player.CharacterController.radius * 0.5f);

            float[] forwardOffsets = { firstForwardOffset, secondForwardOffset, thirdForwardOffset };

            for (int i = 0; i < forwardOffsets.Length; i++)
            {
                Vector3 probeOrigin = probeBase + (-_currentClimbNormal * forwardOffsets[i]);

                if (!Physics.Raycast(
                        probeOrigin,
                        Vector3.down,
                        out topHit,
                        _ledgeClimbState.LedgeDownProbeDistance,
                        ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (CanStandOnSurface(topHit.normal))
                {
                    return true;
                }
            }

            topHit = default;
            return false;
        }

        private bool HasClimbableContinuationAbove(float ledgeTopHeight)
        {
            Vector3 continuationProbeOrigin = _player.GetBodyCenter(_player.PlayerTransform.position) +
                                              (Vector3.up * _ledgeClimbState.LedgeContinuationCheckUpOffset) +
                                              (-_currentClimbNormal * _ledgeClimbState.LedgeContinuationCheckForwardOffset);

            if (!TryGetClosestClimbHit(
                    continuationProbeOrigin,
                    -_currentClimbNormal,
                    _ledgeClimbState.LedgeContinuationCheckDistance,
                    out RaycastHit continuationHit))
            {
                return false;
            }

            return Vector3.Angle(continuationHit.normal, _currentClimbNormal) <= _surfaceNormalConsistencyAngle &&
                   continuationHit.point.y > ledgeTopHeight + 0.05f;
        }

        private bool IsVerticalMovementBlocked(Vector3 moveDirection, float moveDistance, float allowedVerticalContinuationAngle)
        {
            if (moveDistance <= 0.0001f)
            {
                return false;
            }

            float castRadius = Mathf.Max(0.05f, (_player.CharacterController.radius - _player.CharacterController.skinWidth) * 0.9f);
            float halfHeight = Mathf.Max(castRadius, (_player.CharacterController.height * 0.5f) - castRadius);
            Vector3 center = _player.GetBodyCenter(_player.PlayerTransform.position);
            Vector3 wallOffset = _currentClimbNormal.normalized * (_player.CharacterController.radius + _climbSurfaceOffset + 0.02f);
            Vector3 capsuleCenter = center + wallOffset;
            Vector3 top = capsuleCenter + (Vector3.up * halfHeight);
            Vector3 bottom = capsuleCenter - (Vector3.up * halfHeight);

            if (!Physics.CapsuleCast(
                    top,
                    bottom,
                    castRadius,
                    moveDirection.normalized,
                    out RaycastHit hit,
                    moveDistance + _player.CharacterController.skinWidth,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (IsOwnedCollider(hit.collider))
            {
                return false;
            }

            if (IsSurfaceClimbable(hit.normal))
            {
                float continuationAngle = Vector3.Angle(_currentClimbNormal, hit.normal);
                if (continuationAngle <= allowedVerticalContinuationAngle)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplySample(ClimbSurfaceSample sample)
        {
            float normalLerpT = 1f - Mathf.Exp(-_climbNormalLerpSpeed * Time.deltaTime);
            _currentClimbNormal = Vector3.Slerp(_currentClimbNormal, sample.AverageNormal, normalLerpT).normalized;
            _player.ApplyClimbFacing(_currentClimbNormal, _climbRotationLerpSpeed, false);
            SnapToSurface(sample.AnchorPoint, _currentClimbNormal);
        }

        private void SnapToSurface(Vector3 hitPoint, Vector3 surfaceNormal)
        {
            _player.PlayerTransform.position = GetSnappedRootPosition(hitPoint, surfaceNormal);
        }

        private Vector3 GetSnappedRootPosition(Vector3 hitPoint, Vector3 surfaceNormal)
        {
            Vector3 desiredCenter = hitPoint + (surfaceNormal.normalized * (_player.CharacterController.radius + _climbSurfaceOffset));
            return desiredCenter - _player.CharacterController.center;
        }

        private Vector3 GetWallUp(Vector3 wallNormal)
        {
            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, wallNormal);
            return wallUp.sqrMagnitude > 0.0001f ? wallUp.normalized : Vector3.up;
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

        private bool TryGetClosestSurfaceHit(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit closestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, _climbableLayers, QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            closestHit = default;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closestHit = hits[i];
                }
            }

            return closestDistance < float.MaxValue;
        }

        private bool IsOwnedCollider(Collider collider)
        {
            return collider != null && collider.transform.IsChildOf(_player.PlayerTransform);
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
