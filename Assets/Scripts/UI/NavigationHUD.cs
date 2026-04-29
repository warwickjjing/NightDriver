using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NightDriver.Core;

namespace NightDriver.UI
{
    /// <summary>
    /// 드라이빙 모드 전용 HUD 화살표 컴포넌트.
    /// Screen Space Overlay Canvas 하위에 배치하세요.
    ///
    /// [거리 표시 규칙]
    ///   10m 이내   → "목적지 부근입니다."
    ///   100m 이내  → "XXm 앞"
    ///   그 외      → "X.Xkm"
    ///
    /// [공포 연출]
    ///   SetGlitchOverride(true, angle) — 화살표 방향을 강제로 덮어씁니다 (깜빡임 용).
    ///   ResetGlitch()                  — 실제 방향으로 복귀합니다.
    ///   ShowArrivalMessage(text)       — 거리 텍스트 대신 임시 메시지를 표시합니다.
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Navigation HUD")]
    public sealed class NavigationHUD : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector

        [Header("HUD 요소")]
        [Tooltip("방향을 나타낼 화살표 이미지 (RectTransform이 회전됩니다)")]
        [SerializeField] private RectTransform arrowImage;

        [Tooltip("남은 거리 / 도착 메시지를 표시할 TMP 텍스트")]
        [SerializeField] private TMP_Text distanceText;

        [Tooltip("HUD 전체 루트 오브젝트. null이면 arrowImage와 distanceText를 각각 켜고 끕니다.")]
        [SerializeField] private GameObject hudRoot;

        [Header("플레이어")]
        [Tooltip("비워두면 Camera.main 위치를 원점으로 사용합니다.")]
        [SerializeField] private Transform playerTransform;

        [Header("거리 설정")]
        [Tooltip("이 거리(m) 이내이면 '목적지 부근입니다.' 표시 후 HUD 숨김")]
        [SerializeField] private float nearThreshold = 10f;

        [Tooltip("이 거리(m) 이내이면 'XXm 앞' 표시 (nearThreshold 초과 구간)")]
        [SerializeField] private float midThreshold = 100f;

        [Header("Night (선택)")]
        [Tooltip("비워두면 GameManager.Instance.NightManager를 사용합니다. 일차가 바뀔 때 HUD를 끕니다.")]
        [SerializeField] private NightManager nightManager;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private Transform _destination;
        private Camera    _mainCamera;

        // 글리치 오버라이드
        private bool  _glitchActive;
        private float _glitchAngle;

        // 도착 메시지 임시 표시
        private bool   _arrivalMessageActive;
        private string _arrivalMessageText;
        private float  _arrivalMessageEndTime;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;
            SetHUDVisible(false);

            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
        }

        private void OnEnable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged += HandleDayChanged;
        }

        private void OnDisable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged -= HandleDayChanged;
        }

        private void LateUpdate()
        {
            // 도착 메시지 타임아웃 처리
            if (_arrivalMessageActive && Time.time >= _arrivalMessageEndTime)
            {
                _arrivalMessageActive = false;
                // 메시지가 끝나도 목적지가 없으면 그냥 끔
                if (_destination == null)
                {
                    SetHUDVisible(false);
                    return;
                }
            }

            if (_destination == null)
            {
                SetHUDVisible(false);
                return;
            }

            // 카메라 우선 원점 (VehicleSeatInteraction이 카메라만 이동하므로)
            if (_mainCamera == null) _mainCamera = Camera.main;
            Vector3 origin = _mainCamera != null
                ? _mainCamera.transform.position
                : (playerTransform != null ? playerTransform.position : Vector3.zero);

            float distance = Vector3.Distance(origin, _destination.position);

            // 도착 판정 (nearThreshold 이내)
            if (distance <= nearThreshold && !_arrivalMessageActive)
            {
                // 일반 도착 시에는 그냥 숨김
                SetHUDVisible(false);
                return;
            }

            SetHUDVisible(true);

            // ── 화살표 방향 계산 ─────────────────────────────────────────────
            UpdateArrow(origin);

            // ── 거리 텍스트 ──────────────────────────────────────────────────
            UpdateDistanceText(distance);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API

        /// <summary>
        /// 목적지를 변경합니다. null을 전달하면 HUD를 숨깁니다.
        /// </summary>
        public void SetDestination(Transform target)
        {
            _destination = target;
            _arrivalMessageActive = false;
            if (_destination == null)
                SetHUDVisible(false);
        }

        /// <summary>현재 목적지 Transform을 반환합니다.</summary>
        public Transform GetDestination() => _destination;

        // ─────────────────────────────────────────────────────────────────────
        // 공포 연출 API

        /// <summary>
        /// 화살표 방향을 강제 오버라이드합니다 (글리치 연출용).
        /// active=false이면 실제 방향으로 돌아갑니다.
        /// </summary>
        public void SetGlitchOverride(bool active, float forcedAngleDeg)
        {
            _glitchActive = active;
            _glitchAngle  = forcedAngleDeg;

            // 화살표 게임오브젝트 자체를 켜고 끄는 방식으로 깜빡임 구현
            if (arrowImage != null)
                arrowImage.gameObject.SetActive(active ? true : (_destination != null));
        }

        /// <summary>
        /// 글리치를 해제하고 실제 방향으로 복귀합니다.
        /// </summary>
        public void ResetGlitch()
        {
            _glitchActive = false;
            if (arrowImage != null && _destination != null)
                arrowImage.gameObject.SetActive(true);
        }

        /// <summary>
        /// 거리 텍스트 대신 임시 메시지를 3초간 표시합니다.
        /// (가짜 목적지 도착 연출 등에 사용)
        /// </summary>
        public void ShowArrivalMessage(string message, float displaySeconds = 3f)
        {
            _arrivalMessageActive  = true;
            _arrivalMessageText    = message;
            _arrivalMessageEndTime = Time.time + displaySeconds;

            if (distanceText != null)
                distanceText.text = message;

            SetHUDVisible(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 화살표 업데이트

        private void UpdateArrow(Vector3 origin)
        {
            if (arrowImage == null || _mainCamera == null) return;

            // 글리치 오버라이드 중이면 강제 각도 적용
            if (_glitchActive)
            {
                arrowImage.localRotation = Quaternion.Euler(0f, 0f, _glitchAngle);
                return;
            }

            Vector3 dirWorld = _destination.position - origin;
            dirWorld.y = 0f; // 수평 방향만 사용

            Vector3 camForward = _mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = _mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            float dotForward = Vector3.Dot(dirWorld.normalized, camForward);
            float dotRight   = Vector3.Dot(dirWorld.normalized, camRight);

            // atan2로 화면 회전각 계산 (위쪽 = 전방 = 0도)
            float angle = Mathf.Atan2(dotRight, dotForward) * Mathf.Rad2Deg;
            arrowImage.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 거리 텍스트

        private void UpdateDistanceText(float distance)
        {
            if (distanceText == null) return;

            // 임시 도착 메시지 표시 중이면 덮어쓰지 않음
            if (_arrivalMessageActive)
            {
                distanceText.text = _arrivalMessageText;
                return;
            }

            if (distance <= nearThreshold)
            {
                // nearThreshold 이내 — 도착 부근 메시지
                distanceText.text = "목적지 부근입니다.";
            }
            else if (distance <= midThreshold)
            {
                // 100m 이내 — "XXm 앞"
                distanceText.text = $"{Mathf.RoundToInt(distance)}m 앞";
            }
            else
            {
                // 그 외 — "X.Xkm"
                float km = distance / 1000f;
                distanceText.text = $"{km:F1}km";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 가시성

        private void SetHUDVisible(bool visible)
        {
            // hudRoot가 있으면 그것만 켜고 끔 (깔끔한 방식)
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible);
                return;
            }

            // 없으면 개별 요소 토글
            if (arrowImage != null)
                arrowImage.gameObject.SetActive(visible);
            if (distanceText != null)
                distanceText.gameObject.SetActive(visible);
        }

        private void HandleDayChanged(int _)
        {
            // 다음 날로 넘어갈 때 이전 날 HUD를 끕니다.
            SetDestination(null);
        }
    }
}
