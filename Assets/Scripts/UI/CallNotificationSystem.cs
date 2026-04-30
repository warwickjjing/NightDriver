using System;
using System.Collections;
using NightDriver.Client;
using NightDriver.Core;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 콜 수신 흐름 전체를 관리하는 오케스트레이터 (싱글톤).
    ///
    /// [자동 흐름]
    ///   게임 시작 → Start()에서 첫 콜 자동 수신
    ///   하차 완료 → OnAnyClientDroppedOff → 다음 콜 자동 수신
    ///   손님 스폰 완료 → AfterClientSpawnComplete → NavigationManager.SetPickupMode() 자동 호출
    ///
    /// [수동 API]
    ///   TriggerIncomingCall()  — 외부에서 강제로 다음 콜을 띄울 때
    ///   ReceiveCall(def)       — 특정 ClientDefinition으로 콜을 수신할 때
    ///   TriggerGlitchRingtone() — 공포 연출
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Call Notification System")]
    public sealed class CallNotificationSystem : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 싱글톤

        public static CallNotificationSystem Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — UI 참조

        [Header("UI 컴포넌트")]
        [Tooltip("폰 열기/닫기 통합 컨트롤러")]
        [SerializeField] private PhoneManager phoneManager;

        [Tooltip("화면 상단 슬라이드다운 배너")]
        [SerializeField] private CallNotificationBanner banner;

        [Tooltip("폰 내부 콜 앱 화면")]
        [SerializeField] private PhoneCallApp callApp;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 게임 시스템

        [Header("게임 시스템")]
        [Tooltip("비우면 씬에서 자동 탐색합니다.")]
        [SerializeField] private NavigationManager naviManager;

        [Tooltip("비우면 씬에서 자동 탐색합니다.")]
        [SerializeField] private CallFlowController callFlow;

        [Tooltip("다음 손님 정보를 미리 읽기 위한 ClientSpawner. 비우면 씬에서 자동 탐색합니다.")]
        [SerializeField] private ClientSpawner spawner;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 타이밍

        [Header("콜 수신 타이밍")]
        [Tooltip("게임 시작 후 첫 콜을 수신하기까지 대기 시간(초)")]
        [SerializeField] private float firstCallDelay = 1f;

        [Tooltip("손님 하차 완료 후 다음 콜 수신까지 대기 시간(초)")]
        [SerializeField] private float nextCallDelay = 3f;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 오디오

        [Header("오디오")]
        [Tooltip("콜 수신 알림음 AudioSource")]
        [SerializeField] private AudioSource ringtoneSource;

        [Tooltip("알림음 클립. AudioSource에 미리 설정해도 되고 여기서 지정해도 됩니다.")]
        [SerializeField] private AudioClip ringtoneClip;

        [Header("글리치 린턴 설정")]
        [SerializeField] private float glitchPitchMin = 0.3f;
        [SerializeField] private float glitchPitchMax = 0.8f;
        [SerializeField] private float glitchCutInterval = 0.15f;
        [SerializeField] private float glitchDuration = 3f;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 도현 독백

        [Header("도현 독백 — 일차별 (손님별 독백 없을 때 사용)")]
        [Tooltip("인덱스 0 = 1일차. 비워두면 해당 일차 독백 없음.")]
        [SerializeField] private string[] driverMonologues = Array.Empty<string>();

        [Tooltip("독백을 표시할 TMP_Text (선택). 있으면 3초 후 숨깁니다.")]
        [SerializeField] private TMPro.TMP_Text monologueText;

        // ─────────────────────────────────────────────────────────────────────
        // 이벤트

        /// <summary>콜 수신 시 (ClientDefinition 포함)</summary>
        public event Action<ClientDefinition> OnCallReceived;

        /// <summary>콜 수락 완료 시 (ClientDefinition 포함)</summary>
        public event Action<ClientDefinition> OnCallAccepted;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임 상태

        /// <summary>현재 수신 중이거나 대기 중인 콜. null이면 콜 없음.</summary>
        public ClientDefinition PendingCall => _pendingCall;

        private ClientDefinition _pendingCall;
        private Coroutine        _glitchRoutine;
        private Coroutine        _monologueRoutine;
        private Coroutine        _nextCallRoutine;

        // ─────────────────────────────────────────────────────────────────────
        // 생명주기

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 자동 탐색
            if (naviManager == null)
                naviManager = FindFirstObjectByType<NavigationManager>(FindObjectsInactive.Include);
            if (callFlow == null)
                callFlow    = FindFirstObjectByType<CallFlowController>(FindObjectsInactive.Include);
            if (spawner == null)
                spawner     = FindFirstObjectByType<ClientSpawner>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            // 하차 완료 이벤트 구독 → 다음 콜 자동 수신
            ClientBehaviour.OnAnyClientDroppedOff += HandleClientDroppedOff;
        }

        private void OnDisable()
        {
            ClientBehaviour.OnAnyClientDroppedOff -= HandleClientDroppedOff;
        }

        private void Start()
        {
            // 게임 시작 시 첫 콜을 자동으로 수신합니다.
            if (_nextCallRoutine != null) StopCoroutine(_nextCallRoutine);
            _nextCallRoutine = StartCoroutine(TriggerIncomingCallDelayed(firstCallDelay));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 콜 수신

        /// <summary>
        /// WeekSchedule에서 다음 손님 정보를 읽어 즉시 콜을 수신합니다.
        /// 다음 손님이 없으면 아무 일도 하지 않습니다.
        /// </summary>
        public void TriggerIncomingCall()
        {
            if (spawner == null) return;

            var def = spawner.PeekNextClient();
            if (def == null)
            {
                Debug.Log("[CallNotification] 다음 손님이 없습니다. 콜 수신 없음.");
                return;
            }

            ReceiveCall(def);
        }

        /// <summary>
        /// 특정 ClientDefinition으로 콜을 수신합니다.
        /// 알림음 + 배너 + 뱃지 ON + 콜 앱 데이터 주입이 이루어집니다.
        /// 폰은 자동으로 열리지 않습니다.
        /// </summary>
        public void ReceiveCall(ClientDefinition def)
        {
            if (def == null) return;

            _pendingCall = def;

            // 알림음 재생
            PlayRingtone();

            // 상단 배너 슬라이드 다운
            banner?.Show("새 콜이 도착했습니다");

            // 콜 앱에 손님 정보 미리 주입 (폰 열기 전에도 세팅됨)
            callApp?.SetCallInfo(def);

            // 폰 아이콘 뱃지 ON
            phoneManager?.SetCallBadge(true);

            OnCallReceived?.Invoke(def);
        }

        /// <summary>
        /// PhoneManager.OpenPhone() 직후 호출됩니다.
        /// 미확인 콜이 있으면 PhoneScreenManager를 통해 콜 앱 화면으로 전환합니다.
        /// 콜이 없으면 홈 화면이 그대로 유지됩니다.
        /// </summary>
        public void OnPhoneOpened()
        {
            if (_pendingCall == null) return;

            // PhoneScreenManager가 있으면 화면 전환 위임
            if (PhoneScreenManager.Instance != null)
                PhoneScreenManager.Instance.ShowCallApp();
            else
                callApp?.ShowCallScreen(); // 폴백: ScreenManager 없을 때
        }

        /// <summary>
        /// 수락 버튼 클릭 시 PhoneCallApp에서 호출됩니다.
        ///
        /// [처리 순서]
        ///   1. 배차 완료 피드백 표시
        ///   2. 뱃지 제거
        ///   3. 폰 닫기 (0.8초 딜레이)
        ///   4. TryAcceptCall() → ClientSpawner가 손님·차량 스폰 시작
        ///   5. AfterClientSpawnComplete 이벤트 → NavigationManager.SetPickupMode()
        ///   6. 도현 독백 출력
        /// </summary>
        public void AcceptCall()
        {
            if (_pendingCall == null) return;

            var acceptedDef = _pendingCall;

            // ── 1. 배차 완료 피드백 ──
            callApp?.ShowAcceptedFeedback();

            // ── 2. 뱃지 OFF ──
            phoneManager?.SetCallBadge(false);

            // ── 3. 배차 완료 잠깐 보여준 후 홈으로 복귀 → 폰 닫기 ──
            StartCoroutine(AcceptFeedbackThenCloseRoutine(0.8f));

            // ── 4. 손님 스폰 요청 ──
            //    스폰이 완료되면 AfterClientSpawnComplete 이벤트를 통해 픽업 네비를 시작합니다.
            if (spawner != null)
            {
                // 원샷 구독: 스폰 완료 시 한 번만 실행하고 바로 구독 해제합니다.
                System.Action onSpawnComplete = null;
                onSpawnComplete = () =>
                {
                    spawner.AfterClientSpawnComplete -= onSpawnComplete;

                    // ClientRegistry에서 방금 스폰된 손님 Transform을 가져옵니다.
                    var pickupTarget = ClientRegistry.CurrentClientObject != null
                        ? ClientRegistry.CurrentClientObject.transform
                        : null;

                    if (pickupTarget != null)
                    {
                        Debug.Log($"[CallNotification] 픽업 네비 시작 → {pickupTarget.name}");
                        naviManager?.SetPickupMode(pickupTarget);
                    }
                    else
                    {
                        Debug.LogWarning("[CallNotification] 스폰 후 ClientRegistry가 비어 있습니다.");
                    }
                };
                spawner.AfterClientSpawnComplete += onSpawnComplete;
            }

            callFlow?.TryAcceptCall();

            // ── 5. 도현 독백 출력 ──
            //    _pendingCall이 아직 살아있는 동안 호출해야 손님별 독백을 읽을 수 있습니다.
            PlayDriverMonologue();

            // ── 6. 정리 ──
            _pendingCall = null;
            OnCallAccepted?.Invoke(acceptedDef);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 공포 연출

        /// <summary>알림음 피치를 낮추고 끊기게 재생합니다 (공포 연출).</summary>
        public void TriggerGlitchRingtone()
        {
            if (_glitchRoutine != null) StopCoroutine(_glitchRoutine);
            _glitchRoutine = StartCoroutine(GlitchRingtoneRoutine());
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 이벤트 핸들러

        private void HandleClientDroppedOff()
        {
            // 하차 완료 후 nextCallDelay초 뒤에 다음 콜을 수신합니다.
            if (_nextCallRoutine != null) StopCoroutine(_nextCallRoutine);
            _nextCallRoutine = StartCoroutine(TriggerIncomingCallDelayed(nextCallDelay));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 오디오

        private void PlayRingtone()
        {
            if (ringtoneSource == null) return;

            ringtoneSource.Stop();
            ringtoneSource.pitch = 1f;

            if (ringtoneClip != null) ringtoneSource.clip = ringtoneClip;
            if (ringtoneSource.clip == null) return;

            ringtoneSource.Play();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 코루틴

        private IEnumerator TriggerIncomingCallDelayed(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            TriggerIncomingCall();
            _nextCallRoutine = null;
        }

        private IEnumerator AcceptFeedbackThenCloseRoutine(float delay)
        {
            // 배차 완료 텍스트를 잠깐 보여줍니다.
            yield return new WaitForSeconds(delay);
            // 홈 화면으로 복귀 후 폰 닫기
            PhoneScreenManager.Instance?.ShowHome();
            yield return new WaitForSeconds(0.1f);
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
                ringtoneSource.pitch = UnityEngine.Random.Range(glitchPitchMin, glitchPitchMax);
                ringtoneSource.Play();
                float playTime = UnityEngine.Random.Range(0.05f, glitchCutInterval);
                yield return new WaitForSeconds(playTime);

                ringtoneSource.Stop();
                float silenceTime = UnityEngine.Random.Range(0.05f, glitchCutInterval * 0.5f);
                yield return new WaitForSeconds(silenceTime);

                elapsed += playTime + silenceTime;
            }

            ringtoneSource.pitch = 1f;
            _glitchRoutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 도현 독백

        private void PlayDriverMonologue()
        {
            // 1순위: ClientDefinition에 손님별 독백이 있으면 사용합니다.
            string text = _pendingCall != null && !string.IsNullOrWhiteSpace(_pendingCall.driverMonologue)
                ? _pendingCall.driverMonologue
                : null;

            // 2순위: 일차별 독백 배열에서 가져옵니다.
            if (text == null && driverMonologues != null && driverMonologues.Length > 0)
            {
                int dayIndex = 0;
                if (GameManager.Instance?.NightManager != null)
                    dayIndex = Mathf.Max(0, GameManager.Instance.NightManager.State.dayIndex - 1);

                if (dayIndex < driverMonologues.Length)
                    text = driverMonologues[dayIndex];
            }

            if (string.IsNullOrWhiteSpace(text)) return;

            // PhoneCallApp 독백 박스에 주입합니다.
            callApp?.SetMonologue(text);

            // 별도 monologueText가 연결돼 있으면 3초 표시 후 숨깁니다.
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
