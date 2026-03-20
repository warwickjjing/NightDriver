using UnityEngine;

namespace NightDriver.Character
{
    /// <summary>
    /// 캐릭터 이동/물리 관련 설정값을 보관하는 ScriptableObject.
    ///
    /// FirstPersonMover에 할당하면 여러 캐릭터가 같은 설정을 공유하거나,
    /// 다른 설정을 각각 가질 수 있습니다.
    ///
    /// 생성: Project 창 우클릭 → Create → NightDriver/Config/Character Config
    /// </summary>
    [CreateAssetMenu(menuName = "NightDriver/Config/Character Config", fileName = "CharacterConfig")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [Header("이동")]
        [Tooltip("기본 이동 속도 (m/s)")]
        public float moveSpeed = 5f;

        [Tooltip("달리기 배율")]
        public float sprintMultiplier = 1.6f;

        [Tooltip("달리기 키")]
        public KeyCode sprintKey = KeyCode.LeftShift;

        [Header("중력")]
        [Tooltip("중력 가속도 (음수 권장)")]
        public float gravity = -20f;

        [Tooltip("지면 접촉 시 수직 속도 고정값 (음수 소수)")]
        public float groundedStickVelocity = -2f;

        [Header("CharacterController 기본값")]
        [Tooltip("캡슐 콜라이더 높이")]
        public float controllerHeight = 2.0f;

        [Tooltip("캡슐 콜라이더 반지름")]
        public float controllerRadius = 0.35f;

        [Tooltip("캡슐 콜라이더 중심 오프셋")]
        public Vector3 controllerCenter = new Vector3(0f, 1.0f, 0f);

        [Tooltip("경사면 한계 각도")]
        public float slopeLimit = 45f;

        [Tooltip("계단 오프셋")]
        public float stepOffset = 0.3f;
    }
}
