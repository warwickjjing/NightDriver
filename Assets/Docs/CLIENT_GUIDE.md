# 클라이언트 생성 가이드

이 문서는 NightDriver의 손님 생성/대화/목적지/하차 플로우를 빠르게 세팅하기 위한 개발자 가이드입니다.

## 1) 클라이언트 Prefab 생성 및 컴포넌트 설정

클라이언트 Prefab 루트(또는 자식)에 아래 컴포넌트를 붙입니다.

- `InteractionPrompt`
  - 손님과의 첫 대화 시작 프롬프트 (`말걸기 [E]`)
  - `ClientSpawner`가 `SetYarnNode(startNode)`로 자동 주입
- `ClientBehaviour`
  - 목적지 목록, 도착 감지, 내리기 프롬프트, 하차 처리 담당
- `ClientNPC`
  - 하차 후 `exitPoint`까지 걸어가고 사라지는 연출 담당

## 2) 목적지 Transform 설정 (Prefab의 ClientBehaviour)

`ClientBehaviour.destinations[]`에 목적지 엔트리를 추가합니다. 최소 1개 이상 필요합니다.

각 엔트리 필드:

- `id`: Yarn에서 `<<setDestination id>>`로 넘길 식별자
- `location`: 실제 목적지 Transform (씬 오브젝트 참조)
- `exitPoint`: 손님이 하차 후 걸어갈 목표 지점 Transform
- `arrivalYarnNode`: 도착 후 실행할 Yarn 노드 (비워두면 대화 없이 즉시 하차)

예시:

- `id = Destination_Building`
- `location = BuildingDropPoint`
- `exitPoint = BuildingExitPoint`
- `arrivalYarnNode = Destination_Building`

## 3) ClientDefinition / WeekSchedule 등록

`ClientDefinition`에 아래를 설정합니다.

- `clientId`: 예) `D1_A`
- `prefab`: 클라이언트 Prefab
- `vehiclePrefab`: 동시 스폰할 차량 Prefab (없으면 `None`)
- `spawnPointId`: `SpawnPointSet`의 id와 일치
- `startNode`: 첫 대화 노드 (예: `D1_A`)

그 다음 `WeekSchedule`에서 일차/콜 순서에 맞게 `ClientDefinition`을 연결합니다.

## 4) Yarn 대사 작성 규칙

첫 대화 노드에서 목적지 선택 시 `jump` 대신 `setDestination`을 사용합니다.

예시:

```
-> xx빌딩으로 간다
    <<setDestination Destination_Building>>
-> 병원으로 간다
    <<setDestination Destination_Hospital>>
```

목적지 도착 대화는 `ClientBehaviour.destinations[].arrivalYarnNode`에 지정한 노드에서 처리합니다.

## 5) 차량 Prefab 동시 리스폰

`ClientSpawner`는 현재 클라이언트 스폰 시점에 `vehiclePrefab`도 같은 위치/회전으로 함께 스폰합니다.
다음 손님 스폰 또는 디스폰 시 차량도 함께 정리됩니다.

## 6) 전체 플로우 체크리스트

1. `ClientSpawner`가 손님 + 차량 스폰
2. HUD가 손님 위치를 가리킴
3. 플레이어가 손님 근처 도착 시 HUD 자동 숨김
4. `말걸기 [E]`로 첫 대화 시작 (`startNode`)
5. Yarn에서 `<<setDestination ...>>` 호출
6. HUD가 목적지로 전환
7. 목적지 근처에서 `내리기 [E]` 표시
8. 입력 시 도착 대화(선택) 후 손님 하차/보행/소멸
9. 하차 완료 이벤트로 다음 콜 진행

## 7) 자주 발생하는 실수

- `setDestination`의 id와 `ClientBehaviour.destinations[].id` 불일치
- `location`/`exitPoint` 미할당
- `ClientNPC` 컴포넌트 누락 (하차 이동 미동작)
- `ClientDefinition.startNode`와 Yarn 노드 title 불일치
