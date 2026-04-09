using System;
using NightDriver.Dialogue;
using NightDriver.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace NightDriver.Client
{
    /// <summary>
    /// 목적지 도착 존.
    /// 활성화되면 플레이어 접근 시 "목적지 도착 [E]" 프롬프트를 표시하고,
    /// E 입력으로 도착 대화를 시작합니다.
    /// 도착 대화가 끝나면 현재 손님을 하차시켜 exitPoint까지 걷게 만듭니다.
    /// </summary>
    public sealed class DestinationZone : MonoBehaviour
    {
        public static event Action OnAnyClientDroppedOff;

        [Header("Identity")]
        [SerializeField] private string zoneId;
        public string ZoneId => zoneId;

        [Header("Dialogue")]
        [SerializeField] private string arrivalYarnNode = "Destination_Building";
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueService dialogueService;

        [Header("Player Detect")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float arrivalRadius = 5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Client Exit")]
        [SerializeField] private Transform exitPoint;

        [Header("Prompt UI")]
        [SerializeField] private string promptText = "목적지 도착 [E]";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 canvasSize = new Vector2(260f, 52f);
        [SerializeField] private float canvasScale = 0.008f;

        private Transform playerTransform;
        private Camera mainCamera;
        private GameObject promptRoot;
        private float sqrArrivalRadius;
        private bool waitingArrivalDialogueComplete;

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
            if (playerTransform == null || promptRoot == null) return;
            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
            bool inRange = sqrDist <= sqrArrivalRadius;
            if (!inRange)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
            BillboardUpdate();

            if (Input.GetKeyDown(interactKey))
                StartArrivalDialogue();
        }

        private void StartArrivalDialogue()
        {
            if (dialogueRunner == null || string.IsNullOrWhiteSpace(arrivalYarnNode)) return;
            if (dialogueRunner.IsDialogueRunning) return;

            waitingArrivalDialogueComplete = true;
            promptRoot.SetActive(false);
            dialogueRunner.StartDialogue(arrivalYarnNode);
        }

        private void HandleDialogueCompleted()
        {
            if (!waitingArrivalDialogueComplete) return;
            waitingArrivalDialogueComplete = false;

            HandleClientDropoff();
            gameObject.SetActive(false);
        }

        private void HandleClientDropoff()
        {
            var clientObj = ClientRegistry.CurrentClientObject;
            if (clientObj == null)
            {
                OnAnyClientDroppedOff?.Invoke();
                return;
            }

            var clientNpc = clientObj.GetComponentInChildren<ClientNPC>(true);
            if (clientNpc == null)
            {
                OnAnyClientDroppedOff?.Invoke();
                return;
            }

            void OnWalkEnd()
            {
                clientNpc.OnWalkOffComplete -= OnWalkEnd;
                OnAnyClientDroppedOff?.Invoke();
            }

            clientNpc.OnWalkOffComplete += OnWalkEnd;
            clientNpc.WalkOff(exitPoint);
        }

        private void BillboardUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            promptRoot.transform.rotation =
                Quaternion.LookRotation(promptRoot.transform.position - mainCamera.transform.position);
        }

        private GameObject BuildPromptUI()
        {
            var root = new GameObject("[DestinationPromptCanvas]");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = promptOffset;
            root.transform.localScale = Vector3.one * canvasScale;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 11;

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
            tmp.text = promptText;
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
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.12f);
            Gizmos.DrawSphere(transform.position, arrivalRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, arrivalRadius);
        }
    }
}
