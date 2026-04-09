using NightDriver.Character;
using NightDriver.Character.Movement;
using NightDriver.Dialogue;
using NightDriver.UI;
using UnityEngine;

namespace NightDriver.Character.Animation
{
    [AddComponentMenu("NightDriver/Character/Player Locomotion Animator")]
    public sealed class PlayerLocomotionAnimator : MonoBehaviour
    {
        [Header("References (optional)")]
        [SerializeField] private Animator animator;

        [Header("Animator Parameters")]
        [SerializeField] private string speedFloat = "Speed";
        [Tooltip("Speed 값을 부드럽게 변화시키는 시간(초). 0이면 즉시 반영.")]
        [SerializeField] private float dampTime = 0.08f;

        [Header("Safety")]
        [Tooltip("Animator의 Root Motion이 CharacterController 이동을 덮어쓰는 문제를 방지합니다.")]
        [SerializeField] private bool disableApplyRootMotion = true;

        [Header("Input")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private bool fallbackToWasdKeys = true;

        [Header("Speed Mapping")]
        [Tooltip("CharacterController가 있으면 실제 수평 속도로 Speed를 맞춥니다(FirstPersonMover와 동기화).")]
        [SerializeField] private bool preferCharacterControllerVelocity = true;
        [Tooltip("FirstPersonMover가 없을 때 속도 정규화에 쓰는 최대 수평 속도(m/s).")]
        [SerializeField] private float referenceMaxHorizontalSpeed = 8f;
        [Tooltip("수평 속도가 이 값(m/s) 미만이면 Speed를 0으로 둡니다.")]
        [SerializeField] private float velocityIdleThreshold = 0.06f;

        [Header("Speed Mapping (입력 기반 폴백)")]
        [Tooltip("걷기일 때 Speed 스케일 (기본: 1.0)")]
        [SerializeField] private float walkScale = 0.5f;
        [Tooltip("달리기(Shift)일 때 Speed 스케일 (기본: 1.0)")]
        [SerializeField] private float runScale = 1.0f;
        [Tooltip("Speed 값을 0~1로 제한합니다.")]
        [SerializeField] private bool clamp01 = true;

        private int speedHash;
        private CharacterController _characterController;
        private FirstPersonMover _firstPersonMover;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            speedHash = Animator.StringToHash(speedFloat);

            if (animator != null && disableApplyRootMotion)
                animator.applyRootMotion = false;

            _characterController = GetComponent<CharacterController>();
            _firstPersonMover = GetComponent<FirstPersonMover>();
        }

        private void LateUpdate()
        {
            if (animator == null) return;

            // UI/대화/차량 탑승(좌석) 중에는 locomotion 값을 0으로 둡니다.
            if (PhoneUIController.IsAnyPhoneVisible
                || (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
                || PlayerControlLock.VehicleSeated)
            {
                SetSpeed(0f);
                return;
            }

            if (TrySetSpeedFromCharacterVelocity())
                return;

            float x = Input.GetAxisRaw(horizontalAxis);
            float z = Input.GetAxisRaw(verticalAxis);
            if (fallbackToWasdKeys && Mathf.Abs(x) < 0.0001f && Mathf.Abs(z) < 0.0001f)
            {
                x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
                z = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
            }
            var input = new Vector2(x, z);
            if (input.sqrMagnitude > 1f) input.Normalize();

            bool sprint = Input.GetKey(sprintKey);
            float baseMagnitude = input.magnitude;
            float scaled = baseMagnitude * (sprint ? runScale : walkScale);
            if (clamp01) scaled = Mathf.Clamp01(scaled);

            SetSpeed(scaled);
        }

        /// <summary>
        /// CC 수평 속도를 최대 이동 속도로 나눈 0~1 값으로 Speed를 설정합니다. 적용했으면 true.
        /// </summary>
        private bool TrySetSpeedFromCharacterVelocity()
        {
            if (!preferCharacterControllerVelocity || _characterController == null || !_characterController.enabled)
                return false;

            var v = _characterController.velocity;
            float horizontal = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (horizontal < velocityIdleThreshold)
            {
                SetSpeed(0f);
                return true;
            }

            float maxRef = _firstPersonMover != null
                ? _firstPersonMover.EffectiveMaxHorizontalSpeed
                : referenceMaxHorizontalSpeed;
            float n = maxRef > 0.0001f ? horizontal / maxRef : 0f;
            if (clamp01) n = Mathf.Clamp01(n);
            SetSpeed(n);
            return true;
        }

        private void SetSpeed(float value)
        {
            if (animator == null) return;
            // damp로 0 근처까지 줄이면 Animator에 극소값이 박힘 → 정지는 즉시 0
            if (value <= 0f || value < 1e-4f)
            {
                animator.SetFloat(speedHash, 0f);
                return;
            }
            if (dampTime > 0f)
                animator.SetFloat(speedHash, value, dampTime, Time.deltaTime);
            else
                animator.SetFloat(speedHash, value);
        }
    }
}

