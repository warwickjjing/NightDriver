using System;
using UnityEngine;

namespace NightDriver.Core
{
    [Serializable]
    public sealed class NightRuntimeState
    {
        [Range(1, 7)] public int dayIndex = 1;
        public int callsCompleted;
        [Tooltip("하룻밤 목표 콜 수. NightConfig SO가 있으면 자동으로 덮어씁니다.")]
        public int callsPerNight = 2;
        public bool nightFailed;
    }

    /// <summary>
    /// 밤 진행 상태를 관리하는 매니저.
    ///
    /// NightConfig SO를 할당하면 callsPerNight / startDay 등을 코드 수정 없이 조정할 수 있습니다.
    /// SO를 비워두면 인스펙터 직접 설정값을 사용합니다.
    /// </summary>
    public sealed class NightManager : MonoBehaviour
    {
        public event Action<int> OnDayChanged;
        public event Action<NightPhase> OnPhaseChanged;
        public event Action OnNightCompleted;
        public event Action OnNightFailed;

        [Header("Config (SO — 비워두면 아래 직접 설정값 사용)")]
        [SerializeField] private NightConfig config;

        [Header("Runtime State")]
        [SerializeField] private NightRuntimeState state = new NightRuntimeState();
        [SerializeField] private NightPhase phase = NightPhase.NightStart;

        public NightRuntimeState State => state;
        public NightPhase Phase => phase;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (config != null)
            {
                state.callsPerNight = config.callsPerNight;
                state.dayIndex      = Mathf.Clamp(config.startDay, 1, config.maxDay);
            }
        }

        private void Start()
        {
            // Awake 이후 모든 OnEnable 구독이 완료된 뒤 초기 상태를 한 번 브로드캐스트합니다.
            // 이렇게 해야 ClientSpawner 등 구독자가 게임 시작 시 첫 스폰을 자동으로 처리합니다.
            OnDayChanged?.Invoke(state.dayIndex);
        }

        // ─────────────────────────────────────────────

        public void StartNewRun()
        {
            state.dayIndex       = config != null ? config.startDay : 1;
            state.callsCompleted = 0;
            state.nightFailed    = false;
            SetPhase(NightPhase.NightStart);
            OnDayChanged?.Invoke(state.dayIndex);
        }

        public void BeginNight(int dayIndex)
        {
            int max = config != null ? config.maxDay : 7;
            state.dayIndex       = Mathf.Clamp(dayIndex, 1, max);
            state.callsCompleted = 0;
            state.nightFailed    = false;
            SetPhase(NightPhase.NightStart);
            OnDayChanged?.Invoke(state.dayIndex);
        }

        public void EnterDrivingLoop()
        {
            if (state.nightFailed) return;
            SetPhase(NightPhase.DrivingLoop);
        }

        public void CompleteOneCall()
        {
            CompleteOneCall(null);
        }

        /// <summary>
        /// 콜 1건 완료 처리. <paramref name="callsLimitForTonight"/>가 있으면 그 값을 오늘의 목표 콜 수로 쓰고,
        /// 없으면 <see cref="NightRuntimeState.callsPerNight"/>를 씁니다.
        /// </summary>
        public void CompleteOneCall(int? callsLimitForTonight)
        {
            if (state.nightFailed) return;

            state.callsCompleted++;
            int limit = callsLimitForTonight ?? Mathf.Max(1, state.callsPerNight);
            limit = Mathf.Max(1, limit);
            if (state.callsCompleted >= limit)
                EndNight(success: true);
        }

        public void FailNight()
        {
            if (state.nightFailed) return;
            state.nightFailed = true;
            EndNight(success: false);
        }

        public void AdvanceToNextNight()
        {
            int max  = config != null ? config.maxDay : 7;
            int next = Mathf.Clamp(state.dayIndex + 1, 1, max);
            BeginNight(next);
        }

        // ─────────────────────────────────────────────

        private void EndNight(bool success)
        {
            SetPhase(NightPhase.NightEnd);
            if (success) OnNightCompleted?.Invoke();
            else         OnNightFailed?.Invoke();
        }

        private void SetPhase(NightPhase next)
        {
            if (phase == next) return;
            phase = next;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
