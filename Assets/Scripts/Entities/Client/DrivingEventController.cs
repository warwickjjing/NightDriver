using NightDriver.Core;
using NightDriver.Vehicle;
using UnityEngine;
using Yarn.Unity;

namespace NightDriver.Client
{
    /// <summary>
    /// 차량 운전 중 정의된 구역·일차·손님에 맞는 Yarn 이벤트를 1회 실행합니다.
    /// </summary>
    public sealed class DrivingEventController : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Binding
        {
            public DrivingEventDefinition definition;
            [Tooltip("트리거 구역 중심(차량/월드 기준)")]
            public Transform triggerCenter;
        }

        [SerializeField] private Binding[] bindings = System.Array.Empty<Binding>();
        [SerializeField] private NightManager nightManager;
        [SerializeField] private DialogueRunner dialogueRunner;

        private bool[] firedForCurrentDay;

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
            if (dialogueRunner == null)
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        private void OnEnable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged += OnDayChanged;
            ResetFiredFlags();
        }

        private void OnDisable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged -= OnDayChanged;
        }

        private void OnDayChanged(int _) => ResetFiredFlags();

        private void ResetFiredFlags()
        {
            int n = bindings != null ? bindings.Length : 0;
            firedForCurrentDay = new bool[n];
        }

        private void Update()
        {
            var vehicle = VehicleSeatInteraction.ActiveInstance;
            if (vehicle == null || !vehicle.IsDriverSeated)
                return;
            if (nightManager == null || bindings == null || dialogueRunner == null)
                return;
            if (dialogueRunner.IsDialogueRunning)
                return;

            int day = nightManager.State.dayIndex;
            Vector3 vehiclePos = vehicle.transform.position;

            for (int i = 0; i < bindings.Length; i++)
            {
                if (firedForCurrentDay[i]) continue;

                var b = bindings[i];
                if (b?.definition == null || b.triggerCenter == null) continue;
                var def = b.definition;
                if (def.dayIndex != day) continue;

                if (!string.IsNullOrWhiteSpace(def.requiredClientId)
                    && !string.Equals(
                        ClientRegistry.CurrentClientId,
                        def.requiredClientId,
                        System.StringComparison.Ordinal))
                    continue;

                float r = Mathf.Max(0.1f, def.triggerRadius);
                float sqr = r * r;
                if ((vehiclePos - b.triggerCenter.position).sqrMagnitude > sqr)
                    continue;

                if (string.IsNullOrWhiteSpace(def.yarnNodeName))
                    continue;

                firedForCurrentDay[i] = true;
                dialogueRunner.StartDialogue(def.yarnNodeName);
                return;
            }
        }
    }
}
