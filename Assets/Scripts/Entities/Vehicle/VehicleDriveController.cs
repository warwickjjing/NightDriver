using System;
using NightDriver.Character;
using NightDriver.Dialogue;
using NightDriver.UI;
using UnityEngine;

namespace NightDriver.Vehicle
{
    /// <summary>
    /// Rigidbody 기반 1인칭 차량 주행 컨트롤러.
    /// - W/S: 전진/후진
    /// - A/D: 좌/우 조향(요)
    /// - 최대 속도: km/h 기준
    /// - 부드러운 가속/감속: 목표 속도로 수렴하는 방식
    ///
    /// 주석은 한국어로 작성.
    /// </summary>
    [AddComponentMenu("NightDriver/Vehicle/Vehicle Drive Controller")]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleDriveController : MonoBehaviour
    {
        [Header("활성 조건")]
        [Tooltip("true면 탑승 상태(VehicleSeated)일 때만 입력을 받습니다.")]
        [SerializeField] private bool onlyWhenSeated = true;

        [Header("입력")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;
        [SerializeField] private KeyCode brakeKey = KeyCode.Space;
        [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;

        [Header("주행 파라미터")]
        [Tooltip("최대 속도 (km/h)")]
        [SerializeField] private float maxSpeedKmh = 60f;
        [Tooltip("전진 가속(목표 속도 추종 강도)")]
        [SerializeField] private float accel = 8f;
        [Tooltip("감속(엑셀을 떼었을 때)")]
        [SerializeField] private float decel = 10f;
        [Tooltip("브레이크(스페이스) 감속")]
        [SerializeField] private float brakeDecel = 18f;
        [Tooltip("조향 속도(도/초)")]
        [SerializeField] private float steerDegreesPerSecond = 90f;
        [Tooltip("후진 시 조향을 반대로 적용할지 (현실적인 후진 핸들링)")]
        [SerializeField] private bool invertSteerWhenReversing = true;
        [Tooltip("Shift 부스트 배수 (전진 시 최대속도/가속 강화)")]
        [SerializeField] private float boostMultiplier = 1.35f;

        [Header("안정화")]
        [Tooltip("무게중심 오프셋. y를 음수로 두면 전복/곤두박질이 줄어듭니다.")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
        [Tooltip("true면 X/Z 회전을 고정해서 아케이드처럼 안정화합니다.")]
        [SerializeField] private bool freezePitchAndRoll = true;
        [Tooltip("true면 매 FixedUpdate마다 월드 수직 기준으로 롤/피치를 0에 가깝게 맞춥니다. Freeze X/Z가 꺼져 있거나 자식 Rigidbody 때문에 차체가 기울 때 보조용입니다.")]
        [SerializeField] private bool snapYawOnlyWorldUpEachFixedStep = false;
        [Tooltip("차량의 실제 전방 기준. 모델 축이 꼬여있으면 여기를 지정하세요.")]
        [SerializeField] private Transform forwardReference;

        [Header("충돌 후 공회전")]
        [Tooltip("Rigidbody Angular Drag가 이보다 작으면 Awake에서 올립니다. 0이면 건드리지 않습니다.")]
        [SerializeField] private float minimumAngularDrag = 1.75f;
        [Tooltip("각속도 상한(rad/s). 낮출수록 충돌 후 공회전이 빨리 멈춥니다. 0이면 Rigidbody 기본값을 유지합니다.")]
        [SerializeField] private float maximumAngularVelocity = 8f;

        [Header("조작감(그립/미끄럼)")]
        [Tooltip("횡(옆) 미끄럼을 줄이는 정도. 0이면 빙판, 1이면 매우 끈적한 그립.")]
        [Range(0f, 1f)]
        [SerializeField] private float lateralGrip = 0.75f;
        [Tooltip("속도가 빠를수록 조향이 둔해지는 정도. 0이면 항상 동일, 1이면 고속에서 크게 둔해짐.")]
        [Range(0f, 1f)]
        [SerializeField] private float highSpeedSteerDamping = 0.6f;

        [Header("경사/코너 보정")]
        [Tooltip("지면 노멀을 이용해 전진 방향을 경사에 맞춰 보정합니다(오르막 대응).")]
        [SerializeField] private bool alignDriveToGround = true;
        [Tooltip("지면 노멀을 얻기 위한 레이캐스트 거리")]
        [SerializeField] private float groundRayDistance = 2.5f;
        [Tooltip("저속에서도 코너를 돌 수 있도록 조향 최소 배율을 보장합니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float minSteerFactor = 0.35f;
        [Tooltip("정지에 가까워도 조향이 되게 할지 (주차/유턴 편의)")]
        [SerializeField] private bool allowSteerNearStop = true;

        [Header("디버그")]
        [SerializeField] private bool drawForwardRay = true;

        public event Action OnTrafficViolation;

        private Rigidbody rb;
        private bool canDrive = true;
        private float targetSpeedMs;
        private float currentSpeedMs;
        private Vector3 baseCenterOfMass;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            baseCenterOfMass = rb.centerOfMass;
            rb.centerOfMass = baseCenterOfMass + centerOfMassOffset;

            if (freezePitchAndRoll)
                rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (minimumAngularDrag > 0f && rb.angularDrag < minimumAngularDrag)
                rb.angularDrag = minimumAngularDrag;
            if (maximumAngularVelocity > 0f)
                rb.maxAngularVelocity = maximumAngularVelocity;
        }

        private void FixedUpdate()
        {
            // UI/대화 중에는 주행 입력을 잠급니다.
            if (PhoneUIController.IsAnyPhoneVisible
                || (DialogueService.Instance != null && DialogueService.Instance.IsRunning))
                return;

            if (onlyWhenSeated && !PlayerControlLock.VehicleSeated)
                return;

            if (!canDrive)
            {
                ApplyVelocityTarget(0f, decel);
                ApplyYawOnlySnapIfNeeded();
                return;
            }

            float throttle = (Input.GetKey(forwardKey) ? 1f : 0f) + (Input.GetKey(backKey) ? -1f : 0f);
            float steer = (Input.GetKey(rightKey) ? 1f : 0f) + (Input.GetKey(leftKey) ? -1f : 0f);
            bool braking = Input.GetKey(brakeKey);
            bool boosting = Input.GetKey(boostKey);

            float maxMs = Mathf.Max(0.1f, maxSpeedKmh / 3.6f);
            float boost = (boosting && throttle > 0.001f) ? Mathf.Max(1f, boostMultiplier) : 1f;

            // 브레이크 우선: W를 누른 채여도 목표 속도를 0으로 강하게 감속
            if (braking)
                targetSpeedMs = 0f;
            else
                targetSpeedMs = throttle * maxMs * boost;

            // 목표 속도로 부드럽게 수렴
            float accelRate = accel * boost;
            float rate = braking ? brakeDecel : (Mathf.Abs(throttle) > 0.001f ? accelRate : decel);
            ApplyVelocityTarget(targetSpeedMs, rate);

            // 조향은 현재 속도가 있을수록 자연스럽게(정지 상태에서 빙글빙글 방지)
            float signedSpeedMs = GetCurrentSpeedMs();
            float speedFactor01 = Mathf.Clamp01(Mathf.Abs(signedSpeedMs) / maxMs);

            // 후진 시 조향 반전(현실적인 후진 핸들링)
            if (invertSteerWhenReversing && signedSpeedMs < -0.2f)
                steer *= -1f;

            // 고속 조향 둔화
            float steerDamp = 1f - (highSpeedSteerDamping * speedFactor01);
            steerDamp = Mathf.Clamp(steerDamp, 0.15f, 1f);

            float steerFactor = Mathf.Max(minSteerFactor, speedFactor01);
            if (!allowSteerNearStop)
                steerFactor = speedFactor01;

            // A/D는 "앞/뒤 입력이 있을 때만" 조향 적용 (정지 상태 제자리 회전 방지)
            if (Mathf.Abs(throttle) <= 0.001f)
                steer = 0f;

            float yaw = steer * steerDegreesPerSecond * steerFactor * steerDamp * Time.fixedDeltaTime;
            if (Mathf.Abs(yaw) > 0.00001f)
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            // 횡(옆) 속도를 줄여서 미끄러짐을 제어 (아케이드 그립)
            ApplyLateralGrip(speedFactor01);

            if (drawForwardRay)
                Debug.DrawRay(transform.position + Vector3.up * 0.3f, GetForward() * 3f, Color.cyan);

            ApplyYawOnlySnapIfNeeded();
        }

        private void ApplyYawOnlySnapIfNeeded()
        {
            if (!snapYawOnlyWorldUpEachFixedStep || rb == null) return;
            if (onlyWhenSeated && !PlayerControlLock.VehicleSeated) return;
            Vector3 e = transform.eulerAngles;
            rb.MoveRotation(Quaternion.Euler(0f, e.y, 0f));
        }

        private void ApplyLateralGrip(float speedFactor01)
        {
            // 속도가 높을수록(=컨트롤이 어려울수록) 약간 더 그립을 주면 조작이 편해집니다.
            float grip = Mathf.Lerp(lateralGrip, 1f, speedFactor01 * 0.35f);
            grip = Mathf.Clamp01(grip);

            Vector3 fwd = GetForward();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x); // fwd에 수직인 평면 right
            right.Normalize();

            Vector3 vel = rb.velocity;
            float lateralSpeed = Vector3.Dot(vel, right);

            // grip=1이면 횡속을 거의 제거, grip=0이면 유지
            float keep = Mathf.Lerp(1f, 0.05f, grip);
            float newLateral = Mathf.MoveTowards(lateralSpeed, 0f, (1f - keep) * 25f * Time.fixedDeltaTime);

            rb.velocity = vel - right * lateralSpeed + right * newLateral;
        }

        private void ApplyVelocityTarget(float targetMs, float rate)
        {
            Vector3 fwd = GetDriveDirection();
            Vector3 vel = rb.velocity;

            float forwardSpeed = Vector3.Dot(vel, fwd);
            float desired = targetMs;
            float newForward = Mathf.MoveTowards(forwardSpeed, desired, rate * Time.fixedDeltaTime);

            // 현재 속도 벡터에서 "전방 성분"만 교체 (옆 미끄러짐은 남김)
            Vector3 lateral = vel - fwd * forwardSpeed;
            rb.velocity = lateral + fwd * newForward;

            currentSpeedMs = Vector3.Dot(rb.velocity, fwd);
        }

        private Vector3 GetDriveDirection()
        {
            // 오르막/내리막에서 앞으로 가려면, 수평이 아니라 "지면을 따라" 힘을 줘야 합니다.
            var rawFwd = forwardReference != null ? forwardReference.forward : transform.forward;
            if (rawFwd.sqrMagnitude < 0.0001f) rawFwd = Vector3.forward;
            rawFwd.Normalize();

            if (!alignDriveToGround)
                return rawFwd;

            // 아래로 레이캐스트해서 지면 노멀을 얻고, 전방 벡터를 지면 평면에 투영
            var origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, groundRayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 projected = Vector3.ProjectOnPlane(rawFwd, hit.normal);
                if (projected.sqrMagnitude > 0.0001f)
                    return projected.normalized;
            }

            return rawFwd;
        }

        private Vector3 GetForward()
        {
            var fwd = forwardReference != null ? forwardReference.forward : transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) return Vector3.forward;
            return fwd.normalized;
        }

        /// <summary>
        /// 외부에서 운전 가능/불가를 제어합니다. (대화 중 잠금 등)
        /// </summary>
        public void SetCanDrive(bool value) => canDrive = value;

        /// <summary>
        /// 현재 속도(km/h)를 반환합니다. (사운드 피치 연동용)
        /// </summary>
        public float GetCurrentSpeed()
        {
            return Mathf.Abs(GetCurrentSpeedMs()) * 3.6f;
        }

        private float GetCurrentSpeedMs()
        {
            var fwd = GetForward();
            return Vector3.Dot(rb.velocity, fwd);
        }

        /// <summary>
        /// 신호 위반 등 교통 위반 이벤트를 외부로 알립니다.
        /// 감지 로직은 별도 컴포넌트/트리거에서 이 함수를 호출하세요.
        /// </summary>
        public void ReportTrafficViolation()
        {
            OnTrafficViolation?.Invoke();
        }
    }
}

