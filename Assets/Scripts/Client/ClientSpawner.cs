using NightDriver.Core;
using NightDriver.Dialogue;
using NightDriver.Interaction;
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
        [Tooltip("활성화 시 즉시 현재 일차의 손님을 스폰합니다.\n" +
                 "NightManager.Start()의 초기 이벤트와 함께 쓰면 중복 스폰이 발생할 수 있으니\n" +
                 "CallFlowController를 쓰는 경우에는 비활성화하세요.")]
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private bool respawnOnDayChanged = true;

        private GameObject current;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
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

            // InteractionPrompt에 Yarn 노드 이름 주입 (자동 배선)
            var prompt = current.GetComponentInChildren<InteractionPrompt>(true);
            if (prompt != null)
                prompt.SetYarnNode(def.startNode);

            // ClientDialogueTarget 기존 시스템과도 호환
            var target = current.GetComponentInChildren<ClientDialogueTarget>(true);
            if (target != null)
                target.Configure(def.clientId, def.startNode);

            ClientRegistry.SetCurrent(current);
        }

        [ContextMenu("Despawn Current Client (Debug)")]
        public void DespawnCurrent()
        {
            if (current == null) return;
            ClientRegistry.ClearIfCurrent(current);
            Destroy(current);
            current = null;
        }

        // ─────────────────────────────────────────────

        private void HandleDayChanged(int _)
        {
            if (!respawnOnDayChanged) return;
            SpawnCurrentClient();
        }
    }
}
