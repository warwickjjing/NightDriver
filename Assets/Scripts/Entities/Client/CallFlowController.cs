using NightDriver.Core;
using NightDriver.Vehicle;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 콜 플로우 오케스트레이터(단일 진입점).
    ///
    /// 흐름:
    /// 1) <see cref="TryAcceptCall"/> — 폰 수락 → <see cref="ClientSpawner"/>로 손님·차량 스폰
    /// 2) Yarn <c>&lt;&lt;pickupComplete&gt;&gt;</c> + <c>&lt;&lt;setDestination …&gt;&gt;</c> — 둘 다 만족 시 차량 탑승 허용
    /// 3) 손님 하차 완료 이벤트 → <see cref="NightManager.CompleteOneCall"/> (오늘 목표 콜 수는 WeekSchedule 인원 우선)
    ///
    /// 데이터: <see cref="WeekSchedule"/>, <see cref="ClientDefinition"/> (스포너가 읽음)
    /// </summary>
    public sealed class CallFlowController : MonoBehaviour, ICallFlow
    {
        [Header("References")]
        [SerializeField] private NightManager nightManager;
        [SerializeField] private ClientSpawner spawner;

        [Header("Behavior")]
        [SerializeField] private bool advanceCallOnDropoff = true;

        private bool pickupDialogueComplete;
        private bool destinationChosen;

        /// <summary>
        /// Yarn <c>&lt;&lt;pickupComplete&gt;&gt;</c> 및 <c>&lt;&lt;setDestination&gt;&gt;</c>를 모두 통과했는지.
        /// 차량 탑승 UI가 이 값을 직접 참조해, 프리팹에서 canEnter만 잘못 열린 경우를 막습니다.
        /// </summary>
        public bool AreVehicleBoardingGatesSatisfied => pickupDialogueComplete && destinationChosen;

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
            if (spawner == null)
                spawner = FindFirstObjectByType<ClientSpawner>();
        }

        private void OnEnable()
        {
            ClientBehaviour.OnAnyClientDroppedOff += HandleClientDroppedOff;
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
            if (nightManager != null)
                nightManager.OnDayChanged += HandleDayChanged;
            if (spawner == null)
                spawner = FindFirstObjectByType<ClientSpawner>();
            if (spawner != null)
                spawner.AfterClientSpawnComplete += OnAfterClientSpawnComplete;
        }

        private void OnDisable()
        {
            ClientBehaviour.OnAnyClientDroppedOff -= HandleClientDroppedOff;
            if (nightManager != null)
                nightManager.OnDayChanged -= HandleDayChanged;
            if (spawner != null)
                spawner.AfterClientSpawnComplete -= OnAfterClientSpawnComplete;
        }

        private void OnAfterClientSpawnComplete()
        {
            pickupDialogueComplete = false;
            destinationChosen = false;
            ResetVehicleSeatLocksForNewCall();
            RefreshVehicleEnterAllowed();
        }

        /// <inheritdoc />
        public bool TryAcceptCall()
        {
            if (nightManager == null || spawner == null)
                return false;

            if (ClientRegistry.CurrentClientObject != null)
            {
                Debug.Log("[CallFlow] 이미 활성 손님이 있어 콜을 받을 수 없습니다.", this);
                return false;
            }

            int completed = nightManager.State.callsCompleted;
            int limit = GetEffectiveCallsLimit();
            if (completed >= limit)
            {
                Debug.Log("[CallFlow] 오늘 콜을 모두 완료했습니다.", this);
                return false;
            }

            spawner.SpawnCurrentClient();
            return true;
        }

        /// <inheritdoc />
        public void NotifyDestinationChosen()
        {
            destinationChosen = true;
            RefreshVehicleEnterAllowed();
        }

        /// <inheritdoc />
        public void NotifyDestinationCleared()
        {
            destinationChosen = false;
            RefreshVehicleEnterAllowed();
        }

        /// <inheritdoc />
        public void NotifyPickupDialogueComplete()
        {
            pickupDialogueComplete = true;
            RefreshVehicleEnterAllowed();
        }

        private void RefreshVehicleEnterAllowed()
        {
            bool allow = pickupDialogueComplete && destinationChosen;
            // 씬에 차량이 여러 대(배치용 + 스폰)면 ActiveInstance는 한 대뿐이라, 나머지는 canEnter가 안 켜져 탑승 UI가 안 뜹니다.
            var all = FindObjectsByType<VehicleSeatInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].EnableEnter(allow);
            }
        }

        private void ResetVehicleSeatLocksForNewCall()
        {
            // 이전 콜 드롭오프에서 enterPermanentlyLocked가 걸린 차량이 씬에 남아 있으면
            // 다음 콜/다음 날에도 탑승 프롬프트가 영구적으로 안 뜨는 문제가 생깁니다.
            var all = FindObjectsByType<VehicleSeatInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var v = all[i];
                if (v == null) continue;
                v.SetEnterPermanentlyLocked(false);
                v.SetCanExit(false);
                v.EnableEnter(false);
            }
        }

        private void HandleDayChanged(int _)
        {
            pickupDialogueComplete = false;
            destinationChosen = false;
            ResetVehicleSeatLocksForNewCall();
            RefreshVehicleEnterAllowed();
        }

        private void HandleClientDroppedOff()
        {
            Debug.Log("HandleClientDroppedOff Call");
            if (!advanceCallOnDropoff) return;
            if (nightManager == null) return;

            nightManager.CompleteOneCall(GetEffectiveCallsLimit());
        }

        /// <summary>
        /// WeekSchedule에 오늘 일차 손님이 있으면 그 명수, 없으면 NightManager.callsPerNight.
        /// </summary>
        private int GetEffectiveCallsLimit()
        {
            int fallback = Mathf.Max(1, nightManager.State.callsPerNight);
            if (spawner == null)
                return fallback;
            var sched = spawner.Schedule;
            if (sched == null)
                return fallback;
            return sched.ResolveCallsLimitForDay(nightManager.State.dayIndex, fallback);
        }
    }
}
