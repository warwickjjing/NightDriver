using TMPro;
using UnityEngine;

namespace NightDriver.Interaction
{
    /// <summary>
    /// 상호작용 프롬프트 UI를 관리하는 싱글턴 컴포넌트.
    /// Canvas 하위의 TextMeshProUGUI를 표시/숨김 처리합니다.
    /// </summary>
    public sealed class InteractionUI : MonoBehaviour
    {
        public static InteractionUI Instance { get; private set; }

        [Header("UI 참조")]
        [Tooltip("프롬프트 문자열을 표시할 TextMeshProUGUI. 비워두면 자식에서 자동 탐색합니다.")]
        [SerializeField] private TMP_Text promptText;

        [Header("기본 텍스트")]
        [Tooltip("NPC별로 별도 텍스트가 지정되지 않았을 때 사용하는 기본 프롬프트")]
        [SerializeField] private string defaultPrompt = "말걸기 [E]";

        // ─────────────────────────────────────────────

        private void Awake()
        {
            // 싱글턴 중복 방지
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // TMP_Text 자동 탐색
            if (promptText == null)
                promptText = GetComponentInChildren<TMP_Text>(true);

            // 시작 시 숨김 상태로 초기화
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 프롬프트를 표시합니다.
        /// </summary>
        /// <param name="prompt">null 또는 빈 문자열이면 defaultPrompt를 사용합니다.</param>
        public void Activate(string prompt = null)
        {
            if (promptText != null)
                promptText.text = string.IsNullOrWhiteSpace(prompt) ? defaultPrompt : prompt;

            SetVisible(true);
        }

        /// <summary>
        /// 프롬프트를 숨깁니다.
        /// </summary>
        public void Deactivate()
        {
            SetVisible(false);
        }

        // ─────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (promptText != null)
                promptText.gameObject.SetActive(visible);
        }
    }
}
