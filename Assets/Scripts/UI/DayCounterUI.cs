using NightDriver.Core;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace NightDriver.UI
{
    public sealed class DayCounterUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private string format = "{0}일차";

        [Header("Source (optional)")]
        [Tooltip("비워두면 GameManager.Instance.NightManager를 자동 사용합니다.")]
        [SerializeField] private NightManager nightManager;

        [Header("Toast Timing")]
        [Tooltip("Day 변경 시 표시를 유지할 시간(초)")]
        [SerializeField] private float displaySeconds = 3f;

        [Tooltip("플레이 시작 시 바로 표시할지 여부. 꺼두면 day가 바뀐 순간부터 표시합니다.")]
        [SerializeField] private bool showOnStart = false;

        [Tooltip("숨길 때 레이캐스트도 같이 차단합니다.")]
        [SerializeField] private bool blockRaycastsWhenHidden = true;

        [Tooltip("Time.timeScale 영향을 받지 않게 할지 선택합니다.")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onDayChanged;

        private Graphic[] uiGraphics;
        private bool[] uiRaycastTargets;
        private bool dayTextRaycastTarget;
        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (dayText == null) dayText = GetComponentInChildren<TMP_Text>(true);
            if (nightManager == null) nightManager = GameManager.Instance != null ? GameManager.Instance.NightManager : null;

            dayTextRaycastTarget = dayText != null && dayText.raycastTarget;

            uiGraphics = GetComponentsInChildren<Graphic>(true);
            uiRaycastTargets = new bool[uiGraphics.Length];
            for (int i = 0; i < uiGraphics.Length; i++)
            {
                uiRaycastTargets[i] = uiGraphics[i] != null && uiGraphics[i].raycastTarget;
            }
        }

        private void OnEnable()
        {
            if (nightManager != null) nightManager.OnDayChanged += HandleDayChanged;
            if (showOnStart) Refresh();
            else SetVisible(false);
        }

        private void OnDisable()
        {
            if (nightManager != null) nightManager.OnDayChanged -= HandleDayChanged;
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        public void Refresh()
        {
            if (nightManager == null) return;
            HandleDayChanged(nightManager.State.dayIndex);
        }

        public void AdvanceToNextNight()
        {
            nightManager?.AdvanceToNextNight();
        }

        public void BeginNight(int dayIndex)
        {
            nightManager?.BeginNight(dayIndex);
        }

        private void HandleDayChanged(int dayIndex)
        {
            if (dayText != null)
            {
                dayText.text = string.Format(format, dayIndex);
            }

            ShowToast();
            onDayChanged?.Invoke(dayIndex);
        }

        private void ShowToast()
        {
            SetVisible(true);

            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            hideCoroutine = StartCoroutine(HideAfterSeconds(displaySeconds));
        }

        private IEnumerator HideAfterSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                SetVisible(false);
                yield break;
            }

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);

            SetVisible(false);
            hideCoroutine = null;
        }

        private void SetVisible(bool visible)
        {
            float a = visible ? 1f : 0f;

            // TMP: dayText를 알파로 제어
            if (dayText != null)
            {
                dayText.alpha = a;
                if (blockRaycastsWhenHidden)
                    dayText.raycastTarget = visible ? dayTextRaycastTarget : false;
            }

            // Image 등 UGUI Graphic들 알파 제어
            for (int i = 0; i < uiGraphics.Length; i++)
            {
                var g = uiGraphics[i];
                if (g == null) continue;

                var c = g.color;
                c.a = a;
                g.color = c;

                if (blockRaycastsWhenHidden)
                    g.raycastTarget = visible ? uiRaycastTargets[i] : false;
            }
        }
    }
}

