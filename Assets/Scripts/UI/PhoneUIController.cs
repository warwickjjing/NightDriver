using NightDriver.Client;
using NightDriver.Core;
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
        [SerializeField] private NightManager nightManager;
        [SerializeField] private ClientSpawner clientSpawner;
        [SerializeField] private bool hidePhoneAfterAccept = true;

        private bool isVisible;
        private float slideT;
        private Vector2 targetAnchoredPos;
        private Vector2 velocity;

        private void Awake()
        {
            if (nightManager == null && GameManager.Instance != null)
                nightManager = GameManager.Instance.NightManager;
            if (clientSpawner == null)
                clientSpawner = FindFirstObjectByType<ClientSpawner>();

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
            if (nightManager == null || clientSpawner == null) return;

            if (ClientRegistry.CurrentClientObject != null)
            {
                Debug.Log("[PhoneUI] 이미 활성 손님이 있어 콜을 받을 수 없습니다.", this);
                return;
            }

            int completed = nightManager.State.callsCompleted;
            int limit = Mathf.Max(1, nightManager.State.callsPerNight);
            if (completed >= limit)
            {
                Debug.Log("[PhoneUI] 오늘 콜을 모두 완료했습니다.", this);
                return;
            }

            // callsCompleted 기준으로 다음 손님을 스폰합니다.
            // ClientSpawner가 동시에 HUD 목적지도 연결합니다.
            clientSpawner.SpawnCurrentClient();

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
