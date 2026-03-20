using NightDriver.Character.Interaction;
using NightDriver.Client;
using UnityEngine;

namespace NightDriver.Dialogue
{
    /// <summary>
    /// 손님에게 붙이는 상호작용 컴포넌트.
    /// - Interactor가 주변에서 IInteractable을 찾고, CanInteract/Interact를 호출합니다.
    /// - 실제 대화 시작은 DialogueService를 통해 Yarn으로 위임합니다.
    /// </summary>
    public sealed class InteractableClientDialogue : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private ClientDialogueTarget target;

        [Header("Prompt")]
        [SerializeField] private string prompt = "E: 말 걸기";

        private void Awake()
        {
            if (target == null) target = GetComponent<ClientDialogueTarget>();
        }

        public bool CanInteract(GameObject interactor)
        {
            // 기본 정책: 대화가 이미 진행 중이면 상호작용 금지
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return false;
            // 이번 손님만 말 걸 수 있도록 제한
            var selfRoot = transform.root != null ? transform.root.gameObject : gameObject;
            if (ClientRegistry.CurrentClientObject != null && ClientRegistry.CurrentClientObject != selfRoot) return false;
            return target != null && !string.IsNullOrWhiteSpace(target.StartNode);
        }

        public void Interact(GameObject interactor)
        {
            if (target == null) return;
            DialogueService.Instance?.TryStart(target.StartNode);
        }

        public string GetPrompt(GameObject interactor)
        {
            if (target != null && !string.IsNullOrWhiteSpace(target.ClientId))
            {
                return $"{prompt} ({target.ClientId})";
            }
            return prompt;
        }
    }
}

