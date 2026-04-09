using System;
using UnityEngine;

namespace NightDriver.Character.Abilities
{
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
