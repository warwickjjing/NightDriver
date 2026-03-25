using NightDriver.Core;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// "손님 하차 완료" 시점에 콜 완료 처리 후 다음 손님을 스폰합니다.
    /// </summary>
    public sealed class CallFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NightManager nightManager;
        [SerializeField] private ClientSpawner spawner;

        [Header("Behavior")]
        [SerializeField] private bool spawnOnEnable = false;
        [SerializeField] private bool advanceCallOnDropoff = true;

        private void Awake()
        {
            // 씬에 저장된 spawnOnEnable=true가 남아 있으면 시작 시 자동 스폰됩니다.
            // 폰 콜 수락 플로우에서는 항상 비활성화합니다.
            spawnOnEnable = false;

            if (nightManager == null && GameManager.Instance != null) nightManager = GameManager.Instance.NightManager;
            if (spawner == null) spawner = FindFirstObjectByType<ClientSpawner>();
        }

        private void OnEnable()
        {
            ClientBehaviour.OnAnyClientDroppedOff += HandleClientDroppedOff;
            if (spawnOnEnable) spawner?.SpawnCurrentClient();
        }

        private void OnDisable()
        {
            ClientBehaviour.OnAnyClientDroppedOff -= HandleClientDroppedOff;
        }

        private void HandleClientDroppedOff()
        {
            if (!advanceCallOnDropoff) return;
            if (nightManager == null) return;

            nightManager.CompleteOneCall();
            spawner?.SpawnCurrentClient();
        }
    }
}

