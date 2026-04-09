using System;
using NightDriver.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace NightDriver.Client
{
    /// <summary>
    /// 클라이언트 Prefab 단에서 목적지 선택/도착/하차 연출을 관리합니다.
    /// </summary>
    public sealed class ClientBehaviour : MonoBehaviour
    {
        [Serializable]
        public sealed class DestinationEntry
        {
            [Tooltip("Yarn의 <<setDestination id>> 와 매칭되는 목적지 ID")]
            public string id;
            [Tooltip("도착 판정에 사용할 목적지 위치")]
            public Transform location;
            [Tooltip("하차 후 손님이 걸어갈 목표 지점")]
            public Transform exitPoint;
            [Tooltip("도착 시 실행할 Yarn 노드 (비워두면 대화 없이 즉시 하차)")]
            public string arrivalYarnNode;
        }

        public static event Action OnAnyClientDroppedOff;

        [Header("Yarn")]
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueService dialogueService;

        [Header("Destinations")]
        [SerializeField] private DestinationEntry[] destinations = Array.Empty<DestinationEntry>();

        [Header("Arrival Detect")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float arrivalRadius = 5f;
        [SerializeField] private KeyCode dropOffKey = KeyCode.E;

        [Header("Prompt UI")]
        [SerializeField] private string arrivalPromptText = "내리기 [E]";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 canvasSize = new Vector2(220f, 52f);
        [SerializeField] private float canvasScale = 0.008f;

        private Transform playerTransform;
        private Camera mainCamera;
        private GameObject promptRoot;
        private DestinationEntry activeDestination;
        private bool waitingArrivalDialogueComplete;
        private float sqrArrivalRadius;

        /// <summary>
        /// 목적지가 선택된 뒤에는 손님에게 말걸기(InteractionPrompt)를 막기 위해 사용합니다.
        /// </summary>
        public bool IsPickupDialogueBlocked { get; private set; }

        /// <summary>
        /// 목적지 하차 플로우가 시작된 뒤에는 말걸기·재탑승이 불가합니다.
        /// </summary>
        public bool TripFinished { get; private set; }

        /// <summary>
        /// ClientDefinition 기반으로 목적지 규칙(도착 노드/ExitPoint)을 주입합니다.
        /// - location은 손님 프리팹의 destinations[] 설정을 우선 사용합니다.
        /// - exitPointId가 지정된 경우 ExitPointSet에서 찾아 덮어씁니다.
        /// - arrivalYarnNode는 destinationRules로 덮어씁니다.
        /// </summary>
        public void ConfigureFromDefinition(
            ClientDefinition def,
            ExitPointSet exitPointSet)
        {
            if (def == null) return;
            if (def.destinationRules == null || def.destinationRules.Length == 0) return;

            for (int i = 0; i < def.destinationRules.Length; i++)
            {
                var rule = def.destinationRules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.destinationId)) continue;

                for (int j = 0; j < destinations.Length; j++)
                {
                    var entry = destinations[j];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                    if (!string.Equals(entry.id, rule.destinationId, StringComparison.Ordinal)) continue;

                    // arrival node override
                    entry.arrivalYarnNode = rule.arrivalYarnNode;

                    // exit point override by id
                    if (exitPointSet != null && !string.IsNullOrWhiteSpace(rule.exitPointId))
                    {
                        var ep = exitPointSet.Find(rule.exitPointId);
                        if (ep != null) entry.exitPoint = ep;
                    }

                    break;
                }
            }

        }

        private void Awake()
        {
            sqrArrivalRadius = arrivalRadius * arrivalRadius;
            mainCamera = Camera.main;

            if (dialogueRunner == null)
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            if (dialogueService == null)
                dialogueService = DialogueService.Instance != null
                    ? DialogueService.Instance
                    : FindFirstObjectByType<DialogueService>();

            promptRoot = BuildPromptUI();
            promptRoot.SetActive(false);
        }

        private void OnEnable()
        {
            var player = GameObject.FindWithTag(playerTag);
            playerTransform = player != null ? player.transform : null;

            if (dialogueService != null)
                dialogueService.OnDialogueCompleted += HandleDialogueCompleted;
        }

        private void OnDisable()
        {
            if (dialogueService != null)
                dialogueService.OnDialogueCompleted -= HandleDialogueCompleted;

            waitingArrivalDialogueComplete = false;
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        private void Update()
        {
            if (activeDestination == null || activeDestination.location == null)
            {
                if (promptRoot != null && promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            if (playerTransform == null || promptRoot == null) return;

            // 하차 플로우가 시작되었거나 이미 완료된 이후에는 프롬프트/입력을 다시 열지 않습니다.
            if (TripFinished || waitingArrivalDialogueComplete)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            // 대화 중에는 도착 프롬프트를 숨깁니다.
            // DialogueRunner 직접 참조는 씬 구성에 따라 DialogueService의 runner와 불일치할 수 있어,
            // 잠금/상태 판정은 DialogueService 기준으로 통일합니다.
            if (dialogueService != null && dialogueService.IsRunning)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            float sqrDist = (playerTransform.position - activeDestination.location.position).sqrMagnitude;
            bool inRange = sqrDist <= sqrArrivalRadius;

            if (!inRange)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
            BillboardUpdate();

            if (Input.GetKeyDown(dropOffKey))
                StartArrivalFlow();
        }

        /// <summary>
        /// 목적지 ID를 활성화합니다.
        /// </summary>
        public void ArmDestination(string id)
        {
            activeDestination = null;
            IsPickupDialogueBlocked = false;
            if (string.IsNullOrWhiteSpace(id)) return;

            for (int i = 0; i < destinations.Length; i++)
            {
                var entry = destinations[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                if (string.Equals(entry.id, id, StringComparison.Ordinal))
                {
                    activeDestination = entry;
                    break;
                }
            }

            if (activeDestination != null)
                IsPickupDialogueBlocked = true;
        }

        /// <summary>
        /// 현재 활성화된 목적지 Transform (HUD·탑승 후 안내 등).
        /// </summary>
        public Transform GetActiveDestinationLocation()
        {
            return activeDestination != null ? activeDestination.location : null;
        }

        /// <summary>
        /// HUD에서 사용할 목적지 위치를 반환합니다.
        /// </summary>
        public Transform GetDestinationLocation(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < destinations.Length; i++)
            {
                var entry = destinations[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                if (string.Equals(entry.id, id, StringComparison.Ordinal))
                    return entry.location;
            }
            return null;
        }

        private void StartArrivalFlow()
        {
            if (activeDestination == null) return;
            if (TripFinished || waitingArrivalDialogueComplete) return;

            if (string.IsNullOrWhiteSpace(activeDestination.arrivalYarnNode))
            {
                DropOffClient();
                return;
            }

            // DialogueService 기준으로 대화를 시작하고, 완료 이벤트로 DropOffClient()를 연결합니다.
            if (dialogueService == null)
            {
                Debug.LogWarning("[ClientBehaviour] DialogueService를 찾을 수 없어 도착 대화를 시작할 수 없습니다. 즉시 하차 처리합니다.", this);
                DropOffClient();
                return;
            }

            if (dialogueService.IsRunning)
            {
                // 이미 다른 대화가 떠 있으면 도착 프롬프트만 숨기고 대기합니다. (E 연타로 재시작 시도 방지)
                if (promptRoot != null && promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            waitingArrivalDialogueComplete = true;
            promptRoot.SetActive(false);
            bool started = dialogueService.TryStart(activeDestination.arrivalYarnNode);
            if (!started)
            {
                Debug.LogWarning($"[ClientBehaviour] 도착 대화 '{activeDestination.arrivalYarnNode}' 시작 실패. (runner 미설정/노드명 오류/대화 진행 중) 즉시 하차 처리합니다.", this);
                waitingArrivalDialogueComplete = false;
                DropOffClient();
            }
        }

        private void HandleDialogueCompleted()
        {
            if (!waitingArrivalDialogueComplete) return;
            waitingArrivalDialogueComplete = false;
            DropOffClient();
        }

        private void DropOffClient()
        {
            TripFinished = true;
            // 목적지 도착 이후에는 HUD를 즉시 숨깁니다.
            var hud = FindFirstObjectByType<NightDriver.UI.NavigationHUD>();
            if (hud != null)
                hud.SetDestination(null);
            if (NightDriver.Vehicle.VehicleSeatInteraction.ActiveInstance != null)
                NightDriver.Vehicle.VehicleSeatInteraction.ActiveInstance.SetEnterPermanentlyLocked(true);

            VehicleBoarding.ReleasePlayerIfBoarded();
            NightDriver.Vehicle.VehicleSeatInteraction.ReleasePassengerIfSeated();
            if (NightDriver.Vehicle.VehicleSeatInteraction.ActiveInstance != null)
                NightDriver.Vehicle.VehicleSeatInteraction.ActiveInstance.ForceExit();

            var clientObj = ClientRegistry.CurrentClientObject;
            if (clientObj == null)
            {
                OnAnyClientDroppedOff?.Invoke();
                return;
            }

            // 다음 콜을 즉시 받을 수 있도록, "현재 손님" 레지스트리는 드롭오프 시작 시점에 해제합니다.
            // (손님이 WalkOff로 걸어가는 연출은 계속 진행)
            ClientRegistry.ClearIfCurrent(clientObj);

            var clientNpc = clientObj.GetComponentInChildren<ClientNPC>(true);
            if (clientNpc == null)
            {
                OnAnyClientDroppedOff?.Invoke();
                return;
            }

            Transform exitPoint = activeDestination != null ? activeDestination.exitPoint : null;

            void OnWalkEnd()
            {
                clientNpc.OnWalkOffComplete -= OnWalkEnd;
                activeDestination = null;
                if (promptRoot != null) promptRoot.SetActive(false);
            }

            clientNpc.OnWalkOffComplete += OnWalkEnd;
            clientNpc.WalkOff(exitPoint);

            // 콜 완료/다음 콜 수락 가능 상태 전환은 WalkOff 완료를 기다리지 않고 즉시 처리합니다.
            OnAnyClientDroppedOff?.Invoke();
            Debug.Log("DropOffClient Call");
        }

        private void BillboardUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            promptRoot.transform.position = activeDestination.location.position + promptOffset;
            promptRoot.transform.rotation =
                Quaternion.LookRotation(promptRoot.transform.position - mainCamera.transform.position);
        }

        private GameObject BuildPromptUI()
        {
            var root = new GameObject("[ClientArrivalPromptCanvas]");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = promptOffset;
            root.transform.localScale = Vector3.one * canvasScale;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;
            root.GetComponent<RectTransform>().sizeDelta = canvasSize;

            var bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.82f);
            bgImage.raycastTarget = false;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(root.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = arrivalPromptText;
            tmp.color = Color.white;
            tmp.fontSize = 5;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);

            return root;
        }

        private void OnDrawGizmosSelected()
        {
            if (activeDestination == null || activeDestination.location == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.12f);
            Gizmos.DrawSphere(activeDestination.location.position, arrivalRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 1f);
            Gizmos.DrawWireSphere(activeDestination.location.position, arrivalRadius);
        }
    }
}
