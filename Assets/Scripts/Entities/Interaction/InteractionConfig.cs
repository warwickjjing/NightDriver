using UnityEngine;

namespace NightDriver.Interaction
{
    /// <summary>
    /// 상호작용 시스템의 설정값을 보관하는 ScriptableObject.
    ///
    /// InteractionPrompt에 할당하면 여러 NPC가 같은 설정을 공유하거나,
    /// NPC마다 다른 거리/키/UI를 설정할 수 있습니다.
    ///
    /// 생성: Project 창 우클릭 → Create → NightDriver/Config/Interaction Config
    /// </summary>
    [CreateAssetMenu(menuName = "NightDriver/Config/Interaction Config", fileName = "InteractionConfig")]
    public sealed class InteractionConfig : ScriptableObject
    {
        [Header("거리 감지")]
        [Tooltip("상호작용 가능 거리 (미터)")]
        public float interactionDistance = 3f;

        [Tooltip("플레이어를 탐색할 태그")]
        public string playerTag = "Player";

        [Header("입력")]
        [Tooltip("대화 시작 키")]
        public KeyCode interactKey = KeyCode.E;

        [Header("프롬프트 UI")]
        [Tooltip("기본 프롬프트 텍스트")]
        public string defaultPromptText = "말걸기 [E]";

        [Tooltip("NPC 기준 프롬프트 위치 오프셋 (로컬)")]
        public Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);

        [Tooltip("World Space Canvas 픽셀 크기")]
        public Vector2 canvasSize = new Vector2(220f, 52f);

        [Tooltip("World Space Canvas 배율 (작을수록 화면에서 작게 보임)")]
        public float canvasScale = 0.008f;
    }
}
