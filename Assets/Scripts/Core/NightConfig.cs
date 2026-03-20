using UnityEngine;

namespace NightDriver.Core
{
    /// <summary>
    /// 게임 규칙 관련 설정값을 보관하는 ScriptableObject.
    ///
    /// NightManager에 할당하면 코드 수정 없이 Inspector에서 규칙을 조정할 수 있습니다.
    /// 비워두면 NightManager의 인스펙터 기본값을 사용합니다.
    ///
    /// 생성: Project 창 우클릭 → Create → NightDriver/Config/Night Config
    /// </summary>
    [CreateAssetMenu(menuName = "NightDriver/Config/Night Config", fileName = "NightConfig")]
    public sealed class NightConfig : ScriptableObject
    {
        [Header("Night Rules")]
        [Tooltip("하룻밤에 처리해야 하는 콜 수")]
        [Range(1, 20)] public int callsPerNight = 2;

        [Tooltip("플레이 가능한 최대 일차")]
        [Range(1, 7)] public int maxDay = 7;

        [Header("Start State")]
        [Tooltip("게임 시작 시 기본 일차")]
        [Range(1, 7)] public int startDay = 1;
    }
}
