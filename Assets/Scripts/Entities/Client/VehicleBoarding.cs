using NightDriver.Character;
using NightDriver.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.Client
{
    /// <summary>
    /// 플레이어가 차량 근처에서 탑승(E) 시 좌석에 붙이고 sit 애니메이션을 재생합니다.
    /// 목적지가 설정된 뒤 HUD를 차량으로 안내하려면 NavigationCommandHandler와 연동됩니다.
    /// </summary>
    [AddComponentMenu("NightDriver/Client/Vehicle Boarding")]
    public sealed class VehicleBoarding : MonoBehaviour
    {
        public static VehicleBoarding ActiveInstance { get; private set; }

        [Header("탑승 판정")]
        [SerializeField] private float boardRadius = 3f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode boardKey = KeyCode.E;

        [Header("좌석 (인스펙터에서 배치)")]
        [Tooltip("플레이어를 붙일 좌석 Transform (비우면 루트 기준)")]
        [SerializeField] private Transform seatTransform;
        [Tooltip("seatTransform이 비어있으면 'Seat' 자식 Transform을 자동 생성합니다.")]
        [SerializeField] private bool autoCreateSeatTransform = true;
        [SerializeField] private Vector3 autoSeatLocalPosition = new Vector3(0.25f, 1.05f, 0.15f);
        [SerializeField] private Vector3 autoSeatLocalEuler = new Vector3(0f, 0f, 0f);

        [Tooltip("HUD 화살표가 가리킬 지점 (비우면 이 오브젝트 transform)")]
        [SerializeField] private Transform hudGuidePoint;

        [Header("플레이어 애니메이션")]
        [SerializeField] private string sitBoolParameter = "Sit";
        [Tooltip("Sit를 Bool 대신 Trigger로 쓰려면 켜기")]
        [SerializeField] private bool useSitTrigger;

        [Header("프롬프트")]
        [SerializeField] private string promptText = "탑승 [E]";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private Vector2 canvasSize = new Vector2(260f, 52f);
        [SerializeField] private float canvasScale = 0.008f;

        [Header("탑승 후")]
        [SerializeField] private bool lockMovementAfterBoard = true;
        [Tooltip("탑승 직후 목적지 위치로 HUD 전환 (ClientBehaviour 활성 목적지)")]
        [SerializeField] private bool switchHudToDestinationAfterBoard = true;
        [Tooltip("탑승/하차 시 enable 토글할 컴포넌트들 (예: 차량 운전 스크립트)")]
        [SerializeField] private Behaviour[] toggleOnBoarding = System.Array.Empty<Behaviour>();

        private Transform playerRoot;
        private Animator playerAnimator;
        private CharacterController playerController;

        private GameObject promptRoot;
        private float sqrBoardRadius;
        private bool isBoarded;
        private NavigationHUD navigationHUD;

        public bool IsBoarded => isBoarded;
        public Transform HudGuideTransform => hudGuidePoint != null ? hudGuidePoint : transform;

        /// <summary>
        /// 하차·목적지 하차 연출 직전에 플레이어를 좌석에서 풀어 줍니다.
        /// </summary>
        public static void ReleasePlayerIfBoarded()
        {
            if (ActiveInstance == null) return;
            ActiveInstance.ReleasePlayerInternal();
        }

        private void Awake()
        {
            sqrBoardRadius = boardRadius * boardRadius;
            if (seatTransform == null)
            {
                if (autoCreateSeatTransform)
                {
                    var existing = transform.Find("Seat");
                    if (existing != null)
                        seatTransform = existing;
                    else
                    {
                        var seat = new GameObject("Seat");
                        seat.transform.SetParent(transform, false);
                        seat.transform.localPosition = autoSeatLocalPosition;
                        seat.transform.localRotation = Quaternion.Euler(autoSeatLocalEuler);
                        seatTransform = seat.transform;
                    }
                }
                else
                {
                    seatTransform = transform;
                }
            }

            navigationHUD = FindFirstObjectByType<NavigationHUD>();
            promptRoot = BuildPromptUI();
            promptRoot.SetActive(false);
        }

        private void OnEnable()
        {
            ActiveInstance = this;
            var p = GameObject.FindWithTag(playerTag);
            playerRoot = p != null ? p.transform : null;
            CachePlayerComponents();
        }

        private void OnDisable()
        {
            if (ActiveInstance == this) ActiveInstance = null;
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        private void Update()
        {
            if (isBoarded || playerRoot == null || promptRoot == null) return;

            if (PhoneUIController.IsAnyPhoneVisible)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            float sqr = (playerRoot.position - transform.position).sqrMagnitude;
            bool inRange = sqr <= sqrBoardRadius;

            if (!inRange)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
            BillboardPrompt();

            if (Input.GetKeyDown(boardKey))
                TryBoard();
        }

        private void CachePlayerComponents()
        {
            if (playerRoot == null) return;
            playerAnimator = playerRoot.GetComponentInChildren<Animator>(true);
            playerController = playerRoot.GetComponent<CharacterController>();
        }

        private void ReleasePlayerInternal()
        {
            if (!isBoarded) return;

            var p = GameObject.FindWithTag(playerTag);
            var root = p != null ? p.transform : playerRoot;
            if (root == null)
            {
                isBoarded = false;
                PlayerControlLock.VehicleSeated = false;
                return;
            }

            playerAnimator = root.GetComponentInChildren<Animator>(true);
            playerController = root.GetComponent<CharacterController>();

            root.SetParent(null, worldPositionStays: true);
            if (playerController != null) playerController.enabled = true;

            if (playerAnimator != null && !useSitTrigger)
                playerAnimator.SetBool(sitBoolParameter, false);

            for (int i = 0; i < toggleOnBoarding.Length; i++)
            {
                if (toggleOnBoarding[i] != null)
                    toggleOnBoarding[i].enabled = false;
            }

            isBoarded = false;
            PlayerControlLock.VehicleSeated = false;
        }

        private void TryBoard()
        {
            if (isBoarded) return;
            CachePlayerComponents();

            if (playerRoot == null) return;

            isBoarded = true;
            if (promptRoot != null) promptRoot.SetActive(false);

            if (playerController != null) playerController.enabled = false;

            playerRoot.SetParent(seatTransform, worldPositionStays: false);
            playerRoot.localPosition = Vector3.zero;
            playerRoot.localRotation = Quaternion.identity;

            if (playerAnimator != null)
            {
                if (useSitTrigger)
                {
                    playerAnimator.ResetTrigger(sitBoolParameter);
                    playerAnimator.SetTrigger(sitBoolParameter);
                }
                else
                    playerAnimator.SetBool(sitBoolParameter, true);
            }

            if (lockMovementAfterBoard)
                PlayerControlLock.VehicleSeated = true;

            for (int i = 0; i < toggleOnBoarding.Length; i++)
            {
                if (toggleOnBoarding[i] != null)
                    toggleOnBoarding[i].enabled = true;
            }

            if (switchHudToDestinationAfterBoard && navigationHUD != null)
            {
                var client = ClientRegistry.CurrentClientObject;
                var behaviour = client != null
                    ? client.GetComponentInChildren<ClientBehaviour>(true)
                    : null;
                var dest = behaviour != null ? behaviour.GetActiveDestinationLocation() : null;
                if (dest != null)
                    navigationHUD.SetDestination(dest);
            }
        }

        private void BillboardPrompt()
        {
            var cam = Camera.main;
            if (cam == null || promptRoot == null) return;
            promptRoot.transform.position = transform.position + promptOffset;
            promptRoot.transform.rotation =
                Quaternion.LookRotation(promptRoot.transform.position - cam.transform.position);
        }

        private GameObject BuildPromptUI()
        {
            var root = new GameObject("[VehicleBoardPrompt]");
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

            return root;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Gizmos.DrawSphere(transform.position, boardRadius);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, boardRadius);
        }
    }
}
