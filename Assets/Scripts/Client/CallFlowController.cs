using NightDriver.Core;
using NightDriver.Dialogue;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 개발용: "대화가 끝나면 콜 완료 → 다음 손님 리스폰" 흐름을 연결합니다.
    /// </summary>
    public sealed class CallFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NightManager nightManager;
        [SerializeField] private ClientSpawner spawner;
        [SerializeField] private DialogueService dialogue;

        [Header("Behavior")]
        [SerializeField] private bool spawnOnEnable = true;
        [SerializeField] private bool advanceCallOnDialogueComplete = true;

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null) nightManager = GameManager.Instance.NightManager;
            if (spawner == null) spawner = FindFirstObjectByType<ClientSpawner>();
            if (dialogue == null) dialogue = DialogueService.Instance != null ? DialogueService.Instance : FindFirstObjectByType<DialogueService>();
        }

        private void OnEnable()
        {
            if (dialogue != null) dialogue.OnDialogueCompleted += HandleDialogueCompleted;
            if (spawnOnEnable) spawner?.SpawnCurrentClient();
        }

        private void OnDisable()
        {
            if (dialogue != null) dialogue.OnDialogueCompleted -= HandleDialogueCompleted;
        }

        private void HandleDialogueCompleted()
        {
            if (!advanceCallOnDialogueComplete) return;
            if (nightManager == null) return;

            nightManager.CompleteOneCall();
            spawner?.SpawnCurrentClient();
        }
    }
}

