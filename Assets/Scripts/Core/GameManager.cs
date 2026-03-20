using UnityEngine;

namespace NightDriver.Core
{
    /// <summary>
    /// 게임 전역 매니저 싱글턴.
    /// App 하위에 배치하고, App 루트 전체가 씬 전환 후에도 유지됩니다.
    /// </summary>
    public sealed class GameManager : SingletonBehaviour<GameManager>
    {
        [Header("Managers")]
        [SerializeField] private NightManager nightManager;
        [SerializeField] private EndingTracker endingTracker;
        [SerializeField] private MetaSaveSystem metaSaveSystem;

        public NightManager NightManager => nightManager;
        public EndingTracker EndingTracker => endingTracker;
        public MetaSaveSystem MetaSaveSystem => metaSaveSystem;

        // ─────────────────────────────────────────────

        protected override bool PersistAcrossScenes => true;

        // App 루트 전체를 Destroy해야 하므로 기본(gameObject만 Destroy)을 override합니다.
        protected override void OnDuplicateInstance()
        {
            var duplicateRoot = transform.root != null ? transform.root.gameObject : gameObject;
            Destroy(duplicateRoot);
        }

        protected override void OnInitialize()
        {
            // 자식에서 자동 탐색 (Inspector에서 직접 할당하는 것을 권장)
            if (metaSaveSystem == null) metaSaveSystem = GetComponentInChildren<MetaSaveSystem>(true);
            if (endingTracker == null)  endingTracker  = GetComponentInChildren<EndingTracker>(true);
            if (nightManager == null)   nightManager   = GetComponentInChildren<NightManager>(true);

            metaSaveSystem?.Load();
        }

        // ─────────────────────────────────────────────

        private void OnApplicationQuit()
        {
            metaSaveSystem?.Save();
        }
    }
}
