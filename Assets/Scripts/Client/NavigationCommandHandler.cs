using NightDriver.UI;
using UnityEngine;
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

        private void Awake()
        {
            if (navigationHUD == null)
                navigationHUD = FindFirstObjectByType<NavigationHUD>();

            if (dialogueRunner == null)
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();

            if (dialogueRunner != null)
                RegisterCommands();
            else
                Debug.LogWarning("[NavigationCommandHandler] DialogueRunner를 찾을 수 없습니다.", this);
        }

        // ─────────────────────────────────────────────

        private void RegisterCommands()
        {
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

                var boarding = VehicleBoarding.ActiveInstance;
                if (boarding != null && !boarding.IsBoarded)
                    navigationHUD?.SetDestination(boarding.HudGuideTransform);
                else
                    navigationHUD?.SetDestination(location);
            });
        }
    }
}
