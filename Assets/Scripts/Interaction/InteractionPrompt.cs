using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace NightDriver.Interaction
{
    /// <summary>
    /// NPC에 부착하는 상호작용 컴포넌트.
    ///
    /// - 플레이어가 일정 거리 이내로 접근하면 NPC 머리 위에 World Space 프롬프트 UI를 자동 생성합니다.
    /// - E키(또는 설정한 키)를 누르면 지정된 Yarn 노드로 대화를 시작합니다.
    /// - 여러 NPC가 동시에 범위 내에 있을 경우, 가장 가까운 NPC의 프롬프트만 표시합니다.
    ///
    /// InteractionConfig SO를 할당하면 여러 NPC가 같은 설정을 공유할 수 있습니다.
    /// SO를 비워두면 인스펙터 직접 설정값을 사용합니다.
    /// </summary>
    public sealed class InteractionPrompt : MonoBehaviour
    {
        // 여러 NPC 중 가장 가까운 NPC를 추적하는 정적 변수
        private static InteractionPrompt s_CurrentCandidate;

        // ─────────────────────────────────────────────

        [Header("Config (SO — 비워두면 아래 직접 설정값 사용)")]
        [SerializeField] private InteractionConfig config;

        [Header("Yarn 대화 설정")]
        [Tooltip("E키 입력 시 시작할 Yarn 노드 이름")]
        [SerializeField] private string yarnNodeName = "Start";

        [Tooltip("씬의 DialogueRunner. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private DialogueRunner dialogueRunner;

        [Header("거리 감지 (Config SO 미할당 시 사용)")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("프롬프트 UI (Config SO 미할당 시 사용)")]
        [SerializeField] private string promptText = "말걸기 [E]";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 canvasSize = new Vector2(220f, 52f);
        [SerializeField] private float canvasScale = 0.008f;

        // ─────────────────────────────────────────────
        // 런타임 캐시 (SO 또는 직접 설정값 중 하나로 초기화됨)
        private float _sqrInteractionDistance;
        private string _playerTag;
        private KeyCode _interactKey;
        private string _promptText;
        private Vector3 _promptOffset;
        private Vector2 _canvasSize;
        private float _canvasScale;

        private Transform playerTransform;
        private Camera mainCamera;
        private GameObject promptRoot;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            ApplyConfig();

            if (dialogueRunner == null)
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();

            var runnerState = dialogueRunner != null ? "FOUND" : "MISSING";
            var projectState = (dialogueRunner != null && dialogueRunner.YarnProject != null) ? "FOUND" : "MISSING";
            Debug.Log($"[InteractionPrompt] Awake | runner={runnerState}, yarnProject={projectState}, node='{yarnNodeName}'", this);

            promptRoot = BuildPromptUI();
            promptRoot.SetActive(false);
        }

        private void Start()
        {
            mainCamera = Camera.main;

            var playerObj = GameObject.FindWithTag(_playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning(
                    $"[InteractionPrompt] '{_playerTag}' 태그를 가진 플레이어를 찾을 수 없습니다. " +
                    "플레이어 오브젝트의 Tag를 설정해주세요.", this);
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // ── 대화 진행 중에는 프롬프트 숨김 ────────────────────────
            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                if (s_CurrentCandidate == this) ClearCandidate();
                return;
            }

            // ── 거리 판정 ───────────────────────────────────────────
            float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
            bool inRange = sqrDist <= _sqrInteractionDistance;

            if (inRange)
            {
                if (s_CurrentCandidate == null
                    || s_CurrentCandidate == this
                    || sqrDist < GetCandidateSqrDist())
                {
                    s_CurrentCandidate = this;
                }
            }
            else
            {
                if (s_CurrentCandidate == this) ClearCandidate();
                return;
            }

            // ── 내가 현재 후보일 때만 프롬프트 표시 + 입력 처리 ──────
            if (s_CurrentCandidate != this) return;

            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
            BillboardUpdate();

            if (Input.GetKeyDown(_interactKey))
                TryStartDialogue();
        }

        private void OnDisable()
        {
            if (s_CurrentCandidate == this) ClearCandidate();
        }

        private void OnDestroy()
        {
            if (s_CurrentCandidate == this) ClearCandidate();
        }

        // ─────────────────────────────────────────────

        private void ClearCandidate()
        {
            s_CurrentCandidate = null;
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        private float GetCandidateSqrDist()
        {
            if (s_CurrentCandidate == null || playerTransform == null)
                return float.PositiveInfinity;
            return (playerTransform.position - s_CurrentCandidate.transform.position).sqrMagnitude;
        }

        private void TryStartDialogue()
        {
            if (dialogueRunner == null)
            {
                Debug.LogWarning("[InteractionPrompt] DialogueRunner가 연결되지 않았습니다.", this);
                return;
            }
            if (string.IsNullOrWhiteSpace(yarnNodeName))
            {
                Debug.LogWarning("[InteractionPrompt] Yarn Node Name이 비어 있습니다.", this);
                return;
            }
            if (dialogueRunner.IsDialogueRunning) return;

            bool nodeExists = false;
            string[] nodeNames = null;
            if (dialogueRunner.YarnProject != null)
            {
                nodeNames = dialogueRunner.YarnProject.NodeNames;
                nodeExists = nodeNames != null && System.Array.Exists(nodeNames, n => n == yarnNodeName);
            }

            Debug.Log(
                $"[InteractionPrompt] StartDialogue requested | node='{yarnNodeName}', existsInProject={nodeExists}, nodeCount={(nodeNames != null ? nodeNames.Length : 0)}",
                this);

            dialogueRunner.StartDialogue(yarnNodeName);
        }

        private void BillboardUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || promptRoot == null) return;

            promptRoot.transform.rotation =
                Quaternion.LookRotation(promptRoot.transform.position - mainCamera.transform.position);
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// SO가 있으면 SO 값을, 없으면 인스펙터 직접 설정값을 런타임 필드에 복사합니다.
        /// </summary>
        private void ApplyConfig()
        {
            float dist;
            if (config != null)
            {
                dist           = config.interactionDistance;
                _playerTag     = config.playerTag;
                _interactKey   = config.interactKey;
                _promptText    = string.IsNullOrWhiteSpace(config.defaultPromptText) ? promptText : config.defaultPromptText;
                _promptOffset  = config.promptOffset;
                _canvasSize    = config.canvasSize;
                _canvasScale   = config.canvasScale;
            }
            else
            {
                dist           = interactionDistance;
                _playerTag     = playerTag;
                _interactKey   = interactKey;
                _promptText    = promptText;
                _promptOffset  = promptOffset;
                _canvasSize    = canvasSize;
                _canvasScale   = canvasScale;
            }

            _sqrInteractionDistance = dist * dist;
        }

        /// <summary>
        /// 검은 배경 + 흰색 텍스트의 World Space Canvas를 코드로 생성합니다.
        /// Inspector에서 별도 오브젝트를 만들 필요 없습니다.
        /// </summary>
        private GameObject BuildPromptUI()
        {
            // ── 루트: World Space Canvas ─────────────────────────
            var root = new GameObject("[PromptCanvas]");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = _promptOffset;
            root.transform.localScale    = Vector3.one * _canvasScale;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            root.GetComponent<RectTransform>().sizeDelta = _canvasSize;

            // ── 검은 배경 패널 ────────────────────────────────────
            var bg      = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);

            var bgImage = bg.AddComponent<Image>();
            bgImage.color         = new Color(0f, 0f, 0f, 0.82f);
            bgImage.raycastTarget = false;

            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // ── 흰색 TMP 텍스트 ───────────────────────────────────
            var textGO = new GameObject("Label");
            textGO.transform.SetParent(root.transform, false);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = _promptText;
            tmp.color         = Color.white;
            tmp.fontSize      = 5;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f,  4f);
            textRt.offsetMax = new Vector2(-10f, -4f);

            return root;
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 런타임에서 Yarn 노드 이름을 교체합니다.
        /// ClientSpawner가 손님 스폰 후 자동으로 주입할 때 사용합니다.
        /// </summary>
        public void SetYarnNode(string nodeName)
        {
            if (!string.IsNullOrWhiteSpace(nodeName))
            {
                yarnNodeName = nodeName;
                Debug.Log($"[InteractionPrompt] SetYarnNode injected | node='{yarnNodeName}'", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float dist = config != null ? config.interactionDistance : interactionDistance;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
            Gizmos.DrawSphere(transform.position, dist);
            Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
            Gizmos.DrawWireSphere(transform.position, dist);
        }
    }
}
