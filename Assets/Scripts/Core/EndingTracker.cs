using System;
using UnityEngine;

namespace NightDriver.Core
{
    public enum EndingType
    {
        None = 0,
        A_Liberation = 1,
        B_Obsession = 2,
        C_Denial = 3,
    }

    [Serializable]
    public sealed class EndingProgress
    {
        [Range(0, 10)] public int ascensionsSucceeded;
        public EndingType resolvedEnding = EndingType.None;
    }

    public sealed class EndingTracker : MonoBehaviour
    {
        public const int AscensionThresholdForDay7 = 4;

        public event Action<int> OnAscensionCountChanged;
        public event Action<EndingType> OnEndingResolved;

        [SerializeField] private EndingProgress progress = new EndingProgress();

        public int AscensionsSucceeded => progress.ascensionsSucceeded;
        public EndingType ResolvedEnding => progress.resolvedEnding;

        public void ResetRunCounters()
        {
            progress.ascensionsSucceeded = 0;
            progress.resolvedEnding = EndingType.None;
            OnAscensionCountChanged?.Invoke(progress.ascensionsSucceeded);
        }

        public void RecordAscensionSuccess()
        {
            progress.ascensionsSucceeded = Mathf.Clamp(progress.ascensionsSucceeded + 1, 0, 999);
            OnAscensionCountChanged?.Invoke(progress.ascensionsSucceeded);
        }

        public bool CanMeetBrotherOnDay7()
        {
            return progress.ascensionsSucceeded >= AscensionThresholdForDay7;
        }

        public EndingType ResolveEndingForDay7(bool canMeetBrother, bool choseBrotherDesiredDestination)
        {
            if (!canMeetBrother)
            {
                progress.resolvedEnding = EndingType.C_Denial;
            }
            else
            {
                progress.resolvedEnding = choseBrotherDesiredDestination ? EndingType.A_Liberation : EndingType.B_Obsession;
            }

            OnEndingResolved?.Invoke(progress.resolvedEnding);
            return progress.resolvedEnding;
        }
    }
}
