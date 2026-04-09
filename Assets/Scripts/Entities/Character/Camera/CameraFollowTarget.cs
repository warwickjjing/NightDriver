using UnityEngine;

namespace NightDriver.Character.Camera
{
    /// <summary>
    /// 카메라 리그를 플레이어 시야 기준 Transform에 따라가게 합니다.
    /// - 부모/자식으로 묶지 않고도 위치 추적 가능
    /// - 회전은 FirstPersonCamera가 담당하므로 여기서는 위치만 처리
    /// </summary>
    [AddComponentMenu("NightDriver/Character/Camera Follow Target")]
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("카메라가 따라갈 기준 Transform (예: PlayerRoot/ViewTarget)")]
        [SerializeField] private Transform followTarget;

        [Header("Follow")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.65f, 0f);
        [Tooltip("0이면 즉시 추적, 값이 클수록 부드럽게 추적")]
        [SerializeField] private float smoothTime = 0.04f;
        [SerializeField] private bool useUnscaledTime = false;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (followTarget == null) return;

            Vector3 targetPos = followTarget.position + worldOffset;
            if (smoothTime <= 0.0001f)
            {
                transform.position = targetPos;
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                dt);
        }

        public void SetTarget(Transform target)
        {
            followTarget = target;
        }
    }
}

