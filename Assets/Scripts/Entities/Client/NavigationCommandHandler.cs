using NightDriver.UI;
using UnityEngine;
using NightDriver.Dialogue;
using Yarn.Unity;

namespace NightDriver.Client
{
    /// <summary>
    /// Yarn 커맨드를 통해 NavigationHUD와 ClientBehaviour 목적지를 제어합니다.
    ///
    /// ▸ <<setDestination 목적지ID>>
    ///     - NavigationHUD 화살표를 해당 목적지로 전환
    ///     - 현재 손님의 ClientBehaviour에서 목적지를 활성화
    ///
    /// ▸ <<setDestination none>>  (또는 빈 문자열)
    ///     - HUD 숨김 + 활성 목적지 해제
    ///
    /// ▸ <<pickupComplete>>
    ///     - 픽업 대화 완료 플래그. <<setDestination>>과 함께 만족되어야 차량 탑승이 허용됩니다.
    ///
    /// 이 컴포넌트는 DialogueRunner가 있는 오브젝트(또는 그 자식)에 배치하세요.
    /// 실제 목적지/도착 처리는 현재 손님의 ClientBehaviour가 담당합니다.
    /// </summary>
    public sealed class NavigationCommandHandler : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("방향 HUD. 비워두면 씬에서 자동 탐색합니다.")]
        [SerializeField] private NavigationHUD navigationHUD;

        [Tooltip("커맨드를 등록할 DialogueRunner. 비워두면 씬에서 자동 탐색합니다.")]
        [SerializeField] private DialogueRunner dialogueRunner;

        [Tooltip("비우면 씬에서 자동 탐색합니다. 목적지 확정 후 차량 탑승 허용은 CallFlow에 위임합니다.")]
        [SerializeField] private CallFlowController callFlow;

        private bool commandsRegistered;

        private void Awake()
        {
            if (navigationHUD == null)
                navigationHUD = FindFirstObjectByType<NavigationHUD>();

            ResolveCallFlow();

            TryResolveDialogueRunner();
        }

        private void ResolveCallFlow()
        {
            if (callFlow != null)
                return;
            // Unity 2022.x 등: FindFirstObjectByType(FindObjectsInactive, …) 오버로드가 없을 수 있음 → 비활성 포함 탐색
            callFlow = FindObjectOfType<CallFlowController>(true);
        }

        private void OnEnable()
        {
            // 씬 로드 순서에 따라 DialogueRunner가 Awake 때 아직 없을 수 있어 OnEnable에서도 재시도합니다.
            TryResolveDialogueRunner();
            TryRegisterCommands();
        }

        // ─────────────────────────────────────────────

        private void TryResolveDialogueRunner()
        {
            // DialogueService가 사용하는 Runner가 "진짜" 실행 러너이므로 우선 사용합니다.
            if (DialogueService.Instance != null && DialogueService.Instance.Runner != null)
            {
                dialogueRunner = DialogueService.Instance.Runner;
                return;
            }

            if (dialogueRunner == null)
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        private void TryRegisterCommands()
        {
            if (commandsRegistered) return;
            if (dialogueRunner == null)
            {
                Debug.LogWarning("[NavigationCommandHandler] DialogueRunner를 찾을 수 없습니다.", this);
                return;
            }
            RegisterCommands();
            commandsRegistered = true;
        }

        private void RegisterCommands()
        {
            dialogueRunner.AddCommandHandler("pickupComplete", () =>
            {
                ResolveCallFlow();
                callFlow?.NotifyPickupDialogueComplete();
            });

            // <<setDestination 목적지ID>>
            dialogueRunner.AddCommandHandler<string>("setDestination", destinationId =>
            {
                if (string.IsNullOrWhiteSpace(destinationId)
                    || destinationId.ToLowerInvariant() == "none")
                {
                    navigationHUD?.SetDestination(null);
                    var noneClient = ClientRegistry.CurrentClientObject;
                    var noneBehaviour = noneClient != null
                        ? noneClient.GetComponentInChildren<ClientBehaviour>(true)
                        : null;
                    noneBehaviour?.ArmDestination(string.Empty);
                    ResolveCallFlow();
                    callFlow?.NotifyDestinationCleared();
                    return;
                }

                var client = ClientRegistry.CurrentClientObject;
                var behaviour = client != null
                    ? client.GetComponentInChildren<ClientBehaviour>(true)
                    : null;
                if (behaviour == null)
                {
                    Debug.LogWarning(
                        "[NavigationCommandHandler] 현재 손님의 ClientBehaviour를 찾을 수 없습니다.", this);
                    return;
                }

                behaviour.ArmDestination(destinationId);
                var location = behaviour.GetDestinationLocation(destinationId);
                if (location == null)
                {
                    Debug.LogWarning(
                        $"[NavigationCommandHandler] ClientBehaviour에서 목적지 '{destinationId}'를 찾을 수 없습니다.", this);
                    return;
                }

                // Yarn에서 지정한 목적지 Transform을 그대로 사용합니다.
                // (예전에는 VehicleBoarding이 있으면 차량 쪽으로 HUD를 돌렸는데, 목적지 HUD가 엇나가는 원인이 되어 제거했습니다.)
                navigationHUD?.SetDestination(location);

                ResolveCallFlow();
                callFlow?.NotifyDestinationChosen();
            });
        }
    }
}
