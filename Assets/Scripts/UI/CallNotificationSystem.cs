using System;
using System.Collections;
using NightDriver.Client;
using NightDriver.Core;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 콜 수신 흐름 전체를 관리하는 오케스트레이터.
    ///
    /// [흐름]
    ///   1) ReceiveCall(def)     — 알림음 재생 + 배너 표시 + 뱃지 ON
    ///   2) PhoneManager.Open() — 폰 열기 → 콜 앱 화면으로 자동 전환 → 손님 정보 표시
    ///   3) AcceptCall()         — 배차 완료 + 뱃지 OFF + 폰 닫기 + NaviManager.SetPickupMode()
    ///
    /// [공포 연출]
    ///   TriggerGlitchRingtone() — 알림음 피치 낮추고 끊기게 재생
    ///
    /// Inspector에서 PhoneManager / CallNotificationBanner / PhoneCallApp을 연결하세요.
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Call Notification System")]
    public sealed class CallNotificationSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 싱글톤

        public static CallNotificationSystem Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector 참조

        [Header("참조 — UI 컴포넌트")]
        [Tooltip("폰 열기/닫기 통합 컨트롤러")]
        [SerializeField] private PhoneManager phoneManager;

        [Tooltip("화면 상단 슬라이드다운 배너")]
        [SerializeField] private CallNotificationBanner banner;

        [Tooltip("폰 내부 콜 앱 화면")]
        [SerializeField] private PhoneCallApp callApp;

        [Header("참조 — 게임 시스템")]
        [Tooltip("비우면 씬에서 자동 탐색합니다.")]
        [SerializeField] private NavigationManager naviManager;

        [Tooltip("비우면 씬에서 자동 탐색합니다.")]
        [SerializeField] private CallFlowController callFlow;

        [Header("오디오")]
        [Tooltip("콜 수신 알림음 AudioSource")]
        [SerializeField] private AudioSource ringtoneSource;

        [Tooltip("알림음 클립. AudioSource에 미리 설정돼 있어도 되고, 여기서 별도로 지정해도 됩니다.")]
        [SerializeField] private AudioClip ringtoneClip;

        [Header("글리치 린턴 설정")]
        [Tooltip("글리치 시 최저 피치 배수 (1 = 정상)")]
        [SerializeField] private float glitchPitchMin = 0.3f;
        [Tooltip("글리치 시 최고 피치 배수")]
        [SerializeField] private float glitchPitchMax = 0.8f;
        [Tooltip("글리치 끊김 사이 간격(초)")]
        [SerializeField] private float glitchCutInterval = 0.15f;
        [Tooltip("글리치 총 지속 시간(초)")]
        [SerializeField] private float glitchDuration = 3f;

        [Header("도현 독백 — 일차별")]
        [Tooltip("일차(index 0 = 1일차)별 콜 수락 직후 도현이 내뱉는 독백 텍스트. 비워두면 출력하지 않습니다.")]
        [SerializeField] private string[] driverMonologues = Array.Empty<string>();

        [Tooltip("도현 독백을 표시할 TMP 텍스트 (선택). 있으면 3초간 출력 후 숨깁니다.")]
        [SerializeField] private TMPro.TMP_Text monologueText;

        // ─────────────────────────────────────────────────────────────────────
        // 이벤트

        /// <summary>콜 수신 시 (ClientDefinition 포함)</summary>
        public event Action<ClientDefinition> OnCallReceived;

        /// <summary>콜 수락 완료 시</summary>
        public event Action<ClientDefinition> OnCallAccepted;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private ClientDefinition _pendingCall;
        private Coroutine _glitchRoutine;
        private Coroutine _monologueRoutine;

        // ─────────────────────────────────────────────────────────────────────
        // 생명주기

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 자동 탐색
            if (naviManager == null)
                naviManager = FindFirstObjectByType<NavigationManager>(FindObjectsInactive.Include);
            if (callFlow == null)
                callFlow = FindFirstObjectByType<CallFlowController>(FindObjectsInactive.Include);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 콜 수신

        /// <summary>
        /// 새 콜이 도착했음을 시스템에 알립니다.
        /// 알림음 재생 + 배너 표시 + 뱃지 ON 처리됩니다.
        /// 폰은 자동으로 열리지 않습니다.
        /// </summary>
        /// <param name="def">수신된 ClientDefinition (손님 정보)</param>
        public void ReceiveCall(ClientDefinition def)
        {
            _pendingCall = def;

            // 알림음 재생
            PlayRingtone(normal: true);

            // 배너 표시 (슬라이드 다운)
            banner?.Show("새 콜이 도착했습니다");

            // 폰 내 콜 앱에 손님 정보 미리 주입 (열기 전에도 세팅)
            callApp?.SetCallInfo(def);

            // 폰 아이콘 뱃지 ON
            phoneManager?.SetCallBadge(true);

            OnCallReceived?.Invoke(def);
        }

        /// <summary>
        /// 폰이 열릴 때 PhoneManager에서 호출됩니다.
        /// 미확인 콜이 있으면 자동으로 콜 앱 화면으로 전환합니다.
        /// </summary>
        public void OnPhoneOpened()
        {
            if (_pendingCall != null)
                callApp?.ShowCallScreen();
        }

        /// <summary>
        /// 수락 버튼 클릭 시 PhoneCallApp에서 호출됩니다.
        /// 배차 완료 처리 후 폰을 닫고 네비게이션을 시작합니다.
        /// </summary>
        public void AcceptCall()
        {
            if (_pendingCall == null) return;

            var acceptedDef = _pendingCall;
            // _pendingCall은 PlayDriverMonologue 내부에서 참조하므로 독백 재생 후 null 처리


            // 배차 완료 텍스트 (콜 앱 내부에서 표시)
            callApp?.ShowAcceptedFeedback();

            // 뱃지 제거
            phoneManager?.SetCallBadge(false);

            // 폰 닫기 (약간 딜레이 후 — 배차 완료 텍스트를 잠깐 보여주기 위해)
            StartCoroutine(ClosePhoneDelayed(0.8f));

            // CallFlowController를 통해 손님 스폰 요청
            callFlow?.TryAcceptCall();

            // 도현 독백 출력 (_pendingCall이 아직 살아있는 동안 호출해야 손님별 독백을 읽을 수 있습니다)
            PlayDriverMonologue();

            // 이벤트 브로드캐스트 후 _pendingCall 초기화
            _pendingCall = null;

            // 픽업 Transform 주입은 AfterClientSpawnComplete 이벤트를 구독한 외부 컴포넌트에서 합니다.
            OnCallAccepted?.Invoke(acceptedDef);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 공포 연출

        /// <summary>
        /// 알림음 피치를 낮추고 끊기게 재생하는 글리치 린턴을 연출합니다.
        /// </summary>
        public void TriggerGlitchRingtone()
        {
            if (_glitchRoutine != null)
                StopCoroutine(_glitchRoutine);
            _glitchRoutine = StartCoroutine(GlitchRingtoneRoutine());
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 오디오

        private void PlayRingtone(bool normal)
        {
            if (ringtoneSource == null) return;

            ringtoneSource.Stop();
            ringtoneSource.pitch = 1f;

            if (ringtoneClip != null)
                ringtoneSource.clip = ringtoneClip;

            if (ringtoneSource.clip == null) return;

            if (normal)
                ringtoneSource.Play();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 코루틴

        private IEnumerator ClosePhoneDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            phoneManager?.ClosePhone();
        }

        private IEnumerator GlitchRingtoneRoutine()
        {
            if (ringtoneSource == null) yield break;

            ringtoneSource.Stop();
            if (ringtoneClip != null) ringtoneSource.clip = ringtoneClip;
            if (ringtoneSource.clip == null) yield break;

            float elapsed = 0f;

            while (elapsed < glitchDuration)
            {
                // 랜덤 피치로 짧게 재생
                ringtoneSource.pitch = UnityEngine.Random.Range(glitchPitchMin, glitchPitchMax);
                ringtoneSource.Play();
                float playTime = UnityEngine.Random.Range(0.05f, glitchCutInterval);
                yield return new WaitForSeconds(playTime);

                // 무음 구간
                ringtoneSource.Stop();
                float silenceTime = UnityEngine.Random.Range(0.05f, glitchCutInterval * 0.5f);
                yield return new WaitForSeconds(silenceTime);

                elapsed += playTime + silenceTime;
            }

            ringtoneSource.pitch = 1f;
            _glitchRoutine = null;
        }

        private void PlayDriverMonologue()
        {
            // 1순위: ClientDefinition에 손님별 독백이 있으면 사용합니다.
            string text = _pendingCall != null && !string.IsNullOrWhiteSpace(_pendingCall.driverMonologue)
                ? _pendingCall.driverMonologue
                : null;

            // 2순위: 일차별 독백 배열 (driverMonologues)에서 가져옵니다.
            if (text == null && driverMonologues != null && driverMonologues.Length > 0)
            {
                int dayIndex = 0;
                if (GameManager.Instance != null && GameManager.Instance.NightManager != null)
                    dayIndex = Mathf.Max(0, GameManager.Instance.NightManager.State.dayIndex - 1);

                if (dayIndex < driverMonologues.Length)
                    text = driverMonologues[dayIndex];
            }

            if (string.IsNullOrWhiteSpace(text)) return;

            // PhoneCallApp 독백 박스에 주입합니다.
            if (callApp != null)
                callApp.SetMonologue(text);

            // 별도 monologueText가 연결돼 있으면 그것도 표시합니다.
            if (monologueText != null)
            {
                if (_monologueRoutine != null) StopCoroutine(_monologueRoutine);
                _monologueRoutine = StartCoroutine(ShowMonologueRoutine(text, 3f));
            }
        }

        private IEnumerator ShowMonologueRoutine(string text, float displaySeconds)
        {
            monologueText.text = text;
            monologueText.gameObject.SetActive(true);
            yield return new WaitForSeconds(displaySeconds);
            monologueText.gameObject.SetActive(false);
            _monologueRoutine = null;
        }
    }
}
