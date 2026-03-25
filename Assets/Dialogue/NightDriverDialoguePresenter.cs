using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn;
using Yarn.Unity;

public class NewDialoguePresenter : DialoguePresenterBase
{
    [Header("UI 참조")]
    [SerializeField] private GameObject dialoguePanel;   // DialoguePanel
    [SerializeField] private TMP_Text speakerText;       // SpeakerText
    [SerializeField] private TMP_Text lineText;          // LineText
    [SerializeField] private Button continueButton;      // ContinueButton
    [SerializeField] private GameObject optionsPanel;    // OptionsPanel
    [SerializeField] private Button optionButtonPrefab;  // 선택지 버튼 프리팹
    [Header("입력")]
    [SerializeField] private KeyCode continueKey = KeyCode.E;
    [SerializeField] private KeyCode alternateContinueKey = KeyCode.Space;
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private float inputBufferSeconds = 0.12f;

    [Header("옵션 하이라이트")]
    [SerializeField] private Color optionNormalImageColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color optionSelectedImageColor = new Color32(255, 220, 60, 255);

    // 플레이어가 "다음"을 눌렀는지 추적
    private bool continuePressed = false;
    // 플레이어가 선택한 옵션
    private DialogueOption? selectedOption = null;

    // ──────────────────────────────────────────
    // 대화 시작
    public override async YarnTask OnDialogueStartedAsync()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        continuePressed = false;
        selectedOption = null;
    }

    // 대화 종료
    public override async YarnTask OnDialogueCompleteAsync()
    {
        Debug.Log("[NewDialoguePresenter] OnDialogueCompleteAsync");
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // 한 줄 대사 표시
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        var lineId = line.TextID;
        var character = line.CharacterName ?? "(null)";
        var text = line.TextWithoutCharacterName.Text;

        // 화자 이름 / 본문 표시
        speakerText.text = line.CharacterName ?? "";
        lineText.text     = line.TextWithoutCharacterName.Text;

        // 계속 버튼 표시
        continuePressed = false;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continuePressed = true);
        }

        // 대화 시작 키(E)가 같은 프레임에 중복 소비되어 첫 줄이 스킵되는 현상을 방지
        float unlockTime = Time.unscaledTime + inputBufferSeconds;

        // ContinueButton이 없어도 키/클릭으로 다음 줄로 진행할 수 있게 처리
        while (!continuePressed)
        {
            if (Time.unscaledTime >= unlockTime
                && (Input.GetKeyDown(continueKey)
                    || Input.GetKeyDown(alternateContinueKey)
                    || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetMouseButtonDown(0)))
            {
                continuePressed = true;
            }
            await YarnTask.Yield();
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    // 선택지 표시
    public override async YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        selectedOption = null;

        if (dialogueOptions == null || dialogueOptions.Length == 0)
        {
            return null;
        }

        if (optionsPanel != null) optionsPanel.SetActive(true);

        // 선택지 버튼 생성
        var createdButtons = new System.Collections.Generic.List<Button>(dialogueOptions.Length);
        var createdImages = new System.Collections.Generic.List<Image>(dialogueOptions.Length);
        foreach (var option in dialogueOptions)
        {
            var btn = Instantiate(optionButtonPrefab, optionsPanel.transform);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = option.Line.TextWithoutCharacterName.Text;

            var captured = option;
            btn.onClick.AddListener(() => selectedOption = captured);

            createdButtons.Add(btn);
            var image = btn.GetComponentInChildren<Image>(true);
            createdImages.Add(image);
        }

        // 옵션 진입 직후 입력 중복 소비를 방지
        float unlockTime = Time.unscaledTime + inputBufferSeconds;

        int selectedIndex = 0;
        void ApplyHighlight()
        {
            for (int i = 0; i < createdImages.Count; i++)
            {
                var img = createdImages[i];
                if (img == null) continue;
                img.color = (i == selectedIndex) ? optionSelectedImageColor : optionNormalImageColor;
            }
        }

        ApplyHighlight();

        // 플레이어가 선택할 때까지 대기 (위/아래로 이동, Enter로 확정, E는 반응 안 함)
        while (selectedOption == null && !cancellationToken.IsNextContentRequested)
        {
            if (Time.unscaledTime >= unlockTime)
            {
                bool changed = false;

                if (Input.GetKeyDown(upKey))
                {
                    selectedIndex = (selectedIndex - 1 + dialogueOptions.Length) % dialogueOptions.Length;
                    changed = true;
                }
                else if (Input.GetKeyDown(downKey))
                {
                    selectedIndex = (selectedIndex + 1) % dialogueOptions.Length;
                    changed = true;
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    selectedOption = dialogueOptions[selectedIndex];
                    break;
                }

                if (changed) ApplyHighlight();
            }

            await YarnTask.Yield();
        }

        // 버튼 클릭으로 selectedOption이 세팅된 경우를 함께 지원
        if (selectedOption == null)
            selectedOption = dialogueOptions[selectedIndex];

        // 버튼 정리
        foreach (Transform child in optionsPanel.transform)
            Destroy(child.gameObject);

        if (optionsPanel != null) optionsPanel.SetActive(false);

        return selectedOption;
    }
}