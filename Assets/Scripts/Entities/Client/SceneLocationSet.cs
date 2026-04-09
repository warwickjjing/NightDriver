using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// ID → Transform 매핑을 Dictionary 캐시로 관리하는 추상 기반 클래스.
    ///
    /// SpawnPointSet / DestinationSet 이 이 클래스를 상속해 코드 중복을 제거합니다.
    /// 하위 클래스는 별도 로직 없이 [AddComponentMenu] 어트리뷰트만 붙이면 됩니다.
    ///
    /// 성능:
    ///   - 내부적으로 Dictionary를 캐시해 Find()를 O(1)로 처리합니다.
    ///   - 에디터에서 Inspector 값이 변경되면 OnValidate()로 캐시를 자동 무효화합니다.
    /// </summary>
    public abstract class SceneLocationSet : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Find()에서 사용할 고유 ID")]
            public string id;
            [Tooltip("해당 위치의 Transform")]
            public Transform point;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        // Dictionary 캐시 — Awake 또는 첫 Find() 호출 시 빌드됩니다.
        private Dictionary<string, Transform> cache;

        // ─────────────────────────────────────────────

        protected virtual void Awake() => BuildCache();

        private void OnValidate()
        {
            // Inspector에서 값이 바뀌면 다음 Find() 호출 시 다시 빌드합니다.
            cache = null;
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// ID에 해당하는 Transform을 O(1)로 반환합니다.
        /// 해당 ID가 없으면 null을 반환합니다.
        /// </summary>
        public Transform Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (cache == null) BuildCache();
            return cache.TryGetValue(id, out var t) ? t : null;
        }

        /// <summary>
        /// 등록된 모든 Entry를 열거합니다.
        /// </summary>
        public IReadOnlyList<Entry> Entries => entries;

        // ─────────────────────────────────────────────

        private void BuildCache()
        {
            cache = new Dictionary<string, Transform>(entries.Count, StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.id) || e.point == null) continue;
                if (!cache.ContainsKey(e.id))
                    cache[e.id] = e.point;
                else
                    Debug.LogWarning($"[{GetType().Name}] 중복 ID '{e.id}' — 첫 번째 항목만 등록됩니다.", this);
            }
        }
    }
}
