using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NightDriver.Core;

namespace NightDriver.UI
{
    /// <summary>
    /// 화면 상단 중앙에 목적지 방향을 가리키는 HUD 화살표 컴포넌트.
    /// Screen Space Overlay Canvas 하위에 배치하세요.
    /// </summary>
    public sealed class NavigationHUD : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("방향을 나타낼 화살표 이미지 (RectTransform이 회전됩니다)")]
        [SerializeField] private RectTransform arrowImage;

        [Tooltip("남은 거리를 표시할 TMP 텍스트")]
        [SerializeField] private TMP_Text distanceText;

        [Tooltip("플레이어 Transform. 비워두면 Camera.main 위치를 기준으로 사용합니다.")]
        [SerializeField] private Transform playerTransform;

        [Header("Night (Optional)")]
        [Tooltip("비워두면 GameManager.Instance.NightManager를 사용합니다. 일차가 바뀔 때 목적지를 초기화해 HUD를 끕니다.")]
        [SerializeField] private NightManager nightManager;

        [Header("설정")]
        [Tooltip("이 거리(m) 이내에 도달하면 HUD를 숨깁니다. 값이 크면 스폰 직후 손님·차 근처에서도 '도착'으로 처리되어 화살표가 안 보일 수 있습니다.")]
        [SerializeField] private float hideDistance = 3.5f;

        [Tooltip("거리 표시 포맷. {0}에 거리(m 정수)가 들어갑니다.")]
        [SerializeField] private string distanceFormat = "{0}m";

        // ─────────────────────────────────────────────
        // 런타임
        private Transform destination;
        private Camera   mainCamera;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            mainCamera = Camera.main;
            // 시작 시 목적지가 없으면 숨김
            SetVisible(false);

            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
        }

        private void OnEnable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged += HandleDayChanged;
        }

        private void OnDisable()
        {
            if (nightManager != null)
                nightManager.OnDayChanged -= HandleDayChanged;
        }

        private void LateUpdate()
        {
            if (destination == null)
            {
                SetVisible(false);
                return;
            }

            // 플레이어(또는 카메라) 월드 위치
            // VehicleSeatInteraction은 플레이어 루트를 움직이지 않고 카메라만 좌석으로 이동시키므로
            // 거리 계산 원점은 카메라를 우선합니다.
            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 origin = mainCamera != null
                ? mainCamera.transform.position
                : (playerTransform != null ? playerTransform.position : Vector3.zero);

            float distance = Vector3.Distance(origin, destination.position);

            // 도달 판정
            if (distance <= hideDistance)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            // ── 화살표 회전 계산 ────────────────────────────────
            // 목적지 방향을 화면 2D 벡터로 변환
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 dirWorld   = destination.position - origin;
            dirWorld.y = 0f; // 수평 방향만 사용 (경사면 무시)

            // 카메라 전방 기준 각도 계산
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // 카메라 좌표계 기준 x/y 성분으로 화면 방향 벡터 생성
            float dot_forward = Vector3.Dot(dirWorld.normalized, camForward);
            float dot_right   = Vector3.Dot(dirWorld.normalized, camRight);

            // atan2로 화면 회전각 계산 (위쪽 = 전방 = 0도)
            float angle = Mathf.Atan2(dot_right, dot_forward) * Mathf.Rad2Deg;

            if (arrowImage != null)
                arrowImage.localRotation = Quaternion.Euler(0f, 0f, -angle);

            // ── 거리 텍스트 ──────────────────────────────────────
            if (distanceText != null)
                distanceText.text = string.Format(distanceFormat, Mathf.RoundToInt(distance));
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 목적지를 변경합니다. null을 전달하면 HUD를 숨깁니다.
        /// </summary>
        public void SetDestination(Transform target)
        {
            destination = target;
            if (destination == null) SetVisible(false);
        }

        /// <summary>
        /// 현재 목적지를 반환합니다.
        /// </summary>
        public Transform GetDestination() => destination;

        // ─────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (arrowImage != null)
                arrowImage.gameObject.SetActive(visible);
            if (distanceText != null)
                distanceText.gameObject.SetActive(visible);
        }

        private void HandleDayChanged(int _)
        {
            // 다음 날로 넘어갈 때는 이전 날 목적지 HUD를 끕니다.
            SetDestination(null);
        }
    }
}
