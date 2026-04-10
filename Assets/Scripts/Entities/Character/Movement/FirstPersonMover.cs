using NightDriver.Character.Animation;
using UnityEngine;
using NightDriver.Character;
using NightDriver.Dialogue;
using NightDriver.UI;

namespace NightDriver.Character.Movement
{
    /// <summary>
    /// 1인칭 캐릭터 이동 컴포넌트.
    ///
    /// CharacterConfig SO를 할당하면 인스펙터 없이 값을 공유할 수 있습니다.
    /// SO를 비워두면 인스펙터 직접 설정값을 사용합니다.
    /// </summary>
    public sealed class FirstPersonMover : MonoBehaviour
    {
        [Header("Config (SO — 비워두면 아래 직접 설정값 사용)")]
        [SerializeField] private CharacterConfig config;

        [Header("이동 (Config SO 미할당 시 사용)")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

        [Header("중력")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("References (optional)")]
        [Tooltip("비워두면 transform을 사용합니다. Yaw가 적용되는 루트를 넣으세요.")]
        [SerializeField] private Transform moveOrientation;

        [Header("Input Fallback")]
        [Tooltip("Input Manager 축(Horizontal/Vertical)이 0으로 고정될 때 WASD 키 입력으로 대체합니다.")]
        [SerializeField] private bool fallbackToWasdKeys = true;

        [Header("Animation (optional)")]
        [Tooltip("비우면 자식에서 Animator를 찾습니다. 같은 오브젝트에 PlayerLocomotionAnimator가 있으면 여기서는 Speed를 건드리지 않습니다.")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedFloatParameter = "Speed";
        [Tooltip("SetFloat dampTime. 0이면 즉시 반영.")]
        [SerializeField] private float speedDampTime = 0.12f;
        [Tooltip("수평 속도가 이 값(m/s) 미만이면 Speed를 0으로 스냅합니다.")]
        [SerializeField] private float animatorIdleSpeedThreshold = 0.06f;

        // ─────────────────────────────────────────────
        // 런타임 캐시 (SO 또는 직접 설정값 중 하나로 초기화됨)
        private float _moveSpeed;
        private float _sprintMultiplier;
        private KeyCode _sprintKey;
        private float _gravity;
        private float _groundedStickVelocity;

        private CharacterController controller;
        private float verticalVelocity;

        private int _speedHash;
        private bool _skipAnimatorDrive;

        /// <summary>애니메이션 Speed 정규화용 최대 수평 속도(달리기 기준, m/s).</summary>
        public float EffectiveMaxHorizontalSpeed => _moveSpeed * _sprintMultiplier;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            ApplyConfig();
            InitController();
            if (moveOrientation == null) moveOrientation = transform;

            _speedHash = Animator.StringToHash(speedFloatParameter);
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            _skipAnimatorDrive = GetComponent<PlayerLocomotionAnimator>() != null;
        }

        private void Update()
        {
            // CharacterController가 비활성화된 상태(예: 차량 탑승 중)에서는 Move를 호출하면 안 됩니다.
            if (controller == null)
                InitController();
            if (controller == null || !controller.enabled)
            {
                PushAnimatorSpeed(0f);
                return;
            }

            // 폰 UI 열림, 대화 중, 차량 탑승(좌석) 시에는 이동 입력을 잠급니다.
            if (PhoneUIController.IsAnyPhoneVisible
                || (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
                || PlayerControlLock.VehicleSeated)
            {
                PushAnimatorSpeed(0f);
                // 좌석 탑승 중에는 컨트롤러가 꺼져 있을 수 있어 중력 처리도 스킵합니다.
                if (!PlayerControlLock.VehicleSeated)
                {
                    // 중력/접지 처리는 유지해서 물리적으로 뜨지 않게 합니다.
                    if (controller.isGrounded && verticalVelocity < 0f)
                        verticalVelocity = _groundedStickVelocity;
                    verticalVelocity += _gravity * Time.deltaTime;
                    controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
                }
                return;
            }

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            if (fallbackToWasdKeys && Mathf.Abs(x) < 0.0001f && Mathf.Abs(z) < 0.0001f)
            {
                x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
                z = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
            }
            var input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f) input.Normalize();

            float speed = _moveSpeed * (Input.GetKey(_sprintKey) ? _sprintMultiplier : 1f);

            var forward = moveOrientation.forward;
            var right   = moveOrientation.right;
            forward.y = 0f;
            right.y   = 0f;
            forward.Normalize();
            right.Normalize();

            var move = (right * input.x + forward * input.z) * speed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = _groundedStickVelocity;
            }

            verticalVelocity += _gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
            PushAnimatorSpeedFromHorizontalVelocity();
        }

        private void PushAnimatorSpeedFromHorizontalVelocity()
        {
            if (_skipAnimatorDrive || animator == null || controller == null || !controller.enabled)
                return;

            var v = controller.velocity;
            float horizontal = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (horizontal < animatorIdleSpeedThreshold)
            {
                PushAnimatorSpeed(0f);
                return;
            }

            float maxRef = _moveSpeed * _sprintMultiplier;
            float n = maxRef > 0.0001f ? Mathf.Clamp01(horizontal / maxRef) : 0f;
            PushAnimatorSpeed(n);
        }

        private void PushAnimatorSpeed(float value)
        {
            if (_skipAnimatorDrive || animator == null)
                return;
            // damp로 0에 수렴시키면 부동소수점 잔여(예: 8e-35)가 남음 → 정지 시 즉시 0으로 고정
            if (value <= 0f || value < 1e-4f)
            {
                animator.SetFloat(_speedHash, 0f);
                return;
            }
            if (speedDampTime > 0f)
                animator.SetFloat(_speedHash, value, speedDampTime, Time.deltaTime);
            else
                animator.SetFloat(_speedHash, value);
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// SO가 있으면 SO 값을, 없으면 인스펙터 직접 설정값을 런타임 필드에 복사합니다.
        /// </summary>
        private void ApplyConfig()
        {
            if (config != null)
            {
                _moveSpeed              = config.moveSpeed;
                _sprintMultiplier       = config.sprintMultiplier;
                _sprintKey              = config.sprintKey;
                _gravity                = config.gravity;
                _groundedStickVelocity  = config.groundedStickVelocity;
            }
            else
            {
                _moveSpeed              = moveSpeed;
                _sprintMultiplier       = sprintMultiplier;
                _sprintKey              = sprintKey;
                _gravity                = gravity;
                _groundedStickVelocity  = groundedStickVelocity;
            }
        }

        private void InitController()
        {
            controller = GetComponent<CharacterController>();
            if (controller != null) return;

            controller = gameObject.AddComponent<CharacterController>();
            if (config != null)
            {
                controller.height      = config.controllerHeight;
                controller.radius      = config.controllerRadius;
                controller.center      = config.controllerCenter;
                controller.slopeLimit  = config.slopeLimit;
                controller.stepOffset  = config.stepOffset;
            }
            else
            {
                controller.height      = 2.0f;
                controller.radius      = 0.35f;
                controller.center      = new Vector3(0f, 1.0f, 0f);
                controller.slopeLimit  = 45f;
                controller.stepOffset  = 0.3f;
            }
        }
    }
}
