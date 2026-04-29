# 폰 콜 수신 & 네비게이션 시스템 — 씬 배치 가이드

> 생성일: 2026-04-15  
> 업데이트: 2026-04-28 — UI 기반 Hierarchy / Inspector 설정 섹션 추가  
> 관련 파일: `Assets/Scripts/UI/` 하위 7개 스크립트

---

## 목차

1. [전체 구조 한눈에 보기](#1-전체-구조-한눈에-보기)
2. [콜 수신 시스템 (4개 컴포넌트)](#2-콜-수신-시스템)
3. [네비게이션 시스템 (3개 컴포넌트)](#3-네비게이션-시스템)
4. [씬 계층(Hierarchy) 예시](#4-씬-계층hierarchy-예시)
5. [Inspector 연결 체크리스트](#5-inspector-연결-체크리스트)
6. [게임 코드에서 호출하는 법](#6-게임-코드에서-호출하는-법)
7. [공포 연출 함수 사용법](#7-공포-연출-함수-사용법)
8. [자주 실수하는 것](#8-자주-실수하는-것)
9. [PhoneCallApp UI 상세 배치 (스크린샷 기준)](#9-phonecallapp-ui-상세-배치-스크린샷-기준)

---

## 1. 전체 구조 한눈에 보기

```
[콜 수신 흐름]
CallFlowController ──▶ CallNotificationSystem ──▶ CallNotificationBanner (배너)
                                                ├──▶ PhoneManager (폰 열기/닫기)
                                                └──▶ PhoneCallApp (콜 앱 화면)

[폰 열기 흐름]
E키 / 씬 폰 클릭 ──▶ PhoneManager.OpenPhone() ──▶ CallNotificationSystem.OnPhoneOpened()
                                                 └──▶ PhoneCallApp.ShowCallScreen()

[수락 흐름]
PhoneCallApp 수락 버튼 ──▶ CallNotificationSystem.AcceptCall()
                          ├──▶ CallFlowController.TryAcceptCall()  (손님 스폰)
                          ├──▶ PhoneManager.ClosePhone()
                          └──▶ NavigationManager.SetPickupMode()   (픽업 네비 시작)

[네비게이션 흐름]
SetPickupMode(t)   → PhoneNaviApp 활성,  NavigationHUD 비활성
SetDrivingMode(t)  → PhoneNaviApp 비활성, NavigationHUD 활성
SetNaviOff()       → 전부 비활성
```

---

## 2. 콜 수신 시스템

### 2-1. CallNotificationSystem

**파일**: `Assets/Scripts/UI/CallNotificationSystem.cs`  
**역할**: 콜 수신 전체 흐름을 조율하는 **오케스트레이터** (싱글톤)

#### 생성 방법
1. 씬 Hierarchy에서 빈 GameObject 생성 → 이름 `CallNotificationSystem`
2. `Add Component → NightDriver/UI/Call Notification System`

#### Inspector 연결

| 필드 | 연결 대상 | 비고 |
|------|-----------|------|
| Phone Manager | `PhoneManager` 오브젝트 | |
| Banner | `CallNotificationBanner` 오브젝트 | |
| Call App | `PhoneCallApp` 오브젝트 | |
| Navi Manager | `NavigationManager` 오브젝트 | 없으면 자동 탐색 |
| Call Flow | `CallFlowController` 오브젝트 | 없으면 자동 탐색 |
| Ringtone Source | AudioSource 컴포넌트 | 같은 오브젝트에 붙이거나 별도 연결 |
| Ringtone Clip | AudioClip | 알림음 파일 |
| Driver Monologues | string[] | 인덱스 0 = 1일차, 1 = 2일차 … |
| Monologue Text | TMP_Text | 독백 표시 텍스트 (선택) |

---

### 2-2. CallNotificationBanner

**파일**: `Assets/Scripts/UI/CallNotificationBanner.cs`  
**역할**: 화면 상단에서 슬라이드 다운으로 나타나는 알림 배너

#### 생성 방법
1. Screen Space – Overlay Canvas 하위에 **Panel** 생성 → 이름 `NotificationBanner`
2. Anchor: **Top Center**, Pivot: **(0.5, 1)**
3. 크기 예시: W=600, H=80  
4. `Add Component → NightDriver/UI/Call Notification Banner`
5. 하위에 **TextMeshPro – Text (UI)** 추가 → `messageText` 연결

#### 위치 설정 요령

```
hiddenAnchoredY = 100     ← 화면 위쪽 바깥 (양수 = 위)
shownAnchoredY  = -10     ← 화면 안쪽 (여백 10px)
```

- Pivot Y=1 기준으로 hiddenAnchoredY를 **양수**로 설정하면 화면 위쪽 밖에 숨겨집니다.

#### Inspector 연결

| 필드 | 연결 대상 |
|------|-----------|
| Banner Root | 이 Panel의 RectTransform |
| Message Text | 하위 TMP_Text |
| Background Image | Panel의 Image 컴포넌트 (선택) |
| Accent Color | 주황/빨강 계열 강조색 |

---

### 2-3. PhoneCallApp

**파일**: `Assets/Scripts/UI/PhoneCallApp.cs`  
**역할**: 폰 패널 안의 콜 앱 화면 (손님 정보 + 수락 버튼)

#### 생성 방법
1. `PhonePanel` 하위에 **Panel** 생성 → 이름 `PhoneCallApp`
2. `Add Component → NightDriver/UI/Phone Call App`
3. 하위에 필요한 TMP_Text / Button을 배치하고 연결

#### 추천 UI 구성

```
PhoneCallApp (Panel)
├─ ClientNameText      (TMP_Text)  — 손님 이름
├─ PickupLocationText  (TMP_Text)  — 픽업 위치
├─ EstimatedFareText   (TMP_Text)  — 예상 요금
├─ AcceptButton        (Button)    — 수락 버튼
├─ AcceptedFeedback    (TMP_Text)  — "배차 완료" (평소 숨김)
└─ BadgeDot            (Image)     — 앱 아이콘 뱃지 (!)
```

#### Inspector 연결

| 필드 | 연결 대상 |
|------|-----------|
| App Root | `PhoneCallApp` 패널 자체 |
| Client Name Text | 손님 이름 TMP_Text |
| Pickup Location Text | 픽업 위치 TMP_Text |
| Estimated Fare Text | 예상 요금 TMP_Text |
| Accept Button | 수락 Button |
| Accepted Feedback Text | "배차 완료" TMP_Text |
| Badge Object | 뱃지 Image 오브젝트 |

> **주의**: `Accept Button`의 `OnClick()` 이벤트에는 **아무것도 연결하지 않아도 됩니다.**  
> 스크립트 `Awake()`에서 자동으로 `CallNotificationSystem.AcceptCall()`에 연결됩니다.

---

### 2-4. PhoneManager

**파일**: `Assets/Scripts/UI/PhoneManager.cs`  
**역할**: 폰 열기/닫기 통합 제어 (E키 토글, 운전 중 잠금, 뱃지)

#### 생성 방법
1. UICanvas 루트에 빈 GameObject 생성 → 이름 `PhoneManager`
2. `Add Component → NightDriver/UI/Phone Manager`
3. 씬에 **PhonePanel** (슬라이드 대상 RectTransform) 배치

#### PhonePanel 설정 요령

```
PhonePanel RectTransform
  Anchor : Bottom Center   (또는 원하는 위치)
  Pivot  : (0.5, 0)
  hiddenAnchoredY = -1200  ← 화면 아래 밖
  shownAnchoredY  = 0      ← 화면 기준 위치
```

#### Inspector 연결

| 필드 | 연결 대상 | 비고 |
|------|-----------|------|
| Phone Panel | `PhonePanel` RectTransform | |
| Toggle Key | `E` | 기본값 |
| Phone Object Button | 씬 3D 폰 모델의 Button (선택) | 클릭으로 열기 |
| Badge Dot | 뱃지 오브젝트 | CallNotificationBanner 뱃지와 별개 |
| Call App | `PhoneCallApp` 컴포넌트 | 뱃지 동기화용 |
| Unavailable Hint Text | TMP_Text (선택) | "운전 중 사용 불가" 힌트 |

> **기존 `PhoneUIController`와 충돌**:  
> `PhoneUIController`는 `Tab` 키, `PhoneManager`는 `E` 키를 사용합니다.  
> 같은 씬에 둘 다 있으면 `phonePanel`이 두 곳에서 제어되어 충돌합니다.  
> 새 씬에서는 `PhoneUIController`를 **비활성화**하고 `PhoneManager`만 사용하세요.

---

## 3. 네비게이션 시스템

### 3-1. NavigationManager

**파일**: `Assets/Scripts/UI/NavigationManager.cs`  
**역할**: 네비게이션 모드 전환 오케스트레이터 (싱글톤)

#### 생성 방법
1. 씬에 빈 GameObject → 이름 `NavigationManager`
2. `Add Component → NightDriver/UI/Navigation Manager`

#### Inspector 연결

| 필드 | 연결 대상 |
|------|-----------|
| Hud | `NavigationHUD` 컴포넌트 |
| Phone Navi | `PhoneNaviApp` 컴포넌트 |
| Glitch Max Angle | 170 (기본) |
| Glitch Blink Interval | 0.12 |
| Glitch Duration | 2 |

---

### 3-2. NavigationHUD

**파일**: `Assets/Scripts/UI/NavigationHUD.cs`  
**역할**: 드라이빙 모드 방향 화살표 + 거리 텍스트 HUD

#### 생성 방법
1. Screen Space – Overlay Canvas 하위에 Panel 생성 → 이름 `NavigationHUD`
2. `Add Component → NightDriver/UI/Navigation HUD`
3. 화살표 이미지(Image + RectTransform)와 TMP_Text 배치

#### 추천 UI 구성

```
NavigationHUD (Panel = hudRoot)
├─ ArrowImage   (Image)     — 위 방향 화살표 스프라이트
└─ DistanceText (TMP_Text)  — "X.Xkm" / "XXm 앞" / "목적지 부근입니다."
```

#### Inspector 연결

| 필드 | 연결 대상 | 비고 |
|------|-----------|------|
| Arrow Image | 화살표 Image의 RectTransform | |
| Distance Text | TMP_Text | |
| Hud Root | Panel 오브젝트 | null이면 각 요소 개별 제어 |
| Player Transform | 플레이어 Transform | null이면 Camera.main 사용 |
| Near Threshold | 10 | m |
| Mid Threshold | 100 | m |

---

### 3-3. PhoneNaviApp

**파일**: `Assets/Scripts/UI/PhoneNaviApp.cs`  
**역할**: 도보 픽업 모드 전용 폰 내 네비 앱 화면

#### 생성 방법
1. `PhonePanel` 하위에 Panel 생성 → 이름 `PhoneNaviApp`
2. `Add Component → NightDriver/UI/Phone Navi App`

#### 추천 UI 구성

```
PhoneNaviApp (Panel = appRoot)
├─ PickupNameText  (TMP_Text)  — "손님A 픽업"
├─ DistanceText    (TMP_Text)  — 거리
└─ ArrowImage      (Image)     — 방향 화살표 (선택)
```

#### Inspector 연결

| 필드 | 연결 대상 |
|------|-----------|
| App Root | Panel 오브젝트 |
| Pickup Name Text | TMP_Text |
| Distance Text | TMP_Text |
| Arrow Image | RectTransform (선택) |
| Pickup Label Format | `"{0} 픽업"` |

---

## 4. 씬 계층(Hierarchy) 예시

```
Scene
└─ UICanvas  [Canvas, CanvasScaler, GraphicRaycaster]
    │
    ├─ NotificationBanner      [CallNotificationBanner]
    │   └─ MessageText         [TMP_Text]
    │
    ├─ NavigationHUD           [NavigationHUD]
    │   ├─ ArrowImage          [Image]
    │   └─ DistanceText        [TMP_Text]
    │
    └─ PhonePanel              [RectTransform] ← PhoneManager가 슬라이드 제어
        ├─ PhoneCallApp        [PhoneCallApp]
        │   ├─ ClientNameText
        │   ├─ PickupLocationText
        │   ├─ EstimatedFareText
        │   ├─ AcceptButton
        │   ├─ AcceptedFeedback
        │   └─ BadgeDot
        └─ PhoneNaviApp        [PhoneNaviApp]
            ├─ PickupNameText
            ├─ DistanceText
            └─ ArrowImage

Managers (빈 GameObject 모음)
├─ CallNotificationSystem     [CallNotificationSystem, AudioSource]
├─ PhoneManager               [PhoneManager]
└─ NavigationManager          [NavigationManager]
```

---

## 5. Inspector 연결 체크리스트

### CallNotificationSystem
- [ ] `phoneManager` → PhoneManager
- [ ] `banner` → CallNotificationBanner
- [ ] `callApp` → PhoneCallApp
- [ ] `naviManager` → NavigationManager (없으면 자동)
- [ ] `callFlow` → CallFlowController (없으면 자동)
- [ ] `ringtoneSource` → AudioSource
- [ ] `ringtoneClip` → 알림음 AudioClip
- [ ] `driverMonologues` → 일차별 독백 텍스트 배열 입력
- [ ] `monologueText` → 독백 TMP_Text (선택)

### CallNotificationBanner
- [ ] `bannerRoot` → 배너 Panel RectTransform
- [ ] `messageText` → TMP_Text
- [ ] `backgroundImage` → Image (선택)
- [ ] `hiddenAnchoredY` / `shownAnchoredY` 위치 맞춤

### PhoneCallApp
- [ ] `appRoot` → PhoneCallApp Panel
- [ ] `clientNameText` → TMP_Text
- [ ] `pickupLocationText` → TMP_Text
- [ ] `estimatedFareText` → TMP_Text
- [ ] `acceptButton` → Button
- [ ] `acceptedFeedbackText` → TMP_Text
- [ ] `badgeObject` → 뱃지 Image 오브젝트

### PhoneManager
- [ ] `phonePanel` → PhonePanel RectTransform
- [ ] `badgeDot` → 뱃지 GameObject
- [ ] `callApp` → PhoneCallApp
- [ ] `hiddenAnchoredY` / `shownAnchoredY` 위치 맞춤

### NavigationManager
- [ ] `hud` → NavigationHUD
- [ ] `phoneNavi` → PhoneNaviApp

### NavigationHUD
- [ ] `arrowImage` → 화살표 RectTransform
- [ ] `distanceText` → TMP_Text
- [ ] `hudRoot` → Panel (선택)

### PhoneNaviApp
- [ ] `appRoot` → Panel
- [ ] `pickupNameText` → TMP_Text
- [ ] `distanceText` → TMP_Text

---

## 6. 게임 코드에서 호출하는 법

### 콜 수신 (외부 → CallNotificationSystem)

```csharp
// 콜이 들어왔을 때 (예: NightManager, GameManager 등에서 호출)
CallNotificationSystem.Instance.ReceiveCall(clientDefinition);
```

### 콜 수락 후 픽업 네비 시작

콜 수락은 `PhoneCallApp` 수락 버튼 → `CallNotificationSystem.AcceptCall()` 경로로  
자동 처리됩니다. 단, `NavigationManager.SetPickupMode(pickupTransform)`은  
**손님 스폰이 완료된 시점**에 호출해야 올바른 Transform을 받을 수 있습니다.

```csharp
// ClientSpawner.AfterClientSpawnComplete 이벤트 구독 예시
spawner.AfterClientSpawnComplete += () =>
{
    var spawnTransform = ClientRegistry.CurrentClientObject?.transform;
    if (spawnTransform != null)
        NavigationManager.Instance.SetPickupMode(spawnTransform);
};
```

### 손님 탑승 후 드라이빙 네비 시작

```csharp
// VehicleSeatInteraction 또는 VehicleBoarding에서 탑승 완료 시
NavigationManager.Instance.SetDrivingMode(destinationTransform);

// 운전 중 폰 열기 잠금
PhoneManager.Instance.SetCanOpenPhone(false);
```

### 하차 완료 후 네비 종료

```csharp
NavigationManager.Instance.SetNaviOff();

// 폰 열기 다시 허용
PhoneManager.Instance.SetCanOpenPhone(true);
```

---

## 7. 공포 연출 함수 사용법

### HUD 글리치 (화살표 깜빡임 + 방향 무작위)

```csharp
// 2초간 HUD 화살표가 랜덤 방향으로 깜빡임
NavigationManager.Instance.TriggerHUDGlitch();
```

### 가짜 목적지 (빈 공터로 안내)

```csharp
// HUD가 fakeTarget을 가리키다가 도착 시 "목적지 부근입니다." 출력
NavigationManager.Instance.TriggerWrongDestination(fakeTargetTransform);
```

### 글리치 린턴 (알림음 공포 연출)

```csharp
// 알림음 피치를 낮추고 끊기게 재생
CallNotificationSystem.Instance.TriggerGlitchRingtone();
```

---

## 8. 자주 실수하는 것

| 증상 | 원인 | 해결 |
|------|------|------|
| 배너가 처음부터 보임 | `hiddenAnchoredY`가 화면 안쪽 값으로 설정됨 | Pivot Y=1 기준으로 양수 값(예: 150) 설정 |
| 수락 버튼 눌러도 반응 없음 | `CallNotificationSystem`이 씬에 없음 | 싱글톤 오브젝트 배치 확인 |
| E키로 폰이 안 열림 | `PlayerControlLock.VehicleSeated`가 true | 탑승 중이므로 정상. 하차 후 시도 |
| E키로 폰이 안 열림 (2) | `SetCanOpenPhone(false)` 호출됨 | 운전 시작 시 `false`, 하차 시 `true` 호출 확인 |
| 폰이 두 번 슬라이드됨 | `PhoneUIController`와 `PhoneManager` 동시 활성 | 하나만 사용. `PhoneUIController` 비활성화 권장 |
| 네비 화살표가 항상 위를 가리킴 | `playerTransform` 미연결 + Camera.main 없음 | `playerTransform` 연결 또는 씬에 `MainCamera` 태그 확인 |
| 독백이 안 나옴 | `driverMonologues` 배열이 비어 있음 | Inspector에서 일차별 텍스트 입력 또는 ClientDefinition.driverMonologue 입력 |
| 픽업 네비가 바로 안 시작됨 | `AfterClientSpawnComplete` 미구독 | 스폰 완료 이벤트 구독 후 `SetPickupMode` 호출 |

---

## 9. PhoneCallApp UI 상세 배치 (스크린샷 기준)

아래 레이아웃은 실제 UI 디자인을 기준으로 작성한 Hierarchy 구조와 각 GameObject의
Inspector 설정값입니다. **UI는 이미 제작된 상태**이고, 스크립트는 텍스트/오브젝트 참조만 받습니다.

---

### 9-1. Hierarchy 전체 구조

```
PhonePanel                          ← PhoneManager가 슬라이드 제어하는 루트
│   RectTransform
│   Width: 360  Height: 740         ← 전화기 패널 크기 (디자인에 맞게 조정)
│   Anchor: Bottom Center
│   Pivot: (0.5, 0)
│
└─ PhoneCallApp                     ← [PhoneCallApp 컴포넌트]
    │   Image (배경: 짙은 남색 #0D1117)
    │
    ├─ Header                       ← 상단 타이틀 바
    │   ├─ BackButton               ← Button (← 아이콘)
    │   ├─ TitleText                ← TMP_Text  "CALL RECEIVED"
    │   │   Font: 고딕/Bold  Size: 14  색: #FF6B35 (주황)
    │   └─ MenuButton               ← Button (… 아이콘, 선택)
    │
    ├─ PassengerCard                ← 손님 정보 카드 (테두리: 주황 #FF6B35)
    │   │   Image (RoundedRect, 테두리 1px 주황)
    │   │   Padding: 16px 사방
    │   │
    │   ├─ SubtitleText             ← TMP_Text  "NEW PASSENGER"
    │   │   Font: Bold  Size: 11  색: #FF6B35  LetterSpacing: 2
    │   │
    │   ├─ ClientNameText           ← TMP_Text  "손님 #3"          ★ 스크립트 연결
    │   │   Font: Bold  Size: 28  색: #FFFFFF
    │   │
    │   ├─ PickupAddressFullText    ← TMP_Text  "픽업 위치 B구역 · ..."  ★ 스크립트 연결
    │   │   Font: Regular  Size: 13  색: #AAAAAA
    │   │   Prefix "픽업 위치 " 포함해서 입력하거나 별도 Label 분리 가능
    │   │
    │   └─ FareRow                  ← Horizontal Layout Group
    │       ├─ FareLabel            ← TMP_Text  "예상 요금"  Size: 12  색: #888888
    │       └─ EstimatedFareText    ← TMP_Text  "₩18,000 ~"         ★ 스크립트 연결
    │           Font: Bold  Size: 14  색: #FF6B35
    │
    ├─ RouteSection                 ← 경로 카드 (현재 위치 → 픽업 위치)
    │   │   Image (배경: #1A2030, 모서리 8px)
    │   │   Padding: 16px
    │   │
    │   ├─ CurrentRow               ← Horizontal Layout Group
    │   │   ├─ RedDot               ← Image  Width:8 Height:8  색: #FF4444 (원형)
    │   │   ├─ CurrentLabel         ← TMP_Text  "현재 위치"  Size: 11  색: #888888
    │   │   └─ CurrentAreaText      ← TMP_Text  "A구역 · 도심 중심부"  ★ 스크립트 연결
    │   │       Font: Bold  Size: 13  색: #FFFFFF
    │   │
    │   ├─ RouteLine                ← Image  Width:1 Height:20  색: #444444 (수직선)
    │   │
    │   ├─ PickupRow                ← Horizontal Layout Group
    │   │   ├─ BlueDot              ← Image  Width:8 Height:8  색: #4488FF (원형)
    │   │   ├─ PickupLabel          ← TMP_Text  "픽업 위치"  Size: 11  색: #888888
    │   │   └─ PickupAddressShortText ← TMP_Text  "지하주차장 B2"    ★ 스크립트 연결
    │   │       Font: Bold  Size: 13  색: #FFFFFF
    │   │
    │   └─ WalkingTimeText          ← TMP_Text  "도보 약 2분"        ★ 스크립트 연결
    │       Font: Regular  Size: 11  색: #888888
    │
    ├─ MonologueBox                 ← 독백 박스 (시작 시 비활성)     ★ 스크립트 연결
    │   │   Image (배경: #1A2030, 테두리 없음)
    │   │   Padding: 14px  ContentSizeFitter: Vertical
    │   │
    │   └─ MonologueText            ← TMP_Text                      ★ 스크립트 연결
    │       "오늘은 꼭 많이 벌어야 하는데..."
    │       Font: Italic  Size: 12  색: #AAAAAA  Alignment: Center
    │
    ├─ ButtonRow                    ← Horizontal Layout Group  Spacing: 12
    │   ├─ AcceptButton             ← Button                        ★ 스크립트 연결
    │   │   │   Image 색: #FF6B35  CornerRadius: 8px
    │   │   │   Width: 200  Height: 56
    │   │   └─ Text (TMP)  "수락"  Font: Bold  Size: 18  색: #FFFFFF
    │   │
    │   └─ RejectButton             ← Button                        ★ 스크립트 연결
    │       │   Image 색: #2A3040  CornerRadius: 8px
    │       │   Width: 120  Height: 56
    │       └─ Text (TMP)  "거절"  Font: Regular  Size: 16  색: #888888
    │
    └─ AcceptedFeedback             ← 배차 완료 피드백 (시작 시 비활성)  ★ 스크립트 연결
        Image (배경: #1A3A1A 초록빛)  Width: 전체  Height: 56
        └─ Text (TMP)  "배차 완료"  Font: Bold  Size: 16  색: #44FF88
```

> ★ 표시된 오브젝트는 반드시 `PhoneCallApp` Inspector에 연결해야 합니다.

---

### 9-2. PhoneCallApp Inspector 연결표

`PhoneCallApp` 컴포넌트가 붙은 `PhoneCallApp` GameObject를 선택하면 아래 필드가 보입니다.

| Inspector 필드 | 연결할 오브젝트 | 타입 |
|----------------|-----------------|------|
| **App Root** | `PhoneCallApp` 자기 자신 | `GameObject` |
| **Client Name Text** | `ClientNameText` | `TMP_Text` |
| **Pickup Address Full Text** | `PickupAddressFullText` | `TMP_Text` |
| **Estimated Fare Text** | `EstimatedFareText` | `TMP_Text` |
| **Fare Format** | `₩{0:N0} ~` (그대로) | `string` |
| **Current Area Text** | `CurrentAreaText` | `TMP_Text` |
| **Pickup Address Short Text** | `PickupAddressShortText` | `TMP_Text` |
| **Walking Time Text** | `WalkingTimeText` | `TMP_Text` |
| **Walking Time Format** | `도보 {0}` (그대로) | `string` |
| **Monologue Text** | `MonologueText` | `TMP_Text` |
| **Monologue Box** | `MonologueBox` | `GameObject` |
| **Accept Button** | `AcceptButton` | `Button` |
| **Reject Button** | `RejectButton` | `Button` |
| **Accepted Feedback Object** | `AcceptedFeedback` | `GameObject` |
| **Badge Object** | `BadgeDot` (앱 아이콘 뱃지) | `GameObject` |

> **주의**: `Accept Button`과 `Reject Button`의 Inspector `OnClick()` 이벤트에는  
> 아무것도 추가하지 마세요. 스크립트 `Awake()`에서 자동 연결됩니다.

---

### 9-3. ClientDefinition ScriptableObject 입력값 예시

손님 `D1_A`(`ClientDefinition_D1_A.asset`) 기준 예시입니다.

| 필드 | 입력값 예시 |
|------|-------------|
| Client Id | `D1_A` |
| **Display Name** | `손님 #1` |
| **Pickup Address Full** | `B구역 · 동양아파트 지하주차장 B2` |
| **Pickup Address Short** | `지하주차장 B2` |
| **Estimated Fare Won** | `18000` |
| **Walking Time Label** | `약 2분` |
| **Driver Monologue** | `오늘은 꼭 많이 벌어야 하는데.. 이번달 관리비가 아직 남겼어.` |

이 값들은 `CallNotificationSystem.ReceiveCall(def)` 호출 시  
`PhoneCallApp.SetCallInfo(def)`로 자동 주입됩니다.

---

### 9-4. 색상 팔레트 (참고)

| 용도 | Hex |
|------|-----|
| 배경 (메인) | `#0D1117` |
| 카드 배경 | `#1A2030` |
| 강조색 (주황) | `#FF6B35` |
| 수락 버튼 | `#FF6B35` |
| 거절 버튼 배경 | `#2A3040` |
| 텍스트 (흰색) | `#FFFFFF` |
| 텍스트 (회색) | `#AAAAAA` |
| 텍스트 (어두운 회색) | `#888888` |
| 현재 위치 점 | `#FF4444` |
| 픽업 위치 점 | `#4488FF` |
