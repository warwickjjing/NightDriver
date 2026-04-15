# NightDriver 기본 세팅 가이드

이 문서는 `NightDriver` 프로젝트를 처음 열었을 때 **기본으로 세팅해야 하는 것들**을 한 번에 확인하기 위한 체크리스트입니다.

---

## 프로젝트 열기/버전

- **Unity 버전**: (프로젝트 `ProjectSettings/ProjectVersion.txt` 기준) 동일 버전으로 열기
- **플랫폼**: PC(Windows) 기준
- **권장**: 새로 클론/다운받은 뒤 첫 오픈 시 `Library` 재생성까지 기다리기

---

## 핵심 런타임 구조(Managers)

현재 구조는 `GameManager` 단일 진입점에서 여러 매니저를 참조합니다.

- `**GameManager`**
  - `DontDestroyOnLoad`로 유지
  - 하위에 `NightManager`, `EndingTracker`, `MetaSaveSystem`이 있으면 자동으로 찾아 연결합니다.
- `**NightManager**`
  - 현재 진행 상태(`NightRuntimeState`)를 들고 있음
  - 일차(`dayIndex`) 변경 이벤트: `OnDayChanged(int)`
  - 페이즈 변경 이벤트: `OnPhaseChanged(NightPhase)`

관련 코드:

- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/NightManager.cs`
- `Assets/Scripts/Core/MetaSaveSystem.cs`

### 씬에 반드시 있어야 하는지 확인

- **체크**: 플레이 시작 시 Hierarchy에 `GameManager`(또는 그에 준하는 루트 오브젝트)가 존재해야 함
- **체크**: `GameManager`가 `NightManager`를 찾지 못하면(Null) 밤/일차/페이즈 기반 로직이 동작하지 않음

---

## 씬 구성(Scenes)

현재 프로젝트에 확인되는 씬:

- `Assets/Scenes/Driver Scene.unity`
- `Assets/Low_Poly_Sci-fi_City/Scene/City_Scene.unity`

### 권장 워크플로우

- **테스트 씬**: `Driver Scene`에서 최소 루프(조작/카메라/일차 UI) 확인
- **아트 씬**: `City_Scene`는 환경/배경 확인

---

## 입력(Input)

### 카메라 입력(레거시 Input 기준)

`FirstPersonCamera`는 레거시 Input의 축을 사용합니다.

- 사용 축: `"Mouse X"`, `"Mouse Y"`
- **체크**: `Project Settings > Input Manager`에 축이 존재해야 함
- **주의**: `Active Input Handling`이 “New Input System Only”이면 `Input.GetAxisRaw`가 0이 될 수 있음  
  - 해결: “Both” 또는 레거시 입력 사용 환경으로 맞추기

관련 코드:

- `Assets/Scripts/Character/Camera/FirstPersonCamera.cs`

---

## 카메라(Camera) 기본 세팅

`FirstPersonCamera` 구조(의도):

- **Yaw(좌우)**: `playerRoot`
- **Pitch(상하)**: `cameraPivot`

### 체크리스트

- **체크**: 씬에서 `FirstPersonCamera` 컴포넌트가 활성화되어 있는가
- **체크**: `playerRoot`와 `cameraPivot`가 올바르게 할당되어 있는가
- **권장**: `Main Camera`를 `playerRoot`의 자식으로 두면 가장 깔끔합니다.

---

## UI(Canvas) 기본 세팅

### “X일차” 표시 + 버튼 이벤트(권장 구성)

일차는 `NightManager.State.dayIndex`를 표시합니다.

- **표시 스크립트**: `DayCounterUI`
  - 텍스트 포맷 기본값: `{0}일차`
  - `NightManager.OnDayChanged(int)`를 구독해서 자동 갱신
  - 버튼 이벤트용 메서드 제공:
    - `AdvanceToNextNight()`: 다음 일차로 진행
    - `BeginNight(int dayIndex)`: 특정 일차로 시작

파일:

- `Assets/Scripts/UI/DayCounterUI.cs`

#### Hierarchy 예시

- `Canvas`
  - `DayText` (UI Text)
  - `NextDayButton` (UI Button)
  - `DayCounterUI` (Canvas 또는 별도 빈 오브젝트에 붙이기)

#### Button 연결 방법

- `NextDayButton > OnClick()`
  - 대상: `DayCounterUI`가 붙은 오브젝트
  - 함수: `DayCounterUI.AdvanceToNextNight`

> 참고: 기본 `Text`(UGUI) 기준입니다. TextMeshPro를 쓰는 경우 `DayCounterUI`를 TMP용으로 별도 버전(TMP_Text)으로 바꾸는 것을 권장합니다.

### 파티클(이펙트) 버튼 토글(선택)

Canvas에 버튼을 추가해두고, 클릭할 때마다 파티클 이펙트를 **On/Off 토글**하고 싶으면 아래 스크립트를 사용합니다.

- **토글 스크립트**: `ParticleEffectUIButton`
  - 버튼을 누를 때마다 이펙트 루트 오브젝트를 토글합니다.
    - On: `SetActive(true)` + 자식 `ParticleSystem` 전부 `Play()`
    - Off: `Stop(StopEmittingAndClear)` 후 `SetActive(false)`
  - `particleRoot`에는 **씬에 있는 오브젝트**를 넣어도 되고, **프리팹 에셋**을 넣어도 됩니다.
    - 프리팹 에셋을 넣으면, 첫 On 시점에만 한 번 Instantiate하고 이후에는 같은 인스턴스를 재사용합니다.

파일:

- `Assets/Scripts/UI/ParticleEffectUIButton.cs`

#### 연결 방법(둘 중 하나)

- **방법 A (추천)**: 이 스크립트를 아무 GameObject(예: Canvas 아래 빈 오브젝트)에 붙이고
  - `triggerButton`에 버튼을 할당
  - `particleRoot`에 프리팹/씬 오브젝트 할당
  - (선택) `spawnParent`에 원하는 위치(Transform) 할당
- **방법 B**: 버튼의 `OnClick()`에 `ParticleEffectUIButton.ToggleParticleEffect()`를 직접 연결

---

## 권장 Hierarchy 프리셋(씬 시작 템플릿)

아래 구조는 “최소 구동(매니저 + 카메라/조작 + UI)” 기준의 권장 템플릿입니다.  
**`App` / `World` / `UI`** 는 그냥 그룹용 루트 이름입니다. `@` 를 붙이면(예: `@App`) Hierarchy 정렬 시 맨 위로 오게 할 수 있어서 편할 뿐, 기능 차이는 없습니다.

---

### 1) 트리 구조만 (오브젝트 이름)

```
App
└ GameManager
  └ NightManager
  └ MetaSaveSystem
  └ EndingTracker

World
└ PlayerRoot
  └ CameraRig
    └ Main Camera

UI
└ Canvas
  └ DayCountUI
    └ DayCountText
  └ NextDayButton
    └ NextDayButtonText
  └ PanelLeft
└ EventSystem
```

- **App**: 매니저/싱글톤용 루트 (DontDestroyOnLoad 권장)
- **World**: 플레이어·카메라·월드 오브젝트
- **UI**: 캔버스·이벤트시스템
- **PanelLeft**: 1~7일차 버튼이 생성될 컨테이너(선택). 여기에 `DaySelectorUI`를 붙이면 됨.

---

### 2) 컴포넌트 설정 (오브젝트별)

| 오브젝트 | 붙일 컴포넌트 | 인스펙터 할당 / 비고 |
|----------|----------------|------------------------|
| **App** | (없음) | DontDestroyOnLoad 체크해도 됨(또는 GameManager에서 처리). |
| **GameManager** | `NightDriver.Core.GameManager` | `nightManager` → NightManager, `metaSaveSystem` → MetaSaveSystem, `endingTracker` → EndingTracker(사용 시). |
| **NightManager** | `NightDriver.Core.NightManager` | (없음) |
| **MetaSaveSystem** | `NightDriver.Core.MetaSaveSystem` | (없음) |
| **EndingTracker** | `NightDriver.Core.EndingTracker` | (없음) |
| **PlayerRoot** | (플레이어/이동 스크립트 등) | (없음) |
| **CameraRig** | (없음) | (없음) |
| **Main Camera** | `Camera`, `AudioListener`, `NightDriver.Character.Camera.FirstPersonCamera` | Tag = MainCamera. FirstPersonCamera: `playerRoot` → PlayerRoot, `cameraPivot` → Main Camera. |
| **Canvas** | `Canvas`, `CanvasScaler`, `GraphicRaycaster` | (없음) |
| **DayCountUI** | `NightDriver.UI.DayCounterUI` | `dayText` → DayCountText(TMP_Text). (권장) `nightManager` → GameManager/NightManager. |
| **DayCountText** | `TextMeshProUGUI` (TMP_Text) | (없음) |
| **NextDayButton** | `Button` (+ Image 등) | `onClick` → DayCountUI.AdvanceToNextNight() |
| **NextDayButtonText** | `TMP_Text` | (없음) |
| **PanelLeft** | `VerticalLayoutGroup`, `ContentSizeFitter`(선택), `NightDriver.UI.DaySelectorUI` | DaySelectorUI: `buttonContainer` → PanelLeft(또는 자식), `dayButtonPrefab` → 버튼 프리팹. |
| **EventSystem** | `EventSystem`, `StandaloneInputModule`(또는 New Input용 모듈) | 씬에 1개 필요. |

---

### 3) 프리팹으로 저장해서 재사용/할당하면 좋은 것

- `**@App`(Boot/Managers) 프리팹** *(강력 추천)*
  - 목적: 어떤 씬을 열어도 `GameManager + NightManager + Save` 구성이 동일하게 유지
  - 방법: `@App`(또는 `GameManager` 루트) 오브젝트를 프리팹으로 저장 후, 각 씬에 배치
  - 주의: `GameManager`는 `DontDestroyOnLoad`라서 **중복 생성**되면 자동으로 하나가 파괴됩니다.  
    - 씬마다 여러 개 두지 말고 “첫 씬에만 배치” 또는 “항상 1개만 존재” 전략을 택하세요.
- `**@UI` 프리팹**
  - 목적: UI(일차 텍스트/버튼/이벤트시스템/스케일러)를 씬마다 재사용
  - 포함 권장: `Canvas` + `EventSystem` + `DayCounterUI` 세트
- `**PlayerRoot(+ CameraRig)` 프리팹** *(선택)*
  - 목적: 플레이어/차량/카메라 리그 구성을 씬마다 동일하게 유지
  - 포함 권장: `PlayerRoot` + `CameraRig` + `Main Camera` + `FirstPersonCamera`

### 4) 할당 체크 요약

- **GameManager**: `nightManager` / `metaSaveSystem` / `endingTracker` 직접 할당 권장.
- **FirstPersonCamera**: `playerRoot` = PlayerRoot, `cameraPivot` = Main Camera.
- **DayCounterUI**: `dayText` = DayCountText, (권장) `nightManager` = GameManager/NightManager.
- **NextDayButton**: `onClick` → DayCountUI.AdvanceToNextNight().
- **DaySelectorUI**(PanelLeft): `buttonContainer` = PanelLeft(또는 자식), `dayButtonPrefab` = 버튼 프리팹.

### 5) 기능별로 추가할 때

- **일차 표시**: DayCountUI + DayCountText + DayCounterUI (위 트리/표 참고).
- **1~7일차 버튼**: PanelLeft + DaySelectorUI, 버튼 프리팹·컨테이너 할당.
- **1인칭 카메라**: Main Camera에 FirstPersonCamera, playerRoot/cameraPivot 할당.

> 참고: UI 텍스트는 TMP(`TMP_Text`) 사용.

---

## 손님(리스폰) + Yarn 대화 세팅(필수 오브젝트 체크리스트)

아래는 “일차/콜에 따라 손님을 스폰(리스폰)하고, 손님에게 말 걸면 Yarn 대화가 시작되는” 흐름을 위해 **씬에 만들어야 하는 오브젝트**와 **에셋** 목록입니다.  
현재 설계는 **손님(Client)마다 스폰 위치/목적지/대사/선택지가 고정**되어 있고, 씬에는 “ID → Transform” 매핑만 둡니다.

### 1) 씬에 만들어야 하는 오브젝트

```
App
└ ClientSpawner

World
└ SpawnPointSet
  └ (여러 SpawnPoint Transform들)
└ DestinationSet
  └ (여러 Destination Transform들)
└ (선택) DefaultSpawnPoint

(손님 프리팹)
└ ClientRoot
  └ ClientDialogueTarget
  └ InteractableClientDialogue

App (또는 UI)
└ @Dialogue
  └ DialogueService
  └ DialogueRunner (+ Yarn UI)
```

- **`App/ClientSpawner`**
  - 컴포넌트: `NightDriver.Client.ClientSpawner`
  - 할당:
    - `nightManager` → 씬의 `NightManager` (비워도 자동 탐색 가능)
    - `schedule` → 아래에서 만드는 `WeekSchedule` 에셋
    - `spawnPointSet` → `World/SpawnPointSet` (손님별 `spawnPointId` 사용)
    - `defaultSpawnPoint` → (옵션) 스폰 ID를 못 찾았을 때 사용할 기본 위치
- **`World/SpawnPointSet`**
  - 컴포넌트: `NightDriver.Client.SpawnPointSet`
  - 역할: `spawnPointId` → 실제 Transform 매핑
- **`World/DestinationSet`**
  - 컴포넌트: `NightDriver.Client.DestinationSet`
  - 역할: `destinationId` → 실제 Transform 매핑
- **`@Dialogue`**
  - Yarn의 `DialogueRunner`와 UI가 들어있는 루트(프리팹화 추천)
  - `DialogueService`는 씬당 1개만 존재하도록 구성

### 2) 만들어야 하는 ScriptableObject(에셋)

- **`WeekSchedule`**
  - Create → `NightDriver/Client/Week Schedule`
  - Day1=[A,B,C,D], Day2=[E,F,G,H] 처럼 “순서 리스트” 입력
- **`ClientDefinition` (손님마다 1개 이상)**
  - Create → `NightDriver/Client/Client Definition`
  - 설정:
    - `clientId` (예: `A`)
    - `prefab` (손님 프리팹)
    - `spawnPointId` (예: `A_spawn` → SpawnPointSet에 동일 ID 등록)
    - `startNode` (Yarn 시작 노드명, 예: `A_Start`)
    - `destinationOptions[]` (손님별 고정 선택지)
      - `optionId` / `displayName` / `destinationId` / `nextNode`

### 3) 손님 프리팹에 반드시 있어야 하는 컴포넌트

- **`ClientDialogueTarget`** (`NightDriver.Dialogue.ClientDialogueTarget`)
  - `startNode`가 비어있으면 대화가 시작되지 않습니다.
- **`InteractableClientDialogue`** (`NightDriver.Dialogue.InteractableClientDialogue`)
  - 플레이어의 `Interactor`로 “말 걸기” 시 Yarn 대화를 시작합니다.
  - “이번 손님만 말 걸기” 제한이 걸려 있습니다(현재 손님만 상호작용 가능).

### 4) 테스트 순서(최소)

- `ClientSpawner.schedule`에 `WeekSchedule` 할당
- Day 버튼으로 1~7일차 변경 → 해당 Day/콜 손님이 올바른 **스폰 위치(spawnPointId)** 에 리스폰되는지 확인
- 플레이어가 손님에게 접근 후 상호작용 → Yarn 대화가 시작되는지 확인

## Yarn Spinner(대화/선택지) 세팅

이 프로젝트는 Yarn Spinner를 UPM(Git)으로 이미 포함하고 있습니다.
- 패키지: `dev.yarnspinner.unity` (`Packages/manifest.json` 확인)

### 1) 기본 오브젝트 구성(권장: 프리팹화)

- `@Dialogue` (GameObject, **DontDestroyOnLoad 권장**)
  - `DialogueService` (Component: `NightDriver.Dialogue.DialogueService`)
  - `DialogueRunner` (Component: `Yarn.Unity.DialogueRunner`)
    - `Yarn Project` 할당 (아래 2) 참고)
  - (UI) Yarn Spinner 기본 UI 프리팹 또는 커스텀 UI (아래 4) 참고)

> Yarn Spinner는 기본 제공 UI 프리팹/생성 메뉴가 있습니다.  
> 에디터에서 Yarn 관련 메뉴를 통해 `DialogueRunner + UI` 세트를 생성한 뒤, 그 루트를 `@Dialogue` 프리팹으로 저장해두는 것을 권장합니다.

### 2) Yarn Project 생성/할당

- `Assets/Dialogue/` 폴더 생성(권장)
- Yarn 스크립트 파일(`.yarn`)을 그 안에 저장
- `Yarn Project` 에셋을 생성하고, 대화 스크립트를 추가
- 씬의 `DialogueRunner`에 해당 `Yarn Project`를 할당

### 3) DialogueService 연결(권장 설정)

`DialogueService`는 자동으로 `DialogueRunner`를 자식에서 찾아오지만, 실수 방지를 위해 아래처럼 **직접 할당**을 권장합니다.

- `DialogueService (NightDriver.Dialogue.DialogueService)`
  - `runner` → 같은 루트(`@Dialogue`) 아래의 `DialogueRunner`
  - `variables` → (선택) 별도 VariableStorage를 쓰는 경우만 할당  
    - 보통은 `DialogueRunner`의 VariableStorage를 자동 사용하므로 비워둬도 됩니다.

### 4) “대사가 화면에 보이게” UI(뷰) 생성/할당 (필수)

`DialogueRunner`에 Yarn Project가 할당되어 있어도, **표시용 UI(라인/선택지 Presenter/View)가 없으면 화면에 아무것도 안 뜰 수 있습니다.**

- **방법 A(권장): Yarn Spinner 기본 UI 프리팹 사용**
  - Yarn Spinner의 샘플/기본 UI 프리팹을 씬에 추가
  - 해당 UI 오브젝트의 `Line View` / `Options List View`(버전마다 명칭 상이)를 `DialogueRunner`의 Presenter/View 목록에 연결

- **방법 B: 커스텀 UI 사용**
  - `LineView`(대사 표시) + `OptionsListView`(선택지 버튼) 구성
  - `DialogueRunner`가 참조하도록 연결

체크:
- `DialogueRunner` 인스펙터에서 **Dialogue View/Presenter 목록이 비어있지 않은지** 확인

### 5) 손님(클라이언트)에 대화 연결

손님 프리팹(또는 씬 오브젝트)에 아래 컴포넌트를 붙입니다.
- `ClientDialogueTarget` (`NightDriver.Dialogue.ClientDialogueTarget`)
  - `startNode`: 이 손님과 상호작용 시 시작할 Yarn 노드 이름
- `InteractableClientDialogue` (`NightDriver.Dialogue.InteractableClientDialogue`)
  - Interactor 시스템을 통해 “말 걸기”로 대화 시작

관련 스크립트:
- `Assets/Scripts/Dialogue/DialogueService.cs`
- `Assets/Scripts/Dialogue/ClientDialogueTarget.cs`
- `Assets/Scripts/Dialogue/InteractableClientDialogue.cs`

### 6) “말걸기 프롬프트 + E키 상호작용” 세팅 (필수)

대화가 시작되려면 플레이어가 `Interactor.TryInteract()`를 호출해야 합니다.

- 플레이어(Driver)에 아래 컴포넌트가 있어야 함
  - `Interactor` (`NightDriver.Character.Interaction.Interactor`)
  - `InteractorInput` (`NightDriver.Character.Interaction.InteractorInput`)
    - `promptText`에 TMP UI 텍스트를 할당(권장)
    - 기본 상호작용 키: `E`

관련 스크립트:
- `Assets/Scripts/Character/Interaction/Interactor.cs`
- `Assets/Scripts/Character/Interaction/InteractorInput.cs`

### 체크 포인트(자주 터지는 것)

- **카메라 회전**: `Main Camera`가 `PlayerRoot`의 자식(또는 `CameraRig`의 자식)인지 확인  
  - 그렇지 않으면 Yaw가 플레이어에만 적용되어 “좌우만 안 도는” 증상이 생길 수 있습니다.
- **UI 클릭**: `EventSystem`이 씬에 반드시 1개 있어야 버튼 클릭이 동작합니다.
- **매니저 연결**: `GameManager`의 인스펙터에서 `NightManager/MetaSaveSystem` 참조가 Null이면 하위에서 자동 탐색되도록 배치했는지 확인합니다.
- **안개(포그) 토글 버튼이 안 보인다**: 현재 프로젝트 기본 UI에는 “안개 디버그 토글” 버튼이 포함되어 있지 않습니다(필요하면 별도 버튼/스크립트로 추가).

---

## 세이브/로드(메타)

현재 메타 저장은 `PlayerPrefs` + JSON입니다.

- 키: `NightDriver.MetaSave.v1`
- 저장 시점: `GameManager.OnApplicationQuit()`에서 `MetaSaveSystem.Save()`

관련 코드:

- `Assets/Scripts/Core/MetaSaveSystem.cs`

### 체크리스트

- **체크**: 에디터 종료/플레이 종료 시 저장이 필요하면, Quit 외에도 명시 저장 버튼/트리거를 추가하는 것을 고려

---

## 폴더/스크립트 규칙(권장)

- **코어**: `Assets/Scripts/Core`
- **캐릭터**: `Assets/Scripts/Character`
- **UI**: `Assets/Scripts/UI`
- **네임스페이스**: `NightDriver.`* 유지

---

## 빌드/실행 기본 체크

- **씬 등록**: `File > Build Settings`에 필요한 씬들이 포함되어 있는지
- **GameManager 존재**: 첫 씬에서 `GameManager`가 생성/배치되어 있는지
- **입력 확인**: 마우스 축이 0이 아닌지(커서 락 상태 포함)
- **UI 확인**: `X일차` 표시가 바뀌는지(버튼 클릭 포함)

---

## 자주 발생하는 문제 빠른 진단

- **카메라 좌우가 안 돈다**
  - `playerRoot`만 회전하고 카메라가 자식이 아니면 시야가 안 돌 수 있음
  - 입력 설정(레거시/신 Input System) 불일치로 `Mouse X`가 0일 수도 있음
- **일차가 안 바뀐다**
  - 씬에 `GameManager`/`NightManager`가 없거나 참조가 끊어진 경우
  - UI가 `NightManager`를 못 찾는 경우(인스펙터에 직접 할당 권장)

