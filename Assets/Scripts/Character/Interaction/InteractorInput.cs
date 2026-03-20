using TMPro;
using UnityEngine;

namespace NightDriver.Character.Interaction
{
    public sealed class InteractorInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("References")]
        [SerializeField] private Interactor interactor;
        [Tooltip("비워두면 씬에서 첫 TMP_Text를 찾습니다(권장: 직접 할당).")]
        [SerializeField] private TMP_Text promptText;

        private void Awake()
        {
            if (interactor == null) interactor = GetComponentInChildren<Interactor>(true) ?? GetComponent<Interactor>();
            if (promptText == null) promptText = FindFirstObjectByType<TMP_Text>();
        }

        private void Update()
        {
            if (interactor == null) return;

            var target = interactor.FindBest();
            if (promptText != null)
            {
                if (target == null)
                {
                    promptText.text = string.Empty;
                    promptText.enabled = false;
                }
                else
                {
                    promptText.text = target.GetPrompt(gameObject);
                    promptText.enabled = true;
                }
            }

            if (target != null && Input.GetKeyDown(interactKey))
            {
                interactor.TryInteract();
            }
        }
    }
}

