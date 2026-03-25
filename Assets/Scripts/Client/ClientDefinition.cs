using UnityEngine;

namespace NightDriver.Client
{
    [CreateAssetMenu(menuName = "NightDriver/Client/Client Definition", fileName = "ClientDefinition_")]
    public sealed class ClientDefinition : ScriptableObject
    {
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

        [Header("Yarn")]
        [Tooltip("이 손님의 대화를 시작할 Yarn 노드")]
        public string startNode = "Start";

    }
}

