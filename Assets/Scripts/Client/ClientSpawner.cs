using NightDriver.Core;
using NightDriver.Dialogue;
using NightDriver.Interaction;
using NightDriver.UI;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// WeekSchedule을 기반으로 현재 일차/콜 인덱스에 맞는 손님을 스폰/디스폰합니다.
    ///
    /// - OnDayChanged 이벤트를 구독해 자동으로 손님을 교체합니다.
    /// - 스폰 위치는 ClientDefinition.spawnPointId → SpawnPointSet 순서로 탐색합니다.
    /// - 대화 노드는 ClientDefinition.startNode → InteractionPrompt에 자동 주입됩니다.
    /// </summary>
    public sealed class ClientSpawner : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private NightManager nightManager;
        [SerializeField] private WeekSchedule schedule;

        [Header("Spawn")]
        [SerializeField] private Transform defaultSpawnPoint;
        [Tooltip("손님별 ID 기반 스폰 위치를 사용하려면 씬의 SpawnPointSet을 연결하세요.")]
        [SerializeField] private SpawnPointSet spawnPointSet;
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private bool respawnOnDayChanged = false;

        [Header("Navigation HUD")]
        [Tooltip("손님 스폰 시 자동으로 HUD 목적지를 스폰 위치로 설정합니다.")]
        [SerializeField] private NavigationHUD navigationHUD;

        private GameObject current;
        private GameObject currentVehicle;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            // 폰 콜 수락 기반 플로우에서는 시작 자동 스폰/일차 변경 자동 스폰을 항상 비활성화합니다.
            // (Inspector에서 실수로 켜져 있어도 런타임에서 강제 OFF)
            spawnOnStart = false;
            respawnOnDayChanged = false;

            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;

            // NavigationHUD 자동 탐색 (Inspector 미할당 시)
            if (navigationHUD == null)
                navigationHUD = FindFirstObjectByType<NavigationHUD>();
        }

        private void OnEnable()
        {
            if (nightManager != null) nightManager.OnDayChanged += HandleDayChanged;
        }

        private void Start()
        {
            // spawnOnStart가 켜져 있으면 NightManager 이벤트와 무관하게 초기 스폰을 보장합니다.
            // NightManager.Start()의 OnDayChanged 브로드캐스트로 이미 스폰됐을 수 있으니
            // current가 없을 때만 스폰합니다.
            if (spawnOnStart && current == null)
                SpawnCurrentClient();
        }

        private void OnDisable()
        {
            if (nightManager != null) nightManager.OnDayChanged -= HandleDayChanged;
        }

        // ─────────────────────────────────────────────

        [ContextMenu("Spawn Current Client (Debug)")]
        public void SpawnCurrentClient()
        {
            if (nightManager == null || schedule == null) return;

            int day       = nightManager.State.dayIndex;
            int callIndex = nightManager.State.callsCompleted;
            var def = schedule.GetClientFor(day, callIndex);
            if (def == null || def.prefab == null) return;

            DespawnCurrent();

            // 스폰 위치 결정: SpawnPointSet → defaultSpawnPoint 순으로 폴백
            var point = spawnPointSet != null ? spawnPointSet.Find(def.spawnPointId) : null;
            if (point == null) point = defaultSpawnPoint;
            if (point == null)
            {
                Debug.LogWarning($"[ClientSpawner] 스폰 위치를 찾을 수 없습니다. (id: '{def.spawnPointId}')", this);
                return;
            }

            current      = Instantiate(def.prefab, point.position, point.rotation);
            current.name = $"Client_{def.clientId}";

            if (def.vehiclePrefab != null)
            {
                currentVehicle = Instantiate(def.vehiclePrefab, point.position, point.rotation);
                currentVehicle.name = $"Vehicle_{def.clientId}";
            }

            // InteractionPrompt에 Yarn 노드 이름 주입 (자동 배선)
            var prompt = current.GetComponentInChildren<InteractionPrompt>(true);
            if (prompt != null)
                prompt.SetYarnNode(def.startNode);

            // ClientDialogueTarget 기존 시스템과도 호환
            var target = current.GetComponentInChildren<ClientDialogueTarget>(true);
            if (target != null)
                target.Configure(def.clientId, def.startNode);

            ClientRegistry.SetCurrent(current);

            // HUD 목적지를 손님 자체로 설정 → 근접 시 자동으로 HUD 숨김
            navigationHUD?.SetDestination(current.transform);
        }

        [ContextMenu("Despawn Current Client (Debug)")]
        public void DespawnCurrent()
        {
            if (current != null)
            {
                ClientRegistry.ClearIfCurrent(current);
                Destroy(current);
                current = null;
            }

            if (currentVehicle != null)
            {
                Destroy(currentVehicle);
                currentVehicle = null;
            }
        }

        // ─────────────────────────────────────────────

        private void HandleDayChanged(int _)
        {
            if (!respawnOnDayChanged) return;
            SpawnCurrentClient();
        }
    }
}
