using System;
using UnityEngine;

namespace NightDriver.Character
{
    [Serializable]
    public sealed class CharacterProgress
    {
        [Range(0, 999)] public int trafficViolations;
        [Range(0, 999)] public int perfectClueCollections;
        [Range(0, 999)] public int runsCompleted;
    }

    public sealed class CharacterProgressComponent : MonoBehaviour
    {
        public event Action<int> OnTrafficViolationsChanged;

        [SerializeField] private CharacterProgress progress = new CharacterProgress();
        public CharacterProgress Progress => progress;

        public void ResetNightCounters()
        {
            progress.trafficViolations = 0;
            OnTrafficViolationsChanged?.Invoke(progress.trafficViolations);
        }

        public void AddTrafficViolation()
        {
            progress.trafficViolations = Mathf.Clamp(progress.trafficViolations + 1, 0, 999);
            OnTrafficViolationsChanged?.Invoke(progress.trafficViolations);
        }
    }
}
