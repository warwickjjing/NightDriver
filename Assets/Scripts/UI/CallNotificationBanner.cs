using System.Collections;
using TMPro;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 화면 상단에서 슬라이드 다운으로 나타났다가 3초 후 자동으로 사라지는 알림 배너.
    ///
    /// [Inspector 배치]
    ///   Screen Space Overlay Canvas 하위에 배치하세요.
    ///   bannerRoot의 Pivot을 위쪽(Y=1)으로, Anchor를 상단 중앙으로 설정하면
    ///   hiddenAnchoredY를 양수(예: 150), shownAnchoredY를 0으로 맞출 수 있습니다.
    ///   또는 상단 밖에 배치(hiddenAnchoredY = 양수/음수)해도 됩니다.
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Call Notification Banner")]
    public sealed class CallNotificationBanner : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector

        [Header("배너 루트")]
        [Tooltip("슬라이드 애니메이션 대상 RectTransform")]
        [SerializeField] private RectTransform bannerRoot;

        [Header("텍스트")]
        [Tooltip("배너에 표시할 TMP 텍스트")]
        [SerializeField] private TMP_Text messageText;

        [Header("위치 설정")]
        [Tooltip("숨겨진 상태의 anchoredPosition.y (화면 위쪽 바깥)")]
        [SerializeField] private float hiddenAnchoredY = 150f;

        [Tooltip("표시된 상태의 anchoredPosition.y (화면 안쪽)")]
        [SerializeField] private float shownAnchoredY = -10f;

        [Header("애니메이션")]
        [Tooltip("슬라이드 다운 속도 (SmoothDamp smoothTime)")]
        [SerializeField] private float slideSmoothTime = 0.15f;

        [Header("자동 숨김")]
        [Tooltip("배너가 표시된 후 자동으로 사라지는 시간(초)")]
        [SerializeField] private float autoHideSeconds = 3f;

        [Header("색상")]
        [Tooltip("강조색(accent). 배너 배경 Image가 있으면 이 색으로 설정합니다.")]
        [SerializeField] private Color accentColor = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("배너 배경 Image 컴포넌트 (선택). 있으면 accentColor로 설정합니다.")]
        [SerializeField] private UnityEngine.UI.Image backgroundImage;

        // ─────────────────────────────────────────────────────────────────────
        // 런타임

        private Vector2 _velocity;
        private Vector2 _targetPos;
        private bool    _isVisible;
        private Coroutine _autoHideRoutine;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // 배너 배경 색 설정
            if (backgroundImage != null)
                backgroundImage.color = accentColor;

            // 시작 시 숨김 상태
            if (bannerRoot != null)
            {
                Vector2 pos = bannerRoot.anchoredPosition;
                pos.y = hiddenAnchoredY;
                bannerRoot.anchoredPosition = pos;
                _targetPos = pos;
            }
        }

        private void Update()
        {
            if (bannerRoot == null) return;

            // SmoothDamp로 부드럽게 이동
            bannerRoot.anchoredPosition = Vector2.SmoothDamp(
                bannerRoot.anchoredPosition,
                _targetPos,
                ref _velocity,
                slideSmoothTime);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 공개 API

        /// <summary>
        /// 배너를 슬라이드 다운으로 표시하고 <see cref="autoHideSeconds"/>초 후 자동으로 숨깁니다.
        /// </summary>
        /// <param name="message">표시할 메시지 텍스트</param>
        public void Show(string message)
        {
            if (messageText != null)
                messageText.text = message;

            SetVisible(true);

            // 이미 타이머가 돌고 있으면 리셋
            if (_autoHideRoutine != null)
                StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = StartCoroutine(AutoHideRoutine());
        }

        /// <summary>배너를 즉시 숨깁니다 (슬라이드 업).</summary>
        public void Hide()
        {
            if (_autoHideRoutine != null)
            {
                StopCoroutine(_autoHideRoutine);
                _autoHideRoutine = null;
            }
            SetVisible(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부

        private void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (bannerRoot == null) return;

            Vector2 pos = _targetPos;
            pos.y = visible ? shownAnchoredY : hiddenAnchoredY;
            _targetPos = pos;
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(autoHideSeconds);
            SetVisible(false);
            _autoHideRoutine = null;
        }
    }
}
