using NightDriver.Client;
using NightDriver.Character;
using NightDriver.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.UI
{
    /// <summary>
    /// Phone UI를 Tab 키로 올리고/내리며, 콜받기 버튼으로 다음 손님 스폰을 요청합니다.
    /// </summary>
    public sealed class PhoneUIController : MonoBehaviour
    {
        public static bool IsAnyPhoneVisible { get; private set; }

        [Header("UI")]
        [SerializeField] private RectTransform phonePanel;
        [SerializeField] private Button acceptCallButton;

        [Header("Slide Animation")]
        [SerializeField] private float hiddenAnchoredY = -1200f;
        [SerializeField] private float shownAnchoredY = 0f;
        [SerializeField] private float slideDuration = 0.22f;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        [Header("Call Flow")]
        [Tooltip("비우면 씬에서 자동 탐색합니다. 콜 수락은 CallFlowController(ICallFlow) 한 곳으로 모읍니다.")]
        [SerializeField] private CallFlowController callFlow;
        [SerializeField] private bool hidePhoneAfterAccept = true;

        private bool isVisible;
        private float slideT;
        private Vector2 targetAnchoredPos;
        private Vector2 velocity;

        private void Awake()
        {
            if (callFlow == null)
                callFlow = FindFirstObjectByType<CallFlowController>();

            if (acceptCallButton != null)
            {
                acceptCallButton.onClick.RemoveListener(OnClickAcceptCall);
                acceptCallButton.onClick.AddListener(OnClickAcceptCall);
            }

            if (phonePanel != null)
            {
                Vector2 start = phonePanel.anchoredPosition;
                start.y = hiddenAnchoredY;
                phonePanel.anchoredPosition = start;
                targetAnchoredPos = start;
            }
        }

        private void Update()
        {
            // 대화 중에는 폰을 열지 않도록 강제합니다.
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
            {
                if (isVisible) SetVisible(false);
                return;
            }

            // 차량 탑승 중에는 폰(Tab)을 열지 않도록 강제합니다.
            if (PlayerControlLock.VehicleSeated)
            {
                if (isVisible) SetVisible(false);
                return;
            }

            if (Input.GetKeyDown(toggleKey))
                SetVisible(!isVisible);

            if (phonePanel == null) return;

            if (slideDuration <= 0.0001f)
            {
                phonePanel.anchoredPosition = targetAnchoredPos;
                return;
            }

            phonePanel.anchoredPosition = Vector2.SmoothDamp(
                phonePanel.anchoredPosition,
                targetAnchoredPos,
                ref velocity,
                slideDuration);
        }

        public void OnClickAcceptCall()
        {
            if (callFlow == null)
            {
                Debug.LogWarning("[PhoneUI] CallFlowController를 찾을 수 없습니다.", this);
                return;
            }

            if (!callFlow.TryAcceptCall())
                return;

            if (hidePhoneAfterAccept)
                SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            IsAnyPhoneVisible = isVisible;

            // 폰 UI가 열렸을 때는 커서를 보여주고, 닫히면 다시 잠급니다.
            Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isVisible;

            if (phonePanel == null) return;

            Vector2 next = phonePanel.anchoredPosition;
            next.y = isVisible ? shownAnchoredY : hiddenAnchoredY;
            targetAnchoredPos = next;
        }

        private void OnDisable()
        {
            IsAnyPhoneVisible = false;
        }
    }
}
