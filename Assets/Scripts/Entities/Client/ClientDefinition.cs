using UnityEngine;

namespace NightDriver.Client
{
    [CreateAssetMenu(menuName = "NightDriver/Client/Client Definition", fileName = "ClientDefinition_")]
    public sealed class ClientDefinition : ScriptableObject
    {
        [System.Serializable]
        public sealed class DestinationRule
        {
            [Tooltip("Yarn의 <<setDestination id>> 와 매칭되는 목적지 ID")]
            public string destinationId;
            [Tooltip("도착 시 실행할 Yarn 노드 (비워두면 대화 없이 즉시 하차)")]
            public string arrivalYarnNode;
            [Tooltip("하차 후 손님이 걸어갈 ExitPoint ID (씬의 ExitPointSet에서 찾음). 비워두면 손님 프리팹의 설정을 사용합니다.")]
            public string exitPointId;
        }

        [Header("Identity")]
        public string clientId;

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Vehicle")]
        [Tooltip("손님과 함께 스폰할 차량 프리팹 (없으면 None)")]
        public GameObject vehiclePrefab;

        [Header("Spawn")]
        [Tooltip("이 손님의 스폰 위치 ID(씬의 SpawnPointSet에서 찾음)")]
        public string spawnPointId;

        [Header("Vehicle Spawn (Optional Override)")]
        [Tooltip("비우면 spawnPointId를 그대로 사용합니다. 차량 스폰 위치를 손님과 분리하려면 여기에 다른 SpawnPoint ID를 넣으세요.")]
        public string vehicleSpawnPointId;

        [Tooltip("차량을 spawnPoint에서 로컬로 얼마나 밀어 스폰할지. (0이면 포인트 그대로)")]
        public Vector3 vehicleSpawnLocalOffset = Vector3.zero;

        [Header("Yarn")]
        [Tooltip("이 손님의 대화를 시작할 Yarn 노드")]
        public string startNode = "Start";

        [Header("Destination Rules (Data-driven)")]
        [Tooltip("목적지별 도착 대화/하차 지점을 데이터로 관리합니다. 비워두면 손님 프리팹(ClientBehaviour)의 기존 Destinations 설정을 그대로 사용합니다.")]
        public DestinationRule[] destinationRules = System.Array.Empty<DestinationRule>();

        // ─── 콜 앱 UI 표시 정보 ───────────────────────────────────────────────

        [Header("콜 앱 — 손님 표시 정보")]
        [Tooltip("폰 콜 앱에 표시할 손님 이름. 비워두면 clientId를 사용합니다. (예: 손님 #3)")]
        public string displayName;

        [Tooltip("픽업 위치 전체 주소 (카드 상단에 표시). 예: B구역 · 동양아파트 지하주차장 B2")]
        public string pickupAddressFull;

        [Tooltip("픽업 위치 약식 (경로 섹션에 표시). 예: 지하주차장 B2")]
        public string pickupAddressShort;

        [Tooltip("예상 요금 (원). 표시 포맷은 PhoneCallApp에서 설정합니다. 예: 18000")]
        public int estimatedFareWon = 4800;

        [Tooltip("도보 소요 시간 문구. 예: 약 2분")]
        public string walkingTimeLabel = "약 2분";

        [Header("콜 앱 — 도현 독백 (일차별)")]
        [Tooltip("콜 수락 직후 도현이 내뱉는 독백. 일차에 무관하게 이 손님만의 독백을 넣을 때 사용합니다.\n비워두면 CallNotificationSystem의 driverMonologues 배열(일차 기준)을 사용합니다.")]
        [TextArea(2, 4)]
        public string driverMonologue;
    }
}

