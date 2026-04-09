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

    }
}

