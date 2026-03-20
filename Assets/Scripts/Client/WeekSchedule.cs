using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 일차별 손님 순서를 정의하는 ScriptableObject.
    ///
    /// 내부적으로 Dictionary 캐시를 사용해 GetClientFor()를 O(1)로 처리합니다.
    /// Inspector에서 값이 변경되면 OnValidate()로 캐시가 자동 무효화됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "NightDriver/Client/Week Schedule", fileName = "WeekSchedule_")]
    public sealed class WeekSchedule : ScriptableObject
    {
        [Serializable]
        public sealed class DayEntry
        {
            [Range(1, 7)] public int day = 1;
            public List<ClientDefinition> clients = new List<ClientDefinition>();
        }

        [SerializeField] private List<DayEntry> days = new List<DayEntry>();

        // day → 클라이언트 리스트 Dictionary 캐시
        private Dictionary<int, List<ClientDefinition>> cache;

        // ─────────────────────────────────────────────

        private void OnValidate()
        {
            // Inspector 값 변경 시 캐시 무효화
            cache = null;
        }

        private void OnEnable()
        {
            // SO가 로드될 때 미리 빌드
            BuildCache();
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 지정된 일차(day)와 콜 순서(callIndex, 0-based)에 해당하는 ClientDefinition을 반환합니다.
        /// 없으면 null을 반환합니다.
        /// </summary>
        public ClientDefinition GetClientFor(int day, int callIndex)
        {
            if (callIndex < 0) return null;
            if (cache == null) BuildCache();

            if (!cache.TryGetValue(day, out var clients)) return null;
            return callIndex < clients.Count ? clients[callIndex] : null;
        }

        /// <summary>
        /// 지정된 일차에 등록된 손님 수를 반환합니다.
        /// </summary>
        public int GetClientCountFor(int day)
        {
            if (cache == null) BuildCache();
            return cache.TryGetValue(day, out var clients) ? clients.Count : 0;
        }

        // ─────────────────────────────────────────────

        private void BuildCache()
        {
            cache = new Dictionary<int, List<ClientDefinition>>(days.Count);
            for (int i = 0; i < days.Count; i++)
            {
                var e = days[i];
                if (e == null) continue;
                if (cache.ContainsKey(e.day))
                    Debug.LogWarning($"[WeekSchedule] {e.day}일차 중복 등록 — 첫 번째 항목만 사용됩니다.", this);
                else
                    cache[e.day] = e.clients ?? new List<ClientDefinition>();
            }
        }
    }
}
