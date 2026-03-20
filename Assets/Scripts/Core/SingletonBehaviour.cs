using UnityEngine;

namespace NightDriver.Core
{
    /// <summary>
    /// 씬당 단일 인스턴스를 보장하는 제네릭 싱글턴 MonoBehaviour 기반 클래스.
    ///
    /// 사용 예시:
    ///   public sealed class GameManager : SingletonBehaviour&lt;GameManager&gt;
    ///   {
    ///       protected override bool PersistAcrossScenes => true;
    ///       protected override void OnInitialize() { /* Awake 로직 */ }
    ///   }
    /// </summary>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        /// <summary>
        /// true이면 씬 전환 후에도 파괴되지 않습니다(DontDestroyOnLoad).
        /// 루트 GameObject 전체에 적용됩니다.
        /// </summary>
        protected virtual bool PersistAcrossScenes => false;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                OnDuplicateInstance();
                return;
            }

            Instance = this as T;

            if (PersistAcrossScenes)
            {
                var root = transform.root != null ? transform.root.gameObject : gameObject;
                DontDestroyOnLoad(root);
            }

            OnInitialize();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this as T) Instance = null;
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 중복 인스턴스가 감지되었을 때 호출됩니다.
        /// 기본 동작: 자기 자신(gameObject)을 Destroy합니다.
        /// 루트 오브젝트를 Destroy해야 하는 경우 override하세요.
        /// </summary>
        protected virtual void OnDuplicateInstance()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// 싱글턴 등록 직후 호출됩니다. Awake 로직을 여기에 작성하세요.
        /// </summary>
        protected virtual void OnInitialize() { }
    }
}
