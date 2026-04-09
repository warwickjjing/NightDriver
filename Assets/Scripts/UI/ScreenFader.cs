using System.Collections;
using UnityEngine;

namespace NightDriver.UI
{
    /// <summary>
    /// 화면 페이드(검정 패널) 유틸리티.
    /// - CanvasGroup alpha 0→1→0 제어
    /// - URP/렌더 파이프라인과 무관하게 UI로 처리
    /// </summary>
    [AddComponentMenu("NightDriver/UI/Screen Fader")]
    public sealed class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private Coroutine routine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// 페이드 아웃(검정) → 콜백 → 페이드 인(복귀)
        /// </summary>
        public void FadeOutIn(float durationSeconds, System.Action midAction)
        {
            if (canvasGroup == null) return;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeOutInRoutine(durationSeconds, midAction));
        }

        private IEnumerator FadeOutInRoutine(float durationSeconds, System.Action midAction)
        {
            float half = Mathf.Max(0.0001f, durationSeconds);

            yield return FadeTo(1f, half);
            midAction?.Invoke();
            yield return FadeTo(0f, half);

            routine = null;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (canvasGroup == null) yield break;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                canvasGroup.alpha = a;
                yield return null;
            }

            canvasGroup.alpha = target;

            bool isHidden = target <= 0.001f;
            canvasGroup.blocksRaycasts = !isHidden;
            canvasGroup.interactable = !isHidden;
        }
    }
}

