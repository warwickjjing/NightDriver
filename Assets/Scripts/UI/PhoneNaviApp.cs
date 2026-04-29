using TMPro;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 도보 픽업 모드 전용 폰 네비 앱 UI.
    /// PhoneUIController가 관리하는 폰 패널 안에 하위 오브젝트로 배치하세요.
    ///
    /// Open(pickupTarget)  — 앱 활성화 + 픽업 위치 정보 주입
    /// Close()             — 앱 비활성화
    ///
    /// [거리 표시 규칙] NavigationHUD와 동일
    ///   10m 이내   → "목적지 부근입니다."
    ///   100m 이내  → "XXm 앞"
    ///   그 외      → "X.Xkm"
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Phone Navi App")]
    public sealed class PhoneNaviApp : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector

        [Header("앱 루트")]
        [Tooltip("이 오브젝트 전체를 켜고 끄는 방식으로 앱을 표시/숨깁니다.")]
        [SerializeField] private GameObject appRoot;

        [Header("텍스트 요소")]
        [Tooltip("픽업 대상의 이름이나 태그를 표시할 텍스트 (예: '박수기 님 픽업')")]
        [SerializeField] private TMP_Text pickupNameText;

        [Tooltip("픽업 위치까지 남은 거리를 실시간으로 표시할 텍스트")]
        [SerializeField] private TMP_Text distanceText;

        [Tooltip("폰 화면의 방향 화살표 이미지 RectTransform (선택). 비워두면 화살표 없이 텍스트만 표시됩니다.")]
        [SerializeField] private RectTransform arrowImage;

        [Header("픽업 레이블 포맷")]
        [Tooltip("{0} = 픽업 오브젝트 이름")]
        [SerializeField] private string pickupLabelFormat = "{0} 픽업";

        [Header("거리 설정")]
        [Tooltip("이 거리(m) 이내이면 '목적지 부근입니다.' 표시")]
        [SerializeField] private float nearThreshold = 10f;

        [Tooltip("이 거리(m) 이내이면 'XXm 앞' 표시")]
        [SerializeField] private float midThreshold = 100f;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private Transform _pickupTarget;
        private bool      _isOpen;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // 시작 시 닫힌 상태
            SetAppActive(false);
        }

        private void Update()
        {
            if (!_isOpen || _pickupTarget == null) return;

            // 카메라 우선 원점
            var cam = Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : transform.position;

            float distance = Vector3.Distance(origin, _pickupTarget.position);

            // 거리 텍스트 업데이트
            UpdateDistanceText(distance);

            // 화살표 방향 업데이트 (arrowImage가 있을 때만)
            if (arrowImage != null && cam != null)
                UpdateArrow(origin, cam);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API

        /// <summary>
        /// 폰 네비 앱을 열고 픽업 위치를 주입합니다.
        /// NavigationManager.SetPickupMode()에서 호출합니다.
        /// </summary>
        public void Open(Transform pickupTarget)
        {
            _pickupTarget = pickupTarget;
            _isOpen = true;

            // 픽업 이름 텍스트 주입
            if (pickupNameText != null)
            {
                string label = pickupTarget != null
                    ? string.Format(pickupLabelFormat, pickupTarget.name)
                    : "픽업 대기 중";
                pickupNameText.text = label;
            }

            SetAppActive(true);
        }

        /// <summary>
        /// 폰 네비 앱을 닫습니다.
        /// NavigationManager.SetDrivingMode() / SetNaviOff()에서 호출합니다.
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            _pickupTarget = null;
            SetAppActive(false);
        }

        /// <summary>현재 앱이 열려있는지 여부</summary>
        public bool IsOpen => _isOpen;

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 화살표

        private void UpdateArrow(Vector3 origin, Camera cam)
        {
            Vector3 dirWorld = _pickupTarget.position - origin;
            dirWorld.y = 0f;

            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            float dotForward = Vector3.Dot(dirWorld.normalized, camForward);
            float dotRight   = Vector3.Dot(dirWorld.normalized, camRight);

            float angle = Mathf.Atan2(dotRight, dotForward) * Mathf.Rad2Deg;
            arrowImage.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 거리 텍스트

        private void UpdateDistanceText(float distance)
        {
            if (distanceText == null) return;

            if (distance <= nearThreshold)
                distanceText.text = "목적지 부근입니다.";
            else if (distance <= midThreshold)
                distanceText.text = $"{Mathf.RoundToInt(distance)}m 앞";
            else
                distanceText.text = $"{distance / 1000f:F1}km";
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 — 활성화

        private void SetAppActive(bool active)
        {
            if (appRoot != null)
                appRoot.SetActive(active);
        }
    }
}
