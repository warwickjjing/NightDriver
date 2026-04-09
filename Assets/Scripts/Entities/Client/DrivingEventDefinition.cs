using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 운전 중 위치·일차·손님 ID 조건으로 Yarn 노드를 한 번 실행합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "NightDriver/Client/Driving Event Definition", fileName = "DrivingEvent_")]
    public sealed class DrivingEventDefinition : ScriptableObject
    {
        [Tooltip("NightManager.State.dayIndex 와 일치할 때만")]
        [Range(1, 7)] public int dayIndex = 1;

        [Tooltip("비우면 어떤 손님이든. ClientDefinition.clientId 와 동일한 문자열")]
        public string requiredClientId = string.Empty;

        [Tooltip("구역 반경(미터)")]
        public float triggerRadius = 12f;

        [Tooltip("조건 충족 시 시작할 Yarn 노드")]
        public string yarnNodeName = string.Empty;
    }
}
