# Dialogue & Interaction 세팅 가이드

> **대상 버전**: Unity 6 · Yarn Spinner 3.x · TextMeshPro  
> 처음부터 대화가 화면에 뜨고 E키로 상호작용되는 상태까지 순서대로 따라합니다.

---

## 목차

1. [Yarn Spinner 패키지 확인](#1-yarn-spinner-패키지-확인)
2. [Yarn Project 통합(하나로)](#2-yarn-project-통합하나로)
3. [DialogueRunner 오브젝트 만들기](#3-dialoguerunner-오브젝트-만들기)
4. [대사를 화면에 보여주는 Presenter 만들기](#4-대사를-화면에-보여주는-presenter-만들기)
5. [InteractionUI 세팅 (프롬프트 텍스트)](#5-interactionui-세팅-프롬프트-텍스트)
6. [InteractionPrompt 세팅 (NPC에 부착)](#6-interactionprompt-세팅-npc에-부착)
7. [플레이어 태그 설정](#7-플레이어-태그-설정)
8. [씬 Hierarchy 최종 구조](#8-씬-hierarchy-최종-구조)
9. [테스트 체크리스트](#9-테스트-체크리스트)
10. [자주 하는 실수 빠른 진단](#10-자주-하는-실수-빠른-진단)

---

## 1. Yarn Spinner 패키지 확인

`Packages/manifest.json`에 아래가 있으면 OK.

```json
"dev.yarnspinner.unity": "..."
```

없으면 **Package Manager → Add package by name** → `dev.yarnspinner.unity` 입력.

> 이 프로젝트는 **Yarn Spinner 3.x**를 사용합니다.  
> `NewDialoguePresenter.cs`가 `DialoguePresenterBase` / `YarnTask`를 쓰는 걸로 확인됨.  
> (2.x의 `DialogueViewBase`와 다름 — 혼용 주의)

---

## 2. Yarn Project 통합(하나로)

현재 `D1_A_script.yarnproject` / `D1_B_script.yarnproject` 두 개가 있습니다.  
**DialogueRunner 하나에 프로젝트 하나**가 기본 원칙이므로 아래처럼 합칩니다.

### 2-1. 통합 Yarn Project 생성

```
Project 창 → Assets/Dialogue/ 폴더 우클릭
→ Create → Yarn Spinner → Yarn Project
→ 이름: NightDriver_Dialogue  (이름은 자유)
```

### 2-2. .yarn 파일 추가

생성된 `NightDriver_Dialogue.yarnproject`를 선택하면 Inspector에  
**"Source Scripts"** (또는 "Yarn Scripts") 항목이 보입니다.

```
+ 버튼 → D1_A.yarn 추가
+ 버튼 → D1_B.yarn 추가
```

> 또는 Source Scripts를 비워두고 **"Find Scripts in Project"** 버튼을 누르면  
> 프로젝트 내 모든 .yarn 파일이 자동으로 포함됩니다.

### 2-3. 기존 개별 .yarnproject 파일 삭제(선택)

`D1_A_script.yarnproject` / `D1_B_script.yarnproject`는 더 이상 필요 없으므로  
삭제해도 됩니다. (단, 씬에서 참조 중이면 교체 후 삭제)

---

## 3. DialogueRunner 오브젝트 만들기

### 3-1. Hierarchy에 오브젝트 생성

```
Hierarchy 빈 곳 우클릭 → Create Empty → 이름: @Dialogue
```

`@Dialogue` 오브젝트를 선택 후 Inspector에서:

| 추가할 컴포넌트 | 방법 |
|---|---|
| `DialogueRunner` | Add Component → Yarn Spinner → Dialogue Runner |
| `InMemoryVariableStorage` | Add Component → Yarn Spinner → In Memory Variable Storage |

### 3-2. DialogueRunner Inspector 설정

| 필드 | 값 |
|---|---|
| **Yarn Project** | `NightDriver_Dialogue` (2번에서 만든 것) |
| **Variable Storage** | 같은 오브젝트의 `InMemoryVariableStorage` 드래그 |
| **Dialogue Presenters** | ← 4번에서 채움 |

---

## 4. 대사를 화면에 보여주는 Presenter 만들기

> **가장 중요한 단계**입니다.  
> Yarn Project와 DialogueRunner가 있어도 **Presenter가 없으면 화면에 아무것도 뜨지 않습니다.**

Yarn Spinner 3.x에서는 대사를 표시할 **Presenter**를 직접 구성합니다.

### 4-1. Presenter용 Canvas 오브젝트 구조 만들기

```
Hierarchy → UI 쪽 Canvas 안에 아래 구조 생성:

Canvas
└ DialoguePanel                  ← 대사 전체를 감싸는 Panel (Image 컴포넌트)
  ├ SpeakerText                  ← 화자 이름  (TextMeshPro - Text (UI))
  ├ LineText                     ← 대사 본문  (TextMeshPro - Text (UI))
  ├ ContinueButton               ← "다음" 버튼 (Button + TextMeshPro)
  └ OptionsPanel                 ← 선택지 목록 (비어있는 컨테이너, VerticalLayoutGroup)
    └ (선택지 버튼은 런타임에 생성됨)
```

> `DialoguePanel`은 처음에 **비활성화** 상태로 두세요 (대화 시작 시 Presenter가 켭니다).

### 4-2. `NewDialoguePresenter.cs` 활용

이미 `Assets/Dialogue/NewDialoguePresenter.cs`가 있습니다.  
이 스크립트를 **DialoguePanel 오브젝트에 부착**하고, 아래처럼 필드를 채워넣도록 수정합니다.

#### 최소 동작하는 Presenter 예시

아래는 `NewDialoguePresenter.cs`에 넣을 **실제 구현**입니다.  
기존 파일을 열고 내용을 교체하세요.

```csharp
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
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // 한 줄 대사 표시
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // 화자 이름 / 본문 표시
        if (speakerText != null) speakerText.text = line.CharacterName ?? "";
        if (lineText != null)    lineText.text     = line.TextWithoutCharacterName.Text;

        // 계속 버튼 표시
        continuePressed = false;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continuePressed = true);
        }

        // 플레이어가 버튼을 누를 때까지 대기
        while (!continuePressed && !token.IsNextContentRequested)
            await YarnTask.Yield();

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    // 선택지 표시
    public override async YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        selectedOption = null;

        if (optionsPanel != null) optionsPanel.SetActive(true);

        // 선택지 버튼 생성
        foreach (var option in dialogueOptions)
        {
            var btn = Instantiate(optionButtonPrefab, optionsPanel.transform);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = option.Line.TextWithoutCharacterName.Text;

            var captured = option;
            btn.onClick.AddListener(() => selectedOption = captured);
        }

        // 플레이어가 선택할 때까지 대기
        while (selectedOption == null && !cancellationToken.IsNextContentRequested)
            await YarnTask.Yield();

        // 버튼 정리
        foreach (Transform child in optionsPanel.transform)
            Destroy(child.gameObject);

        if (optionsPanel != null) optionsPanel.SetActive(false);

        return selectedOption;
    }
}
```

### 4-3. 선택지 버튼 프리팹 만들기

```
Hierarchy → 빈 곳 우클릭 → UI → Button - TextMeshPro
→ 이름: OptionButtonPrefab
→ Project 창으로 드래그해서 프리팹 저장
→ Hierarchy에서 삭제 (프리팹으로만 사용)
```

### 4-4. DialoguePresenter를 DialogueRunner에 연결

1. `DialoguePanel` 오브젝트에 `NewDialoguePresenter` 부착
2. Inspector에서 각 필드 할당:
   - `Dialogue Panel` → `DialoguePanel` 오브젝트
   - `Speaker Text` → `SpeakerText` TMP
   - `Line Text` → `LineText` TMP
   - `Continue Button` → `ContinueButton`
   - `Options Panel` → `OptionsPanel`
   - `Option Button Prefab` → 위에서 만든 프리팹
3. `@Dialogue`의 `DialogueRunner` Inspector에서
   - **Dialogue Presenters → +** → `DialoguePanel` 오브젝트 드래그

---

## 5. 프롬프트 세팅 — NPC 머리 위 World Space 텍스트

> `InteractionUI.cs`(Screen Space) 방식이 아닌, **NPC 머리 위에 직접 뜨는 3D 텍스트** 방식입니다.

### 5-1. NPC 프리팹 안에 프롬프트 오브젝트 추가

```
Client_D1_A (NPC 루트)
├ PromptAnchor               ← Empty GameObject, 머리 위 위치에 배치 (예: Y = 2.2)
│  └ PromptText              ← TextMeshPro - Text (3D) 컴포넌트 부착
│                               ※ "TextMeshPro - Text (UI)" 가 아닌
│                                  "TextMeshPro - Text (3D)" 를 사용
└ ...
```

### 5-2. PromptText 오브젝트 설정

| 항목 | 값 |
|---|---|
| **컴포넌트** | `TextMeshPro` (3D) — UI가 아님 |
| **Text** | `말걸기 [E]` |
| **Font Size** | 1~3 (World Space는 값이 작음) |
| **Alignment** | Center / Middle |
| **Active** | **꺼두기** (InteractionPrompt가 켜고 끔) |

### 5-3. InteractionPrompt에 연결

NPC 루트의 `InteractionPrompt.cs` Inspector에서:

| 필드 | 값 |
|---|---|
| **World Prompt Text** | `PromptText` 오브젝트 드래그 |
| **Prompt Text** | `말걸기 [E]` (또는 원하는 문구) |
| **Face Camera** | 체크 ✓ (빌보드 — 항상 카메라를 향함) |

> `World Prompt Text`를 비워두면 자식에서 `TMP_Text`를 자동으로 탐색합니다.

---

## 6. InteractionPrompt 세팅 (NPC에 부착)

> 스크립트 위치: `Assets/Scripts/Interaction/InteractionPrompt.cs`

### 6-1. NPC 프리팹 최종 구조

```
Client_D1_A (루트)
├ InteractionPrompt.cs   ← 루트에 부착
├ PromptAnchor           ← Empty, 머리 위 위치 (Y ≈ 2.2)
│  └ PromptText          ← TextMeshPro (3D), 기본 비활성화
└ [모델 메시 자식들...]
```

> **Collider 없이도 동작합니다.** 거리 판정은 Transform 위치로 계산합니다.

### 6-2. InteractionPrompt 컴포넌트 설정

| 필드 | 값 | 설명 |
|---|---|---|
| **Yarn Node Name** | `D1_A` | 이 NPC 대화 시작 노드 |
| **Dialogue Runner** | `@Dialogue`의 DialogueRunner 드래그 | 비워두면 씬에서 자동 탐색 |
| **Interaction Distance** | `3` | 미터 단위, 자유 조절 |
| **Player Tag** | `Player` | 아래 7번에서 설정 |
| **World Prompt Text** | `PromptText` 드래그 | 비워두면 자식에서 자동 탐색 |
| **Prompt Text** | `말걸기 [E]` | 표시할 문구 |
| **Face Camera** | ✓ | 빌보드 효과 (카메라를 향해 회전) |

### 6-3. NPC별 Yarn Node Name

| NPC | Yarn Node Name |
|---|---|
| D1_A 손님 | `D1_A` |
| D1_B 손님 | `D1_B` |

---

## 7. 플레이어 태그 설정

1. Hierarchy에서 **플레이어(Driver_Dohyun) 오브젝트** 선택
2. Inspector 상단 **Tag 드롭다운** 클릭
3. `Player` 선택 (없으면 "Add Tag" → + → `Player` 추가)

---

## 8. 씬 Hierarchy 최종 구조

```
씬
├ @Dialogue
│  ├ DialogueRunner          (Yarn Project: NightDriver_Dialogue 할당)
│  └ InMemoryVariableStorage
│
├ UI
│  └ Canvas
│     ├ DialoguePanel                ← NewDialoguePresenter.cs 부착
│     │  ├ SpeakerText (TMP)
│     │  ├ LineText (TMP)
│     │  ├ ContinueButton
│     │  └ OptionsPanel
│     │
│     ├ InteractionPromptUI          ← InteractionUI.cs 부착
│     │  └ PromptText (TMP)
│     │
│     ├ DayCountUI (기존)
│     └ PanelLeft / DayButtons (기존)
│
├ World
│  ├ Driver_Dohyun (Tag: Player)
│  └ (스폰된) Client_D1_A
│       ├ InteractionPrompt.cs      ← Yarn Node Name: D1_A
│       ├ PromptAnchor (Y≈2.2)
│       │  └ PromptText (TMP 3D)    ← 기본 비활성화, FaceCamera ON
│
└ ...
```

---

## 9. 테스트 체크리스트

### 대화가 뜨기 전 확인

- [ ] `@Dialogue` 오브젝트가 씬에 존재하는가
- [ ] `DialogueRunner`에 `Yarn Project`가 할당됐는가
- [ ] `Yarn Project`에 `D1_A.yarn`, `D1_B.yarn`이 포함됐는가  
  (Inspector에서 Source Scripts 확인)
- [ ] `DialogueRunner`의 **Dialogue Presenters**가 비어있지 않은가  
  (`DialoguePanel` 또는 `NewDialoguePresenter`가 연결됐는가)
- [ ] `DialoguePanel`의 `NewDialoguePresenter`에 모든 TMP/버튼 필드가 할당됐는가

### 프롬프트 관련 확인

- [ ] 플레이어 오브젝트에 **`Player` 태그**가 설정됐는가
- [ ] NPC에 `InteractionPrompt.cs`가 부착됐는가
- [ ] `InteractionPrompt.Yarn Node Name`이 올바른가 (`D1_A`, `D1_B` 등)
- [ ] NPC 자식에 `PromptText (TextMeshPro 3D)` 오브젝트가 있고 **기본 비활성화** 상태인가
- [ ] `InteractionPrompt.worldPromptText`에 해당 TMP가 연결됐는가 (또는 자동 탐색)

### 동작 확인 순서

1. **Play** 시작
2. 플레이어를 NPC 쪽으로 이동 (WASD)
3. 3m 이내로 접근 → "말걸기 [E]" 텍스트가 화면에 뜨는지 확인
4. **E 키** 입력 → `DialoguePanel`이 나타나고 대사가 표시되는지 확인
5. ContinueButton 클릭 → 다음 줄로 넘어가는지 확인
6. 선택지 노드 도달 시 OptionsPanel에 버튼이 생성되는지 확인

---

## 10. 자주 하는 실수 빠른 진단

| 증상 | 원인 | 해결 |
|---|---|---|
| 프롬프트가 아예 안 뜸 | 플레이어 Tag가 `Player`가 아님 | Inspector → Tag → Player |
| 프롬프트가 안 뜸 | NPC와 플레이어 거리가 `Interaction Distance` 초과 | Interaction Distance를 10으로 임시 확대 후 테스트 |
| 프롬프트가 안 뜸 | `PromptText` 오브젝트가 처음부터 비활성화 안 됨 | PromptText GameObject를 Inspector에서 꺼두기 |
| 프롬프트 텍스트가 뒤집힘 | 빌보드 방향 문제 | Face Camera 옵션 확인 |
| 프롬프트는 뜨는데 E키 무반응 | `DialogueRunner` 참조가 null | `@Dialogue`의 DialogueRunner를 Inspector에서 직접 할당 |
| E키 눌러도 대화 패널 안 뜸 | Dialogue Presenters 리스트가 비어있음 | DialogueRunner → Dialogue Presenters → + → DialoguePanel 추가 |
| 대사 텍스트가 안 보임 | `NewDialoguePresenter`의 TMP 필드가 미할당 | Inspector에서 SpeakerText/LineText 할당 |
| 선택지 버튼이 안 생김 | `Option Button Prefab`이 미할당 | 프리팹 만들어서 Inspector 슬롯에 할당 |
| Yarn 노드를 찾을 수 없다는 오류 | `Yarn Project`에 해당 `.yarn` 파일이 없음 | Source Scripts에 파일 추가 |
| 대화 중 프롬프트가 계속 뜸 | `InteractionPrompt`가 `dialogueRunner.IsDialogueRunning` 확인 못 함 | DialogueRunner가 제대로 연결됐는지 확인 |
| `$ascensionCount`오류 | 변수 선언이 Yarn Project에 없음 | `.yarn` 파일 상단에 `<<declare $ascensionCount = 0>>` 추가 |

---

## 부록: D1_A.yarn 변수 선언 추가 권장

`D1_A.yarn`에서 `$ascensionCount`를 `<<set>>`으로 쓰고 있는데,  
초기 선언이 없으면 런타임 경고가 뜹니다. `.yarn` 파일 맨 위에 추가하세요.

```yarn
<<declare $ascensionCount = 0>>

title: D1_A
tags:
---
...
```

---

> 이 가이드는 **Yarn Spinner 3.x (DialoguePresenterBase / YarnTask 기반)** 기준입니다.
