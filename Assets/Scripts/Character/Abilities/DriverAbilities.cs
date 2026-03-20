using System;
using UnityEngine;

namespace NightDriver.Character.Abilities
{
    [Flags]
    public enum AbilityFlags
    {
        None = 0,
        AfterimageSense = 1 << 0,   // 잔상 감지
        DeepAfterimage = 1 << 1,    // 깊은 잔상
        Intuition = 1 << 2,         // 직감
        Foresight = 1 << 3,         // 예지
        Empathy = 1 << 4,           // 공감
        Communion = 1 << 5,         // 교감
    }

    [Serializable]
    public sealed class DriverAbilityState
    {
        public AbilityFlags unlocked = AbilityFlags.None;
    }

    public sealed class DriverAbilities : MonoBehaviour
    {
        public event Action<AbilityFlags> OnAbilitiesChanged;

        [SerializeField] private DriverAbilityState state = new DriverAbilityState();
        public AbilityFlags Unlocked => state.unlocked;

        public bool Has(AbilityFlags flag) => (state.unlocked & flag) == flag;

        public void Unlock(AbilityFlags flag)
        {
            if (flag == AbilityFlags.None) return;
            var next = state.unlocked | flag;
            if (next == state.unlocked) return;
            state.unlocked = next;
            OnAbilitiesChanged?.Invoke(state.unlocked);
        }

        public void LockAll()
        {
            if (state.unlocked == AbilityFlags.None) return;
            state.unlocked = AbilityFlags.None;
            OnAbilitiesChanged?.Invoke(state.unlocked);
        }
    }
}
