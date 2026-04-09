using System.Collections;
using System.Collections.Generic;
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
    /// - 다음 콜 스폰 시 이전 손님은 WalkOff 연출 중이면 Destroy하지 않습니다(차량만 제거).
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

        [Header("Scene Location Sets (Optional)")]
        [Tooltip("목적지/하차 지점을 ID로 관리하려면 씬의 ExitPointSet을 연결하세요.")]
        [SerializeField] private ExitPointSet exitPointSet;

        [Header("Vehicle Spawn")]
        [Tooltip("ClientDefinition에 개별 설정이 없을 때만 사용하는 기본 차량 오프셋(로컬)입니다.")]
        [SerializeField] private Vector3 defaultVehicleSpawnLocalOffset = new Vector3(2.2f, 0f, 0f);

        [Header("Ground Snap")]
        [Tooltip("스폰 직후 지면으로 스냅합니다(프리팹 피벗/루트가 떠있는 경우 보정).")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private float groundSnapRayUp = 2.0f;
        [SerializeField] private float groundSnapRayDown = 10.0f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private GameObject current;
        private GameObject currentVehicle;
        private bool spawnInProgress;
        private readonly List<GameObject> spawnedClients = new List<GameObject>();
        private readonly List<GameObject> spawnedVehicles = new List<GameObject>();

        /// <summary>연결된 주간 스케줄(없으면 null).</summary>
        public WeekSchedule Schedule => schedule;

        /// <summary>손님·차량 스폰 및 레지스트리/HUD 배선이 끝난 직후(한 콜 단위).</summary>
        public event System.Action AfterClientSpawnComplete;

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

            // ExitPointSet 자동 탐색 (Inspector 미할당 시)
            if (exitPointSet == null)
                exitPointSet = FindFirstObjectByType<ExitPointSet>();
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
            if (spawnInProgress) return;
            if (nightManager == null || schedule == null) return;

            int day       = nightManager.State.dayIndex;
            int callIndex = nightManager.State.callsCompleted;
            var def = schedule.GetClientFor(day, callIndex);
            if (def == null || def.prefab == null) return;

            StartCoroutine(SpawnCurrentClientRoutine(def));
        }

        private IEnumerator SpawnCurrentClientRoutine(ClientDefinition def)
        {
            spawnInProgress = true;
            try
            {
                bool hadVehicleToReplace = currentVehicle != null;
                PrepareForNewSpawn();
                // Destroy는 프레임 끝에 처리되므로, 이전 차량이 있을 때만 한 템포 기다려 겹침 물리를 피합니다.
                if (hadVehicleToReplace)
                {
                    yield return null;
                    yield return new WaitForFixedUpdate();
                }

                // 스폰 위치 결정: SpawnPointSet → defaultSpawnPoint 순으로 폴백
                var point = spawnPointSet != null ? spawnPointSet.Find(def.spawnPointId) : null;
                if (point == null) point = defaultSpawnPoint;
                if (point == null)
                {
                    Debug.LogWarning($"[ClientSpawner] 스폰 위치를 찾을 수 없습니다. (id: '{def.spawnPointId}')", this);
                    yield break;
                }

                current      = Instantiate(def.prefab, point.position, point.rotation);
                current.name = $"Client_{def.clientId}";
                if (snapToGround)
                    SnapRootToGround(current.transform);
                spawnedClients.Add(current);

                if (def.vehiclePrefab != null)
                {
                    Transform vehiclePoint = null;
                    if (spawnPointSet != null)
                    {
                        if (!string.IsNullOrWhiteSpace(def.vehicleSpawnPointId))
                            vehiclePoint = spawnPointSet.Find(def.vehicleSpawnPointId);
                        if (vehiclePoint == null)
                            vehiclePoint = spawnPointSet.Find(def.spawnPointId);
                    }
                    if (vehiclePoint == null)
                        vehiclePoint = point;

                    currentVehicle = Instantiate(def.vehiclePrefab, vehiclePoint.position, vehiclePoint.rotation);
                    currentVehicle.name = $"Vehicle_{def.clientId}";
                    Vector3 offset = def.vehicleSpawnLocalOffset != Vector3.zero
                        ? def.vehicleSpawnLocalOffset
                        : defaultVehicleSpawnLocalOffset;
                    currentVehicle.transform.position = vehiclePoint.position + vehiclePoint.rotation * offset;
                    if (snapToGround)
                        SnapRootToGround(currentVehicle.transform);
                    ResetVehiclePhysics(currentVehicle);
                    spawnedVehicles.Add(currentVehicle);
                }

                var prompt = current.GetComponentInChildren<InteractionPrompt>(true);
                if (prompt != null)
                    prompt.SetYarnNode(def.startNode);

                var target = current.GetComponentInChildren<ClientDialogueTarget>(true);
                if (target != null)
                    target.Configure(def.clientId, def.startNode);

                ClientRegistry.SetCurrent(current, def.clientId);

                var behaviour = current.GetComponentInChildren<ClientBehaviour>(true);
                if (behaviour != null)
                    behaviour.ConfigureFromDefinition(def, exitPointSet);

                navigationHUD?.SetDestination(current.transform);

                AfterClientSpawnComplete?.Invoke();
            }
            finally
            {
                spawnInProgress = false;
            }
        }

        /// <summary>
        /// 새 콜 직전: 이전 차량만 제거. 이전 손님 NPC는 걷기 연출이 끝날 때까지 씬에 남깁니다.
        /// </summary>
        private void PrepareForNewSpawn()
        {
            if (currentVehicle != null)
            {
                StripVehiclePhysicsForOverlap(currentVehicle);
                Destroy(currentVehicle);
                currentVehicle = null;
            }

            if (current != null)
            {
                ClientRegistry.ClearIfCurrent(current);
                current = null;
            }
        }

        /// <summary>
        /// Destroy 직전에도 한 프레임 동안 콜라이더가 살아 있을 수 있어, 새 차량과 겹치지 않게 즉시 물리를 끕니다.
        /// </summary>
        private static void StripVehiclePhysicsForOverlap(GameObject root)
        {
            if (root == null) return;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
            var bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                var rb = bodies[i];
                if (rb == null) continue;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
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
            // 날짜가 바뀌면 이전 일차의 손님/차량은 모두 정리합니다(걷는 중 연출 포함).
            DespawnAllSpawned();
            if (!respawnOnDayChanged) return;
            SpawnCurrentClient();
        }

        private void DespawnAllSpawned()
        {
            // 현재 레지스트리 정리
            if (ClientRegistry.CurrentClientObject != null)
                ClientRegistry.ClearIfCurrent(ClientRegistry.CurrentClientObject);

            // 손님 전부 제거(걷기 연출 포함)
            for (int i = spawnedClients.Count - 1; i >= 0; i--)
            {
                var obj = spawnedClients[i];
                if (obj != null)
                    Destroy(obj);
            }
            spawnedClients.Clear();

            // 차량 전부 제거
            for (int i = spawnedVehicles.Count - 1; i >= 0; i--)
            {
                var obj = spawnedVehicles[i];
                if (obj != null)
                    Destroy(obj);
            }
            spawnedVehicles.Clear();

            current = null;
            currentVehicle = null;
        }

        private static void ResetVehiclePhysics(GameObject vehicleRoot)
        {
            var bodies = vehicleRoot.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                var rb = bodies[i];
                if (rb == null) continue;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void SnapRootToGround(Transform root)
        {
            if (root == null) return;
            Vector3 origin = root.position + Vector3.up * Mathf.Max(0.01f, groundSnapRayUp);
            float distance = Mathf.Max(0.01f, groundSnapRayUp + groundSnapRayDown);
            if (Physics.Raycast(origin, Vector3.down, out var hit, distance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                // 바닥에 붙이되, 현재 yaw는 유지
                root.position = hit.point;
            }
        }
    }
}
