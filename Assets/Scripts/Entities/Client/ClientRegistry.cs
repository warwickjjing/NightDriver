using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// "이번 손님(현재 콜의 손님)" 단일 참조를 유지합니다.
    /// 상호작용/대화/내비 등 다른 시스템이 여기만 보면 됩니다.
    /// </summary>
    public static class ClientRegistry
    {
        public static GameObject CurrentClientObject { get; private set; }

        /// <summary>현재 콜의 <see cref="ClientDefinition.clientId"/>. 없으면 빈 문자열.</summary>
        public static string CurrentClientId { get; private set; } = string.Empty;

        public static void SetCurrent(GameObject client, string clientId = null)
        {
            CurrentClientObject = client;
            CurrentClientId = clientId ?? string.Empty;
        }

        public static void ClearIfCurrent(GameObject client)
        {
            if (CurrentClientObject != client)
                return;
            CurrentClientObject = null;
            CurrentClientId = string.Empty;
        }
    }
}

