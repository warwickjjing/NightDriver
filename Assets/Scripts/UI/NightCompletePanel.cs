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
    /// 이 스크립트가 붙은 오브젝트 전체를 비활성화해도 <see cref="BindAllInScene"/>로 구독한 <c>OnNightCompleted</c>는 유지됩니다.
    /// (표시만 끄는 경우 <c>OnDisable</c>에서 구독을 해제하면 <see cref="GameManager"/>보다 먼저 구독된 뒤 <c>Awake</c>에서 꺼질 때 리스너가 사라지는 버그가 납니다.)
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
            // panelRoot가 본인이면, 씬 시작 시 비활성이었다가 OnNightCompleted로 첫 활성화될 때
            // 이 Awake가 그때 처음 돌며 ApplyInitialHiddenState로 다시 꺼버리는 문제를 피합니다.
            // 초기 숨김은 BindAllInScene이 처리합니다.
            if (panelRoot != null && panelRoot != gameObject)
                ApplyInitialHiddenState();
        }

        private void OnEnable()
        {
            EnsureSubscribed(nightManager);
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
