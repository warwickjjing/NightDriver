using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 폰 내부 화면 전환을 담당합니다.
    /// 홈 화면(배경)과 각 앱 화면을 SetActive로 교체합니다.
    ///
    /// [화면 흐름]
    ///   폰 열기               → ShowHome()         (홈 화면)
    ///   콜 수신 + 폰 열기     → ShowCallApp()       (MAK CHA 앱)
    ///   수락/거절/뒤로가기    → ShowHome()          (홈 화면으로 복귀)
    ///   폰 닫기               → (자동으로 다음 열기 시 ShowHome으로 초기화)
    ///
    /// [Inspector 배치]
    ///   PhonePanel 하위 오브젝트에 이 컴포넌트를 붙이세요.
    ///   homeScreen, callAppScreen 등 각 화면 오브젝트를 연결하면 됩니다.
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Phone Screen Manager")]
    public sealed class PhoneScreenManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 싱글톤

        public static PhoneScreenManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — 화면 오브젝트

        [Header("홈 화면")]
        [Tooltip("폰을 처음 열었을 때 보이는 배경/홈 화면 오브젝트")]
        [SerializeField] private GameObject homeScreen;

        [Header("앱 화면들")]
        [Tooltip("MAK CHA 콜 앱 화면 (PhoneCallApp 컴포넌트가 붙은 오브젝트)")]
        [SerializeField] private GameObject callAppScreen;

        [Tooltip("도보 네비게이션 앱 화면 (PhoneNaviApp 컴포넌트가 붙은 오브젝트) — 선택")]
        [SerializeField] private GameObject naviAppScreen;

        // 추후 앱 화면이 늘어나면 여기에 추가하세요.
        // [SerializeField] private GameObject settingsScreen;

        // ─────────────────────────────────────────────────────────────────────
        // 현재 화면 상태

        /// <summary>현재 활성화된 화면 오브젝트 (null이면 홈)</summary>
        public GameObject CurrentScreen { get; private set; }

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 시작 시 모든 앱 화면 숨기기 (홈만 표시)
            HideAllAppScreens();
            SetScreenActive(homeScreen, true);
            CurrentScreen = homeScreen;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API

        /// <summary>
        /// 홈 화면으로 돌아갑니다.
        /// 뒤로가기 버튼, 수락/거절 완료 후 자동 복귀 시 호출하세요.
        /// </summary>
        public void ShowHome()
        {
            HideAllAppScreens();
            SetScreenActive(homeScreen, true);
            CurrentScreen = homeScreen;
        }

        /// <summary>
        /// MAK CHA 콜 앱 화면을 표시합니다.
        /// CallNotificationSystem.OnPhoneOpened()에서 자동 호출됩니다.
        /// </summary>
        public void ShowCallApp()
        {
            SwitchTo(callAppScreen);

            // PhoneCallApp 컴포넌트에도 ShowCallScreen() 알림
            if (callAppScreen != null)
                callAppScreen.GetComponentInChildren<PhoneCallApp>(true)?.ShowCallScreen();
        }

        /// <summary>
        /// 도보 네비 앱 화면을 표시합니다 (선택).
        /// </summary>
        public void ShowNaviApp()
        {
            SwitchTo(naviAppScreen);
        }

        /// <summary>
        /// 임의의 화면 오브젝트를 직접 지정해 전환합니다.
        /// </summary>
        public void SwitchTo(GameObject screen)
        {
            if (screen == null) return;

            HideAllAppScreens();
            SetScreenActive(homeScreen, false);
            SetScreenActive(screen, true);
            CurrentScreen = screen;
        }

        /// <summary>
        /// 폰이 닫힐 때 호출합니다. 다음에 열 때 홈부터 시작하도록 초기화합니다.
        /// PhoneManager.ClosePhone()에서 자동 호출됩니다.
        /// </summary>
        public void OnPhoneClosed()
        {
            HideAllAppScreens();
            SetScreenActive(homeScreen, true);
            CurrentScreen = homeScreen;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부

        private void HideAllAppScreens()
        {
            SetScreenActive(callAppScreen,  false);
            SetScreenActive(naviAppScreen,  false);
        }

        private static void SetScreenActive(GameObject screen, bool active)
        {
            if (screen != null) screen.SetActive(active);
        }
    }
}
