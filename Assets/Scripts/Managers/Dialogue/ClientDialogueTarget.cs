using UnityEngine;

namespace NightDriver.Dialogue
{
    /// <summary>
    /// 손님(클라이언트) 오브젝트에 붙여서,
    /// "이 손님의 Yarn 시작 노드"와 "외형/ID" 같은 식별 정보를 제공합니다.
    /// </summary>
    public sealed class ClientDialogueTarget : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string clientId;

        [Header("Yarn")]
        [Tooltip("이 손님과 상호작용 시 시작할 Yarn 노드 이름")]
        [SerializeField] private string startNode = "Start";

        public string ClientId => clientId;
        public string StartNode => startNode;

        public void Configure(string newClientId, string newStartNode)
        {
            if (!string.IsNullOrWhiteSpace(newClientId)) clientId = newClientId;
            if (!string.IsNullOrWhiteSpace(newStartNode)) startNode = newStartNode;
        }
    }
}

