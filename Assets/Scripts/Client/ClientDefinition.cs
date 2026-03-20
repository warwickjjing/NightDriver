using UnityEngine;

namespace NightDriver.Client
{
    [CreateAssetMenu(menuName = "NightDriver/Client/Client Definition", fileName = "ClientDefinition_")]
    public sealed class ClientDefinition : ScriptableObject
    {
        [System.Serializable]
        public sealed class DestinationOption
        {
            public string optionId;
            public string displayName;
            [Tooltip("이 선택지가 선택되었을 때 이동할 목적지 ID(씬의 DestinationSet에서 찾음)")]
            public string destinationId;
            [Tooltip("이 선택지를 눌렀을 때 진행할 Yarn 노드(필요하면)")]
            public string nextNode;
        }

        [Header("Identity")]
        public string clientId;

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Spawn")]
        [Tooltip("이 손님의 스폰 위치 ID(씬의 SpawnPointSet에서 찾음)")]
        public string spawnPointId;

        [Header("Yarn")]
        [Tooltip("이 손님의 대화를 시작할 Yarn 노드")]
        public string startNode = "Start";

        [Header("Destination Choices (fixed per client)")]
        public DestinationOption[] destinationOptions = System.Array.Empty<DestinationOption>();
    }
}

