using NightDriver.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.UI
{
    public sealed class DaySelectorUI : MonoBehaviour
    {
        [Header("Source (optional)")]
        [Tooltip("비워두면 GameManager.Instance.NightManager를 자동 사용합니다.")]
        [SerializeField] private NightManager nightManager;

        [Header("UI")]
        [Tooltip("버튼이 생성될 부모(예: 왼쪽 패널의 VerticalLayoutGroup)")]
        [SerializeField] private RectTransform buttonContainer;

        [Tooltip("Button 프리팹(자식에 TMP_Text가 있어야 라벨 셋팅 가능)")]
        [SerializeField] private Button dayButtonPrefab;

        [Header("Days")]
        [SerializeField] private int minDay = 1;
        [SerializeField] private int maxDay = 7;
        [SerializeField] private string labelFormat = "{0}일차";

        private Button[] spawned;

        private void Awake()
        {
            if (buttonContainer == null)
            {
                buttonContainer = GetComponent<RectTransform>();
                if (buttonContainer == null) buttonContainer = transform.parent as RectTransform;
            }
            if (nightManager == null) nightManager = GameManager.Instance != null ? GameManager.Instance.NightManager : null;
        }

        private void OnEnable()
        {
            if (nightManager != null) nightManager.OnDayChanged += HandleDayChanged;
            Rebuild();
            RefreshHighlight();
        }

        private void OnDisable()
        {
            if (nightManager != null) nightManager.OnDayChanged -= HandleDayChanged;
        }

        public void Rebuild()
        {
            if (buttonContainer == null || dayButtonPrefab == null) return;

            // Clear children
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonContainer.GetChild(i).gameObject);
            }

            int count = Mathf.Max(0, maxDay - minDay + 1);
            spawned = count > 0 ? new Button[count] : null;

            for (int day = minDay; day <= maxDay; day++)
            {
                int capturedDay = day;
                var btn = Instantiate(dayButtonPrefab, buttonContainer);
                btn.name = $"DayButton_{capturedDay}";
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    nightManager?.BeginNight(capturedDay);
                });

                var label = btn.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = string.Format(labelFormat, capturedDay);

                spawned[capturedDay - minDay] = btn;
            }
        }

        private void HandleDayChanged(int _)
        {
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            if (nightManager == null || spawned == null) return;

            int current = nightManager.State.dayIndex;
            for (int i = 0; i < spawned.Length; i++)
            {
                var btn = spawned[i];
                if (btn == null) continue;
                int day = minDay + i;
                btn.interactable = day != current;
            }
        }
    }
}
