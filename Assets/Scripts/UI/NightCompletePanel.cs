using NightDriver.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.UI
{
    /// <summary>
    /// 당일 콜 할당량 완료 시 패널을 띄우고, 버튼으로 다음 일차로 넘깁니다.
    ///
    /// <see cref="NightManager.OnNightCompleted"/>는 마지막 콜 하차 후 <see cref="NightManager.CompleteOneCall"/>에서
    /// <c>callsCompleted &gt;= callsPerNight</c>일 때 발생합니다.
    ///
    /// 이 스크립트가 붙은 오브젝트 전체를 비활성화하면 <c>OnEnable</c>이 돌지 않아 이벤트에 붙지 못합니다.
    /// <see cref="BindAllInScene"/>가 비활성 인스턴스까지 찾아 구독하므로, <see cref="Core.GameManager"/>에서 자동 호출됩니다.
    /// </summary>
    public sealed class NightCompletePanel : MonoBehaviour
    {
        [SerializeField] private NightManager nightManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button nextDayButton;

        private NightManager _subscribedTo;
        private bool _buttonHooked;

        /// <summary>씬에 있는 모든 패널(비활성 포함)을 <paramref name="nightManager"/> 이벤트에 연결합니다.</summary>
        public static void BindAllInScene(NightManager nightManager)
        {
            if (nightManager == null)
                return;

            var panels = FindObjectsByType<NightCompletePanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                var p = panels[i];
                if (p == null || !p.gameObject.scene.IsValid())
                    continue;
                if (p.nightManager == null)
                    p.nightManager = nightManager;
                p.ApplyInitialHiddenState();
                p.EnsureSubscribed(p.nightManager);
            }
        }

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
            ApplyInitialHiddenState();
        }

        private void OnEnable()
        {
            EnsureSubscribed(nightManager);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ApplyInitialHiddenState()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void EnsureSubscribed(NightManager manager)
        {
            if (manager == null)
                return;
            if (_subscribedTo == manager)
            {
                HookButtonOnce();
                return;
            }

            Unsubscribe();
            _subscribedTo = manager;
            _subscribedTo.OnNightCompleted += HandleNightCompleted;
            HookButtonOnce();
        }

        private void Unsubscribe()
        {
            if (_subscribedTo != null)
            {
                _subscribedTo.OnNightCompleted -= HandleNightCompleted;
                _subscribedTo = null;
            }
        }

        private void HookButtonOnce()
        {
            if (nextDayButton == null || _buttonHooked)
                return;
            nextDayButton.onClick.AddListener(OnClickNextDay);
            _buttonHooked = true;
        }

        private void HandleNightCompleted()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void OnClickNextDay()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            (_subscribedTo ?? nightManager)?.AdvanceToNextNight();
        }
    }
}
