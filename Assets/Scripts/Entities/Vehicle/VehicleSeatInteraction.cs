using System.Collections.Generic;
using NightDriver.Character;
using NightDriver.Character.Camera;
using NightDriver.Client;
using NightDriver.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.Vehicle
{
    /// <summary>
    /// 차량 탑승/하차 상호작용.
    ///
    /// [탑승]
    /// - 플레이어가 일정 거리 이내면 "탑승하기 [E]" 프롬프트 표시
    /// - E키를 누르면: 페이드 → 카메라(또는 CameraRig)를 DrivingSeat로 이동/부모 변경 → 걷기 비활성 → 주행 활성 → 페이드 인
    ///
    /// [하차]
    /// - canExit(목적지 도착/구역 진입 등)일 때만 F로 하차 가능
    /// - F키를 누르면: 페이드 → 플레이어를 ExitPoint로 이동 → 걷기 활성 → 주행 비활성 → 카메라 복구 → 페이드 인
    /// </summary>
    [AddComponentMenu("NightDriver/Vehicle/Vehicle Seat Interaction")]
    public sealed class VehicleSeatInteraction : MonoBehaviour
    {
        [Header("Transforms")]
        [Tooltip("운전석 위치(카메라가 고정될 자리)")]
        [SerializeField] private Transform drivingSeat;
        [Tooltip("승객(손님)을 붙일 좌석 Transform (비우면 손님은 차량에 탑승하지 않습니다)")]
        [SerializeField] private Transform passengerSeat;
        [Tooltip("하차 위치(플레이어가 내려설 자리)")]
        [SerializeField] private Transform exitPoint;

        [Header("Range / Input")]
        [SerializeField] private float enterRange = 2.5f;
        [SerializeField] private KeyCode enterKey = KeyCode.E;
        [SerializeField] private KeyCode exitKey = KeyCode.F;
        [SerializeField] private string playerTag = "Player";

        [Header("Fade")]
        [SerializeField] private float fadeSeconds = 0.5f;

        [Header("Gates")]
        [Tooltip("true면 외부에서 EnableEnter(true)를 호출하기 전까지 탑승할 수 없습니다. (목적지 선택 후 탑승 활성화용)")]
        [SerializeField] private bool requireEnterEnabled = true;

        [Header("Passenger boarding")]
        [Tooltip("운전석 탑승 후, 손님 루트가 이 거리(미터) 안으로 오면 승객석에 붙입니다.")]
        [SerializeField] private float passengerBoardDistance = 6f;
        [Tooltip("거리 판정 기준점. 비우면 승객석 Transform 위치를 사용합니다.")]
        [SerializeField] private Transform passengerBoardProbeOrigin;

        [Header("Trip lock")]
        [Tooltip("하차·드롭오프 후 이 인스턴스에서는 다시 탑승할 수 없게 됩니다.")]
        [SerializeField] private bool enterPermanentlyLocked;

        [Header("Driver Feel")]
        [Tooltip("true면 운전자는 좌석 위치만 차량을 따라가고, 회전은 수평(Yaw)만 적용합니다. 차량이 굴러도 시야가 같이 뒤집히지 않습니다.")]
        [SerializeField] private bool keepDriverWorldUpright = true;

        [Header("Controllers")]
        [Tooltip("차량 주행 컨트롤러")]
        [SerializeField] private VehicleDriveController driveController;

        [Tooltip("탑승/하차 시 enable 토글할 플레이어 컴포넌트들 (걷기 컨트롤러 등)")]
        [SerializeField] private Behaviour[] playerComponentsToDisable = System.Array.Empty<Behaviour>();

        [Header("Camera")]
        [Tooltip("플레이어 카메라 루트(예: CameraRig). 비우면 Camera.main을 사용합니다.")]
        [SerializeField] private Transform cameraRig;
        [Tooltip("카메라 분리 추적 스크립트(탑승 시 disable, 하차 시 enable)")]
        [SerializeField] private CameraFollowTarget cameraFollowTarget;

        [Header("플레이어 몸(시각)")]
        [Tooltip("탑승 시 카메라만 좌석으로 가고, 캐릭터 메시는 숨깁니다.")]
        [SerializeField] private bool hidePlayerBodyWhenSeated = true;
        [Tooltip("수동으로 숨길 Renderer(비우면 자동: 플레이어 하위 전부, 카메라/카메라리그 하위는 제외)")]
        [SerializeField] private Renderer[] manualBodyRenderers = System.Array.Empty<Renderer>();

        [Header("Prompt UI")]
        [SerializeField] private string enterPromptText = "탑승하기 [E]";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.1f, 0f);
        [SerializeField] private Vector2 canvasSize = new Vector2(260f, 52f);
        [SerializeField] private float canvasScale = 0.008f;

        private Transform playerRoot;
        private GameObject promptRoot;
        private Camera mainCamera;

        private bool isSeated;
        private bool canExit;
        private bool canEnter;

        private Transform originalCamParent;
        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;

        private Transform originalPlayerParent;
        private Vector3 originalPlayerLocalPos;
        private Quaternion originalPlayerLocalRot;

        private Transform originalPassengerParent;
        private Vector3 originalPassengerLocalPos;
        private Quaternion originalPassengerLocalRot;

        private float sqrRange;
        private float sqrPassengerBoardDistance;
        private bool usedUprightDriverFollow;
        private bool passengerAttachedThisRide;

        private CallFlowController _callFlowCache;

        /// <summary>자동 수집한 몸체 Renderer와 원래 enabled 상태</summary>
        private readonly List<Renderer> _bodyRenderers = new List<Renderer>();
        private readonly List<bool> _bodyRendererWasEnabled = new List<bool>();

        private void Awake()
        {
            ActiveInstance = this;
            sqrRange = enterRange * enterRange;
            sqrPassengerBoardDistance = passengerBoardDistance * passengerBoardDistance;
            mainCamera = Camera.main;
            canEnter = !requireEnterEnabled;

            if (drivingSeat == null)
            {
                drivingSeat =
                    transform.Find("DrivingSeat")
                    ?? transform.Find("DriverSeat")
                    ?? transform.Find("Seat")
                    ?? transform;
            }
            if (exitPoint == null)
                exitPoint = transform.Find("ExitPoint");

            if (driveController == null)
                driveController = GetComponentInChildren<VehicleDriveController>(true);

            promptRoot = BuildPromptUI();
            promptRoot.SetActive(false);
        }

        private void OnDisable()
        {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        private void Start()
        {
            var playerObj = GameObject.FindWithTag(playerTag);
            playerRoot = playerObj != null ? playerObj.transform : null;

            if (cameraRig == null)
                cameraRig = Camera.main != null ? Camera.main.transform : null;

            if (cameraFollowTarget == null && cameraRig != null)
                cameraFollowTarget = cameraRig.GetComponent<CameraFollowTarget>();
        }

        private void Update()
        {
            if (playerRoot == null)
            {
                var p = GameObject.FindWithTag(playerTag);
                if (p != null)
                    playerRoot = p.transform;
            }
            if (playerRoot == null)
                return;

            if (!isSeated)
            {
                UpdateEnterPrompt();
                return;
            }

            TryAttachPassengerByProximity();

            // 탑승 중: 하차 키
            if (canExit && Input.GetKeyDown(exitKey))
                ExitVehicle();
        }

        private void LateUpdate()
        {
            if (!isSeated || !usedUprightDriverFollow || drivingSeat == null || playerRoot == null)
                return;
            SyncPlayerRootToDrivingSeatUpright();
        }

        private void SyncPlayerRootToDrivingSeatUpright()
        {
            playerRoot.position = drivingSeat.position;
            Vector3 flatFwd = drivingSeat.forward;
            flatFwd.y = 0f;
            if (flatFwd.sqrMagnitude > 1e-6f)
                playerRoot.rotation = Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
        }

        /// <summary>
        /// <c>Require Enter Enabled</c>일 때는 활성 손님 + CallFlow 게이트를 본다. (인스펙터에서 게이트가 꺼져 있어도 대화 전 UI 방지)
        /// </summary>
        private bool CallFlowAllowsBoardingPrompt()
        {
            if (!requireEnterEnabled)
                return canEnter;

            if (ClientRegistry.CurrentClientObject == null)
                return false;

            if (_callFlowCache == null)
                _callFlowCache = FindObjectOfType<CallFlowController>(true);

            if (_callFlowCache != null)
                return _callFlowCache.AreVehicleBoardingGatesSatisfied;

            return canEnter;
        }

        private void TryAttachPassengerByProximity()
        {
            if (passengerAttachedThisRide || passengerSeat == null)
                return;

            var clientObj = NightDriver.Client.ClientRegistry.CurrentClientObject;
            if (clientObj == null)
                return;
            if (clientObj.transform.parent == passengerSeat)
            {
                passengerAttachedThisRide = true;
                return;
            }

            Transform probe = passengerBoardProbeOrigin != null ? passengerBoardProbeOrigin : passengerSeat;
            float sqr = (clientObj.transform.position - probe.position).sqrMagnitude;
            if (sqr > sqrPassengerBoardDistance)
                return;

            AttachCurrentClientToPassengerSeat();
            passengerAttachedThisRide = true;
        }

        private void UpdateEnterPrompt()
        {
            if (drivingSeat == null) return;
            if (enterPermanentlyLocked)
            {
                if (promptRoot != null && promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            // canEnter만 먼저 보면, CallFlow 게이트는 통과했는데 EnableEnter가 아직/영구 스킵된 프레임에 프롬프트가 영원히 안 뜹니다.
            if (!CallFlowAllowsBoardingPrompt())
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            // 차량 루트가 차체 중심이면 문·운전석에서 2.5m 밖으로 나가 프롬프트가 안 뜨는 경우가 많아 운전석 기준으로 판정합니다.
            Vector3 enterProbe = drivingSeat != null ? drivingSeat.position : transform.position;
            float sqr = (playerRoot.position - enterProbe).sqrMagnitude;
            bool inRange = sqr <= sqrRange;

            if (!inRange)
            {
                if (promptRoot.activeSelf) promptRoot.SetActive(false);
                return;
            }

            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
            BillboardPrompt();

            if (Input.GetKeyDown(enterKey))
                EnterVehicle();
        }

        private void EnterVehicle()
        {
            if (isSeated) return;
            if (drivingSeat == null) return;
            if (!CallFlowAllowsBoardingPrompt())
                return;

            var fader = ScreenFader.Instance;
            if (fader == null)
            {
                // 페이더가 없으면 즉시 처리(테스트 편의)
                DoEnter();
                return;
            }

            fader.FadeOutIn(fadeSeconds, DoEnter);
        }

        private void DoEnter()
        {
            isSeated = true;
            PlayerControlLock.VehicleSeated = true;

            if (promptRoot != null) promptRoot.SetActive(false);

            // 플레이어 위치는 좌석을 따라가야 트리거/도착 판정이 맞습니다.
            // keepDriverWorldUpright: 굴러가는 차량 자식이 되지 않고, 매 프레임 위치+수평 요만 동기화합니다.
            if (playerRoot != null)
            {
                originalPlayerParent = playerRoot.parent;
                originalPlayerLocalPos = playerRoot.localPosition;
                originalPlayerLocalRot = playerRoot.localRotation;

                var cc = playerRoot.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                usedUprightDriverFollow = keepDriverWorldUpright;
                if (keepDriverWorldUpright)
                {
                    playerRoot.SetParent(null, true);
                    SyncPlayerRootToDrivingSeatUpright();
                }
                else
                {
                    playerRoot.SetParent(drivingSeat, worldPositionStays: false);
                    playerRoot.localPosition = Vector3.zero;
                    playerRoot.localRotation = Quaternion.identity;
                }
            }

            // 카메라(또는 CameraRig): 수직 시야 모드에서는 플레이어 자식으로 두어 몸과 같이 이동
            if (cameraRig != null)
            {
                originalCamParent = cameraRig.parent;
                originalCamLocalPos = cameraRig.localPosition;
                originalCamLocalRot = cameraRig.localRotation;

                if (cameraFollowTarget != null)
                    cameraFollowTarget.enabled = false;

                if (keepDriverWorldUpright && playerRoot != null)
                    cameraRig.SetParent(playerRoot, worldPositionStays: true);
                else
                {
                    cameraRig.SetParent(drivingSeat, worldPositionStays: false);
                    cameraRig.localPosition = Vector3.zero;
                    cameraRig.localRotation = Quaternion.identity;
                }
            }

            // 플레이어 걷기 컨트롤러 등 비활성화
            for (int i = 0; i < playerComponentsToDisable.Length; i++)
            {
                if (playerComponentsToDisable[i] != null)
                    playerComponentsToDisable[i].enabled = false;
            }

            // 주행 컨트롤러 활성화
            if (driveController != null)
                driveController.enabled = true;

            if (hidePlayerBodyWhenSeated)
                SetPlayerBodyVisible(false);

            passengerAttachedThisRide = false;
        }

        private void ExitVehicle()
        {
            if (!isSeated) return;

            var fader = ScreenFader.Instance;
            if (fader == null)
            {
                DoExit();
                return;
            }

            fader.FadeOutIn(fadeSeconds, DoExit);
        }

        private void DoExit()
        {
            if (hidePlayerBodyWhenSeated)
                SetPlayerBodyVisible(true);

            isSeated = false;
            PlayerControlLock.VehicleSeated = false;

            // 주행 컨트롤러 비활성화
            if (driveController != null)
                driveController.enabled = false;

            // 플레이어를 하차 지점으로 이동
            if (exitPoint != null && playerRoot != null)
            {
                if (playerRoot.parent == drivingSeat)
                    playerRoot.SetParent(originalPlayerParent, worldPositionStays: true);

                playerRoot.position = exitPoint.position;
                playerRoot.rotation = exitPoint.rotation;

                var cc = playerRoot.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }
            else if (playerRoot != null && (playerRoot.parent == drivingSeat || usedUprightDriverFollow))
            {
                if (playerRoot.parent == drivingSeat)
                    playerRoot.SetParent(originalPlayerParent, worldPositionStays: true);
                else if (usedUprightDriverFollow && originalPlayerParent != null)
                {
                    playerRoot.SetParent(originalPlayerParent, true);
                    playerRoot.localPosition = originalPlayerLocalPos;
                    playerRoot.localRotation = originalPlayerLocalRot;
                }

                var cc = playerRoot.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }

            usedUprightDriverFollow = false;
            passengerAttachedThisRide = false;

            // 카메라 복구
            if (cameraRig != null)
            {
                cameraRig.SetParent(originalCamParent, worldPositionStays: false);
                cameraRig.localPosition = originalCamLocalPos;
                cameraRig.localRotation = originalCamLocalRot;

                // 하차 후 다시 분리 추적 재개
                if (cameraFollowTarget != null)
                    cameraFollowTarget.enabled = true;
            }

            // 플레이어 걷기 컨트롤러 등 활성화
            for (int i = 0; i < playerComponentsToDisable.Length; i++)
            {
                if (playerComponentsToDisable[i] != null)
                    playerComponentsToDisable[i].enabled = true;
            }

            DetachCurrentClientFromPassengerSeat();
        }

        private void AttachCurrentClientToPassengerSeat()
        {
            var clientObj = NightDriver.Client.ClientRegistry.CurrentClientObject;
            if (clientObj == null) return;

            originalPassengerParent = clientObj.transform.parent;
            originalPassengerLocalPos = clientObj.transform.localPosition;
            originalPassengerLocalRot = clientObj.transform.localRotation;

            clientObj.transform.SetParent(passengerSeat, worldPositionStays: false);
            clientObj.transform.localPosition = Vector3.zero;
            clientObj.transform.localRotation = Quaternion.identity;
        }

        private void DetachCurrentClientFromPassengerSeat()
        {
            var clientObj = NightDriver.Client.ClientRegistry.CurrentClientObject;
            if (clientObj == null) return;
            if (passengerSeat == null) return;
            if (clientObj.transform.parent != passengerSeat) return;

            clientObj.transform.SetParent(originalPassengerParent, worldPositionStays: true);
        }

        public static VehicleSeatInteraction ActiveInstance { get; private set; }

        /// <summary>플레이어가 운전석에 탑승한 상태인지(승객 근접 탑승 판정 등).</summary>
        public bool IsDriverSeated => isSeated;

        /// <summary>
        /// 외부에서 탑승 가능 여부를 제어합니다. (목적지 선택 이후에만 탑승 허용 등)
        /// </summary>
        public void EnableEnter(bool value)
        {
            if (value && enterPermanentlyLocked)
                return;
            canEnter = value;
            if (!canEnter && promptRoot != null) promptRoot.SetActive(false);
        }

        /// <summary>
        /// 이번 콜이 끝난 뒤(목적지 하차 등) 같은 차량에 재탑승하지 못하게 합니다.
        /// </summary>
        public void SetEnterPermanentlyLocked(bool locked)
        {
            enterPermanentlyLocked = locked;
            if (locked)
            {
                canEnter = false;
                if (promptRoot != null) promptRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 드롭오프/하차 연출 직전에 손님을 차량에서 분리합니다.
        /// </summary>
        public static void ReleasePassengerIfSeated()
        {
            if (ActiveInstance == null) return;
            ActiveInstance.passengerAttachedThisRide = false;
            ActiveInstance.DetachCurrentClientFromPassengerSeat();
        }

        /// <summary>
        /// 외부(손님 하차 처리 등)에서 강제로 하차를 트리거합니다.
        /// ExitPermissionZone을 통하지 않는 드롭오프 흐름에서 사용합니다.
        /// </summary>
        public void ForceExit()
        {
            if (!isSeated) return;
            ExitVehicle();
        }

        /// <summary>
        /// 목적지 도착/특정 구역 진입 시 하차 가능 여부를 외부에서 제어합니다.
        /// </summary>
        public void SetCanExit(bool value) => canExit = value;

        /// <summary>
        /// 탑승 중 몸 메시 숨김/하차 시 복구.
        /// 카메라가 붙은 오브젝트(및 CameraRig 하위)의 Renderer는 건드리지 않습니다.
        /// </summary>
        private void SetPlayerBodyVisible(bool visible)
        {
            if (playerRoot == null) return;

            if (visible)
            {
                for (int i = 0; i < _bodyRenderers.Count; i++)
                {
                    if (_bodyRenderers[i] == null) continue;
                    _bodyRenderers[i].enabled = _bodyRendererWasEnabled[i];
                }
                return;
            }

            _bodyRenderers.Clear();
            _bodyRendererWasEnabled.Clear();

            if (manualBodyRenderers != null && manualBodyRenderers.Length > 0)
            {
                foreach (var r in manualBodyRenderers)
                {
                    if (r == null) continue;
                    _bodyRenderers.Add(r);
                    _bodyRendererWasEnabled.Add(r.enabled);
                    r.enabled = false;
                }
                return;
            }

            var all = playerRoot.GetComponentsInChildren<Renderer>(true);
            Transform camTf = cameraRig != null ? cameraRig : (Camera.main != null ? Camera.main.transform : null);

            foreach (var r in all)
            {
                if (r == null) continue;
                if (camTf != null && (r.transform == camTf || r.transform.IsChildOf(camTf)))
                    continue;

                _bodyRenderers.Add(r);
                _bodyRendererWasEnabled.Add(r.enabled);
                r.enabled = false;
            }
        }

        private void BillboardPrompt()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || promptRoot == null) return;

            Vector3 anchor = drivingSeat != null ? drivingSeat.position : transform.position;
            promptRoot.transform.position = anchor + promptOffset;
            promptRoot.transform.rotation =
                Quaternion.LookRotation(promptRoot.transform.position - mainCamera.transform.position);
        }

        private GameObject BuildPromptUI()
        {
            var root = new GameObject("[VehicleEnterPromptCanvas]");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = promptOffset;
            root.transform.localScale = Vector3.one * canvasScale;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
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
            tmp.text = enterPromptText;
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

        private void OnValidate()
        {
            sqrPassengerBoardDistance = passengerBoardDistance * passengerBoardDistance;
        }
    }
}

