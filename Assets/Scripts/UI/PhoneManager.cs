using NightDriver.Character;
using NightDriver.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.UI
{
    /// <summary>
    /// 폰 패널의 열기/닫기를 통합 관리합니다.
    ///
    /// [기능]
    ///   - E키 토글 (운전 중·대화 중 차단)
    ///   - 폰 오브젝트(씬에 배치된 폰 모델) 클릭으로 열기
    ///   - SetCanOpenPhone(bool) — 외부에서 열기 권한 제어
    ///   - 콜 알림 뱃지(!) 표시 ON/OFF
    ///
    /// [Inspector 배치]
    ///   씬 어딘가(보통 UICanvas 루트)에 하나만 배치하세요.
    ///   PhonePanel은 아래로 숨겨진 상태를 기본으로 하며,
    ///   SmoothDamp로 슬라이드 인/아웃됩니다.
    ///
    /// 기존 <see cref="PhoneUIController"/>와 역할이 겹치므로,
    /// 새 씬에서는 PhoneManager를 사용하고 PhoneUIController는 제거하거나 비활성화하세요.
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Phone Manager")]
    public sealed class PhoneManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 싱글톤

        public static PhoneManager Instance { get; private set; }

        /// <summary>현재 폰이 열려 있는지 여부.</summary>
        public static bool IsPhoneOpen { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector

        [Header("폰 패널 슬라이드")]
        [Tooltip("슬라이드 대상 RectTransform")]
        [SerializeField] private RectTransform phonePanel;

        [Tooltip("닫힌 상태의 anchoredPosition.y")]
        [SerializeField] private float hiddenAnchoredY = -1200f;

        [Tooltip("열린 상태의 anchoredPosition.y")]
        [SerializeField] private float shownAnchoredY = 0f;

        [Tooltip("SmoothDamp smoothTime")]
        [SerializeField] private float slideSmoothTime = 0.2f;

        [Header("입력")]
        [Tooltip("폰 열기/닫기 토글 키 (기본 E)")]
        [SerializeField] private KeyCode toggleKey = KeyCode.E;

        [Header("씬 폰 오브젝트 클릭 (선택)")]
        [Tooltip("씬에 배치된 3D 폰 모델에 달린 버튼. 클릭 시 OpenPhone() 호출.")]
        [SerializeField] private Button phoneObjectButton;

        [Header("뱃지")]
        [Tooltip("폰 아이콘/상단에 표시할 알림 뱃지(!) 오브젝트")]
        [SerializeField] private GameObject badgeDot;

        [Tooltip("PhoneCallApp의 뱃지도 함께 동기화합니다.")]
        [SerializeField] private PhoneCallApp callApp;

        [Header("화면 전환")]
        [Tooltip("폰 내부 화면 전환 컴포넌트. 비우면 자동 탐색합니다.")]
        [SerializeField] private PhoneScreenManager screenManager;

        [Header("상태 표시 (선택)")]
        [Tooltip("'운전 중 사용 불가' 안내 텍스트 (선택)")]
        [SerializeField] private TMP_Text unavailableHintText;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private bool _canOpenPhone = true;
        private Vector2 _velocity;
        private Vector2 _targetPos;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 시작 시 패널 숨김 상태로 초기화
            if (phonePanel != null)
            {
                Vector2 pos = phonePanel.anchoredPosition;
                pos.y = hiddenAnchoredY;
                phonePanel.anchoredPosition = pos;
                _targetPos = pos;
            }

            // 씬 폰 오브젝트 버튼 연결
            if (phoneObjectButton != null)
                phoneObjectButton.onClick.AddListener(OpenPhone);

            // 안내 텍스트 초기 숨김
            if (unavailableHintText != null)
                unavailableHintText.gameObject.SetActive(false);

            // PhoneScreenManager 자동 탐색
            if (screenManager == null)
                screenManager = GetComponentInChildren<PhoneScreenManager>(true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsPhoneOpen = false;
            }
        }

        private void Update()
        {
            // ── 슬라이드 애니메이션 ──
            if (phonePanel != null)
            {
                phonePanel.anchoredPosition = Vector2.SmoothDamp(
                    phonePanel.anchoredPosition,
                    _targetPos,
                    ref _velocity,
                    slideSmoothTime);
            }

            // ── 키 입력 토글 ──
            if (!Input.GetKeyDown(toggleKey)) return;

            if (IsPhoneOpen)
            {
                ClosePhone();
                return;
            }

            // 열기 시도
            if (!CanOpen())
            {
                // 운전 중 힌트 잠깐 표시 (선택)
                ShowUnavailableHint();
                return;
            }

            OpenPhone();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API

        /// <summary>폰을 엽니다.</summary>
        public void OpenPhone()
        {
            if (!CanOpen()) return;

            IsPhoneOpen = true;

            // 커서 잠금 해제 (UI 상호작용 위해)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SetPanelTarget(shownAnchoredY);

            // 홈 화면부터 시작 (ShowHome은 Awake에서 이미 세팅돼 있음)
            // CallNotificationSystem에서 콜이 있으면 콜 앱으로 자동 전환합니다.
            CallNotificationSystem.Instance?.OnPhoneOpened();
        }

        /// <summary>폰을 닫습니다.</summary>
        public void ClosePhone()
        {
            IsPhoneOpen = false;

            // 커서 다시 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetPanelTarget(hiddenAnchoredY);

            // 닫을 때 홈 화면으로 초기화 → 다음에 열 때 홈부터 시작
            screenManager?.OnPhoneClosed();
        }

        /// <summary>
        /// 폰 열기 권한을 설정합니다.
        /// 운전 중에는 <c>false</c>, 도보 중에는 <c>true</c>로 설정하세요.
        /// </summary>
        public void SetCanOpenPhone(bool canOpen)
        {
            _canOpenPhone = canOpen;

            // 운전 중 권한이 꺼지면 열려있는 폰을 강제로 닫습니다.
            if (!canOpen && IsPhoneOpen)
                ClosePhone();
        }

        /// <summary>
        /// 콜 알림 뱃지(!) 표시 여부를 설정합니다.
        /// PhoneCallApp의 뱃지도 함께 동기화합니다.
        /// </summary>
        public void SetCallBadge(bool visible)
        {
            if (badgeDot != null)
                badgeDot.SetActive(visible);

            // PhoneCallApp의 앱 아이콘 뱃지도 함께 제어
            callApp?.SetBadgeVisible(visible);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부

        /// <summary>현재 폰을 열 수 있는지 확인합니다.</summary>
        private bool CanOpen()
        {
            // 열기 권한이 꺼진 경우 (운전 중 등)
            if (!_canOpenPhone) return false;

            // 차량 탑승 중 (PlayerControlLock 기반)
            if (PlayerControlLock.VehicleSeated) return false;

            // 대화 실행 중
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
                return false;

            return true;
        }

        private void SetPanelTarget(float anchoredY)
        {
            if (phonePanel == null) return;

            Vector2 pos = _targetPos;
            pos.y = anchoredY;
            _targetPos = pos;
        }

        private void ShowUnavailableHint()
        {
            if (unavailableHintText == null) return;

            // 운전 중 안내
            if (PlayerControlLock.VehicleSeated)
                unavailableHintText.text = "운전 중에는 사용할 수 없습니다.";
            else if (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
                unavailableHintText.text = "대화 중에는 사용할 수 없습니다.";
            else
                unavailableHintText.text = "지금은 사용할 수 없습니다.";

            unavailableHintText.gameObject.SetActive(true);

            // 2초 후 자동 숨김
            CancelInvoke(nameof(HideUnavailableHint));
            Invoke(nameof(HideUnavailableHint), 2f);
        }

        private void HideUnavailableHint()
        {
            if (unavailableHintText != null)
                unavailableHintText.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            IsPhoneOpen = false;
        }
    }
}
