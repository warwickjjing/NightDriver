using System.Collections;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 심야 대리기사 네비게이션 전체 상태를 관리하는 오케스트레이터.
    ///
    /// [모드]
    ///   PhoneNaviMode  — 도보 모드. 픽업 이동 중. 폰 네비 앱 활성, HUD 비활성.
    ///   DrivingNaviMode — 드라이빙 모드. 손님 탑승 후. 폰 네비 비활성, HUD 활성.
    ///   Off             — 모두 비활성. 도착 또는 하차 시.
    ///
    /// [사용법]
    ///   NavigationManager.Instance.SetPickupMode(pickupTransform);
    ///   NavigationManager.Instance.SetDrivingMode(destinationTransform);
    ///   NavigationManager.Instance.SetNaviOff();
    ///   NavigationManager.Instance.TriggerHUDGlitch();
    ///   NavigationManager.Instance.TriggerWrongDestination(fakeTransform);
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Navigation Manager")]
    public sealed class NavigationManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 싱글톤
        public static NavigationManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        // Inspector 참조

        [Header("HUD (드라이빙 모드)")]
        [Tooltip("드라이빙 모드에서 사용할 HUD 컴포넌트")]
        [SerializeField] private NavigationHUD hud;

        [Header("폰 네비 앱 (도보 모드)")]
        [Tooltip("픽업 이동 중 폰에 표시할 네비 앱 컴포넌트")]
        [SerializeField] private PhoneNaviApp phoneNavi;

        [Header("공포 연출 — HUD Glitch")]
        [Tooltip("글리치 중 화살표가 틀어지는 최대 각도 (도)")]
        [SerializeField] private float glitchMaxAngle = 170f;
        [Tooltip("깜빡임 간격 (초)")]
        [SerializeField] private float glitchBlinkInterval = 0.12f;
        [Tooltip("글리치 지속 시간 (초)")]
        [SerializeField] private float glitchDuration = 2f;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임 상태

        /// <summary>현재 네비게이션 모드</summary>
        public NaviMode CurrentMode { get; private set; } = NaviMode.Off;

        private Transform _realDestination;
        private Coroutine _glitchRoutine;
        private Coroutine _wrongDestRoutine;

        // ─────────────────────────────────────────────────────────────────────

        public enum NaviMode
        {
            Off,
            PhoneNavi,   // 도보 픽업
            Driving      // 드라이빙 HUD
        }

        // ─────────────────────────────────────────────────────────────────────
        // 생명주기

        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 자동 탐색 (비워뒀을 때)
            if (hud == null)
                hud = FindFirstObjectByType<NavigationHUD>(FindObjectsInactive.Include);
            if (phoneNavi == null)
                phoneNavi = FindFirstObjectByType<PhoneNaviApp>(FindObjectsInactive.Include);

            // 시작 시 모두 비활성
            ApplyOff();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 모드 전환

        /// <summary>
        /// [상황 1] 도보 모드 시작.
        /// 폰 네비 앱을 열고 픽업 위치 텍스트를 주입합니다.
        /// HUD는 비활성 상태를 유지합니다.
        /// </summary>
        /// <param name="pickupTarget">손님 픽업 위치 Transform</param>
        public void SetPickupMode(Transform pickupTarget)
        {
            StopHorrorRoutines();

            CurrentMode = NaviMode.PhoneNavi;
            _realDestination = pickupTarget;

            // HUD 끄기
            hud?.SetDestination(null);

            // 폰 네비 앱 열기 + 목적지 주입
            phoneNavi?.Open(pickupTarget);
        }

        /// <summary>
        /// [상황 2] 드라이빙 모드 시작.
        /// 폰 네비 앱을 닫고 HUD 화살표를 활성화합니다.
        /// </summary>
        /// <param name="destination">손님 목적지 Transform</param>
        public void SetDrivingMode(Transform destination)
        {
            StopHorrorRoutines();

            CurrentMode = NaviMode.Driving;
            _realDestination = destination;

            // 폰 네비 앱 닫기
            phoneNavi?.Close();

            // HUD 활성화
            hud?.SetDestination(destination);
        }

        /// <summary>
        /// 네비게이션 전체 비활성화.
        /// 목적지 도착 또는 손님 하차 시 호출합니다.
        /// </summary>
        public void SetNaviOff()
        {
            StopHorrorRoutines();
            CurrentMode = NaviMode.Off;
            _realDestination = null;
            ApplyOff();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API — 공포 연출

        /// <summary>
        /// HUD 화살표를 2초간 깜빡이고 방향을 랜덤하게 틀었다가 원래 방향으로 복귀합니다.
        /// 드라이빙 모드가 아닐 때는 무시됩니다.
        /// </summary>
        public void TriggerHUDGlitch()
        {
            if (CurrentMode != NaviMode.Driving || hud == null)
                return;

            if (_glitchRoutine != null)
                StopCoroutine(_glitchRoutine);

            _glitchRoutine = StartCoroutine(HUDGlitchRoutine());
        }

        /// <summary>
        /// HUD가 가짜 목적지를 가리키도록 합니다.
        /// 해당 위치 근처 도착 시 "목적지 부근입니다. 안내를 종료합니다."를 표시하고 HUD를 끕니다.
        /// </summary>
        /// <param name="fakeTarget">가짜 목적지 Transform (빈 공터 등)</param>
        public void TriggerWrongDestination(Transform fakeTarget)
        {
            if (hud == null || fakeTarget == null)
                return;

            if (_wrongDestRoutine != null)
                StopCoroutine(_wrongDestRoutine);

            _wrongDestRoutine = StartCoroutine(WrongDestinationRoutine(fakeTarget));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부

        private void ApplyOff()
        {
            hud?.SetDestination(null);
            phoneNavi?.Close();
        }

        private void StopHorrorRoutines()
        {
            if (_glitchRoutine != null)
            {
                StopCoroutine(_glitchRoutine);
                _glitchRoutine = null;
                hud?.ResetGlitch();
            }
            if (_wrongDestRoutine != null)
            {
                StopCoroutine(_wrongDestRoutine);
                _wrongDestRoutine = null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 코루틴 — HUD 글리치

        private IEnumerator HUDGlitchRoutine()
        {
            float elapsed = 0f;

            while (elapsed < glitchDuration)
            {
                // 화살표 깜빡임 ON (랜덤 각도 틀어짐)
                float fakeAngle = Random.Range(-glitchMaxAngle, glitchMaxAngle);
                hud?.SetGlitchOverride(true, fakeAngle);

                yield return new WaitForSeconds(glitchBlinkInterval * 0.5f);

                // 화살표 깜빡임 OFF
                hud?.SetGlitchOverride(false, 0f);

                yield return new WaitForSeconds(glitchBlinkInterval * 0.5f);

                elapsed += glitchBlinkInterval;
            }

            // 원래 목적지 방향으로 복귀
            hud?.ResetGlitch();
            _glitchRoutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 코루틴 — 가짜 목적지

        private IEnumerator WrongDestinationRoutine(Transform fakeTarget)
        {
            // HUD를 가짜 목적지로 전환
            hud?.SetDestination(fakeTarget);

            // 가짜 목적지 근처(10m) 도착 대기
            while (true)
            {
                if (fakeTarget == null) break;

                var cam = Camera.main;
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, fakeTarget.position);
                    if (dist <= 10f)
                        break;
                }
                yield return null;
            }

            // "목적지 부근입니다. 안내를 종료합니다." 표시 후 HUD 종료
            hud?.ShowArrivalMessage("목적지 부근입니다. 안내를 종료합니다.");
            yield return new WaitForSeconds(3f);

            // HUD를 끄되, 진짜 목적지는 보존 (드라이빙 중이라면 계속 달려야 하므로)
            hud?.SetDestination(null);
            _wrongDestRoutine = null;
        }
    }
}
