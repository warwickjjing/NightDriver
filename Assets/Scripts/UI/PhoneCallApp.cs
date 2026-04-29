using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NightDriver.Client;

namespace NightDriver.UI
{
    /// <summary>
    /// 폰 패널 내부의 콜 앱 화면.
    /// UI 요소는 Inspector에서 연결하고, 이 스크립트는 데이터 주입만 담당합니다.
    ///
    /// [연결 흐름]
    ///   CallNotificationSystem.ReceiveCall(def) → SetCallInfo(def) → 텍스트 주입
    ///   수락 버튼 클릭 → OnClickAccept() → CallNotificationSystem.AcceptCall()
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Phone Call App")]
    public sealed class PhoneCallApp : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 앱 루트

        [Header("앱 루트")]
        [Tooltip("콜 앱 전체 패널 오브젝트. SetActive로 켜고 끕니다.")]
        [SerializeField] private GameObject appRoot;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 상단 카드 (손님 정보)

        [Header("상단 카드 — 손님 정보")]
        [Tooltip("손님 이름 텍스트. (예: 손님 #3)")]
        [SerializeField] private TMP_Text clientNameText;

        [Tooltip("픽업 위치 전체 주소 텍스트. (예: B구역 · 동양아파트 지하주차장 B2)")]
        [SerializeField] private TMP_Text pickupAddressFullText;

        [Tooltip("예상 요금 텍스트. (예: ₩18,000 ~)")]
        [SerializeField] private TMP_Text estimatedFareText;

        [Tooltip("요금 표시 포맷. {0} = 금액 숫자. (예: '₩{0:N0} ~')")]
        [SerializeField] private string fareFormat = "₩{0:N0} ~";

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 경로 섹션 (현재 위치 → 픽업 위치)

        [Header("경로 섹션 — 현재 위치 → 픽업 위치")]
        [Tooltip("플레이어 현재 위치 지역명 텍스트. (예: A구역 · 도심 중심부)\n" +
                 "SetCurrentArea(string)로 코드에서 갱신할 수 있습니다.")]
        [SerializeField] private TMP_Text currentAreaText;

        [Tooltip("픽업 위치 약식 텍스트. (예: 지하주차장 B2)")]
        [SerializeField] private TMP_Text pickupAddressShortText;

        [Tooltip("도보 소요 시간 텍스트. (예: 도보 약 2분)")]
        [SerializeField] private TMP_Text walkingTimeText;

        [Tooltip("'도보' 접두사 포맷. {0} = ClientDefinition.walkingTimeLabel")]
        [SerializeField] private string walkingTimeFormat = "도보 {0}";

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 독백 박스

        [Header("독백 박스 (도현 내면 독백)")]
        [Tooltip("독백 텍스트. CallNotificationSystem이 수락 직후 값을 주입합니다.")]
        [SerializeField] private TMP_Text monologueText;

        [Tooltip("독백 박스 전체 오브젝트. 독백이 없으면 숨깁니다.")]
        [SerializeField] private GameObject monologueBox;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 버튼

        [Header("버튼")]
        [Tooltip("수락 버튼. OnClick 이벤트는 Awake()에서 자동 연결됩니다.")]
        [SerializeField] private Button acceptButton;

        [Tooltip("거절 버튼. 클릭 시 폰을 닫습니다. (선택)")]
        [SerializeField] private Button rejectButton;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 배차 완료 피드백

        [Header("배차 완료 피드백")]
        [Tooltip("수락 후 잠깐 표시할 '배차 완료' 텍스트 오브젝트.")]
        [SerializeField] private GameObject acceptedFeedbackObject;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 뱃지

        [Header("앱 아이콘 뱃지")]
        [Tooltip("콜 알림 뱃지(!) 오브젝트. SetBadgeVisible()로 제어합니다.")]
        [SerializeField] private GameObject badgeObject;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private ClientDefinition _currentDef;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // 수락 버튼 — Awake에서 자동 연결 (Inspector OnClick 불필요)
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(OnClickAccept);
            }

            // 거절 버튼 — 폰 닫기
            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveAllListeners();
                rejectButton.onClick.AddListener(OnClickReject);
            }

            // 초기 숨김 처리
            if (acceptedFeedbackObject != null)
                acceptedFeedbackObject.SetActive(false);

            if (monologueBox != null)
                monologueBox.SetActive(false);

            SetAppActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 데이터 주입

        /// <summary>
        /// ClientDefinition의 표시 데이터를 UI에 주입합니다.
        /// CallNotificationSystem.ReceiveCall()에서 자동으로 호출됩니다.
        /// </summary>
        public void SetCallInfo(ClientDefinition def)
        {
            _currentDef = def;

            if (def == null)
            {
                ClearTexts();
                return;
            }

            // ── 손님 이름 ──
            if (clientNameText != null)
                clientNameText.text = string.IsNullOrWhiteSpace(def.displayName)
                    ? (string.IsNullOrWhiteSpace(def.clientId) ? "손님" : def.clientId)
                    : def.displayName;

            // ── 픽업 위치 전체 주소 (카드) ──
            if (pickupAddressFullText != null)
                pickupAddressFullText.text = string.IsNullOrWhiteSpace(def.pickupAddressFull)
                    ? def.spawnPointId
                    : def.pickupAddressFull;

            // ── 예상 요금 ──
            if (estimatedFareText != null)
                estimatedFareText.text = string.Format(fareFormat, def.estimatedFareWon);

            // ── 픽업 위치 약식 (경로 섹션) ──
            if (pickupAddressShortText != null)
                pickupAddressShortText.text = string.IsNullOrWhiteSpace(def.pickupAddressShort)
                    ? def.pickupAddressFull
                    : def.pickupAddressShort;

            // ── 도보 시간 ──
            if (walkingTimeText != null)
                walkingTimeText.text = string.IsNullOrWhiteSpace(def.walkingTimeLabel)
                    ? string.Empty
                    : string.Format(walkingTimeFormat, def.walkingTimeLabel);

            // ── 독백 (손님별 독백이 있을 때만 미리 표시) ──
            SetMonologue(def.driverMonologue);
        }

        /// <summary>
        /// 플레이어의 현재 위치 지역명을 갱신합니다.
        /// 위치 감지 시스템 등 외부에서 호출하세요.
        /// </summary>
        public void SetCurrentArea(string areaLabel)
        {
            if (currentAreaText != null)
                currentAreaText.text = areaLabel;
        }

        /// <summary>
        /// 독백 텍스트를 주입합니다.
        /// text가 비어 있으면 독백 박스를 숨깁니다.
        /// </summary>
        public void SetMonologue(string text)
        {
            bool hasText = !string.IsNullOrWhiteSpace(text);

            if (monologueBox != null)
                monologueBox.SetActive(hasText);

            if (monologueText != null)
                monologueText.text = hasText ? text : string.Empty;
        }

        /// <summary>콜 앱 화면을 활성화합니다.</summary>
        public void ShowCallScreen()
        {
            SetAppActive(true);
        }

        /// <summary>콜 앱 화면을 비활성화합니다.</summary>
        public void HideCallScreen()
        {
            SetAppActive(false);
        }

        /// <summary>
        /// 수락 완료 피드백("배차 완료")을 표시하고 수락 버튼을 비활성화합니다.
        /// CallNotificationSystem.AcceptCall()에서 호출됩니다.
        /// </summary>
        public void ShowAcceptedFeedback()
        {
            if (acceptButton != null)
                acceptButton.interactable = false;

            if (rejectButton != null)
                rejectButton.interactable = false;

            if (acceptedFeedbackObject != null)
                acceptedFeedbackObject.SetActive(true);
        }

        /// <summary>앱을 초기화합니다 (다음 콜 수신 전 리셋).</summary>
        public void ResetApp()
        {
            _currentDef = null;
            ClearTexts();

            if (acceptButton != null)   acceptButton.interactable   = true;
            if (rejectButton != null)   rejectButton.interactable   = true;
            if (acceptedFeedbackObject != null) acceptedFeedbackObject.SetActive(false);
            if (monologueBox != null)   monologueBox.SetActive(false);

            SetAppActive(false);
        }

        /// <summary>콜 알림 뱃지(!) 표시 여부를 설정합니다.</summary>
        public void SetBadgeVisible(bool visible)
        {
            if (badgeObject != null)
                badgeObject.SetActive(visible);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부

        private void OnClickAccept()
        {
            CallNotificationSystem.Instance?.AcceptCall();
        }

        private void OnClickReject()
        {
            // 거절: 폰만 닫습니다 (추후 거절 로직 확장 가능)
            PhoneManager.Instance?.ClosePhone();
        }

        private void SetAppActive(bool active)
        {
            if (appRoot != null)
                appRoot.SetActive(active);
        }

        private void ClearTexts()
        {
            if (clientNameText != null)         clientNameText.text         = string.Empty;
            if (pickupAddressFullText != null)   pickupAddressFullText.text  = string.Empty;
            if (estimatedFareText != null)       estimatedFareText.text      = string.Empty;
            if (pickupAddressShortText != null)  pickupAddressShortText.text = string.Empty;
            if (walkingTimeText != null)         walkingTimeText.text        = string.Empty;
            if (currentAreaText != null)         currentAreaText.text        = string.Empty;
        }
    }
}
