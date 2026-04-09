using System;
using UnityEngine;
using NightDriver.Core;

namespace NightDriver.Character
{
    public sealed class PlayerCharacter : MonoBehaviour
    {
        public event Action<int> OnCashChanged;
        public event Action<int> OnSoulShardsChanged;
        public event Action<int> OnMemoryPiecesChanged;

        [Header("Runtime Currencies (meta-backed)")]
        [SerializeField] private int cash;
        [SerializeField] private int soulShards;
        [SerializeField] private int memoryPieces;

        public int Cash => cash;
        public int SoulShards => soulShards;
        public int MemoryPieces => memoryPieces;

        private MetaSaveSystem save;

        private void Awake()
        {
            save = GameManager.Instance != null ? GameManager.Instance.MetaSaveSystem : null;
            if (save != null)
            {
                PullFromSave(save.Data);
                save.OnLoaded += PullFromSave;
                save.OnSaved += PullFromSave;
            }
        }

        private void OnDestroy()
        {
            if (save != null)
            {
                save.OnLoaded -= PullFromSave;
                save.OnSaved -= PullFromSave;
            }
        }

        private void PullFromSave(MetaSaveData data)
        {
            cash = data.cash;
            soulShards = data.soulShards;
            memoryPieces = data.memoryPieces;

            OnCashChanged?.Invoke(cash);
            OnSoulShardsChanged?.Invoke(soulShards);
            OnMemoryPiecesChanged?.Invoke(memoryPieces);
        }

        public void AddCash(int amount)
        {
            if (amount <= 0) return;
            cash = SafeAdd(cash, amount);
            if (save != null) save.Data.cash = cash;
            OnCashChanged?.Invoke(cash);
        }

        public bool TrySpendCash(int amount)
        {
            if (amount <= 0) return true;
            if (cash < amount) return false;
            cash -= amount;
            if (save != null) save.Data.cash = cash;
            OnCashChanged?.Invoke(cash);
            return true;
        }

        public void AddSoulShards(int amount)
        {
            if (amount <= 0) return;
            soulShards = SafeAdd(soulShards, amount);
            if (save != null) save.Data.soulShards = soulShards;
            OnSoulShardsChanged?.Invoke(soulShards);
        }

        public bool TrySpendSoulShards(int amount)
        {
            if (amount <= 0) return true;
            if (soulShards < amount) return false;
            soulShards -= amount;
            if (save != null) save.Data.soulShards = soulShards;
            OnSoulShardsChanged?.Invoke(soulShards);
            return true;
        }

        public void AddMemoryPieces(int amount)
        {
            if (amount <= 0) return;
            memoryPieces = SafeAdd(memoryPieces, amount);
            if (save != null) save.Data.memoryPieces = memoryPieces;
            OnMemoryPiecesChanged?.Invoke(memoryPieces);
        }

        private static int SafeAdd(int a, int b)
        {
            long sum = (long)a + b;
            if (sum > int.MaxValue) return int.MaxValue;
            if (sum < int.MinValue) return int.MinValue;
            return (int)sum;
        }
    }
}
