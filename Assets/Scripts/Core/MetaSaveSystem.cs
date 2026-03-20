using System;
using UnityEngine;

namespace NightDriver.Core
{
    [Serializable]
    public sealed class MetaSaveData
    {
        public int cash;
        public int soulShards;
        public int memoryPieces;

        public string[] unlockedUpgradeIds = Array.Empty<string>();
    }

    public sealed class MetaSaveSystem : MonoBehaviour
    {
        private const string PlayerPrefsKey = "NightDriver.MetaSave.v1";

        public event Action<MetaSaveData> OnLoaded;
        public event Action<MetaSaveData> OnSaved;

        [SerializeField] private MetaSaveData data = new MetaSaveData();

        public MetaSaveData Data => data;

        public void Load()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                data = new MetaSaveData();
                OnLoaded?.Invoke(data);
                return;
            }

            var json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                data = new MetaSaveData();
                OnLoaded?.Invoke(data);
                return;
            }

            try
            {
                data = JsonUtility.FromJson<MetaSaveData>(json) ?? new MetaSaveData();
            }
            catch
            {
                data = new MetaSaveData();
            }

            OnLoaded?.Invoke(data);
        }

        public void Save()
        {
            try
            {
                var json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
                OnSaved?.Invoke(data);
            }
            catch
            {
                // Intentionally swallow to avoid blocking quit on save failure.
            }
        }
    }
}
