using System;
using System.Collections;
using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 손님 NPC의 하차 후 이동(걸어감) 연출을 담당합니다.
    /// Animator가 있으면 이동 중 파라미터를 켜고, 도착 시 끕니다.
    /// </summary>
    public sealed class ClientNPC : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float arriveDistance = 0.15f;

        [Header("Animator (Optional)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string walkAnimBool = "IsWalking";

        public event Action OnWalkOffComplete;

        private Coroutine walkRoutine;

        /// <summary>
        /// 지정한 지점까지 이동한 뒤 자신을 제거합니다.
        /// </summary>
        public void WalkOff(Transform exitPoint)
        {
            if (walkRoutine != null)
                StopCoroutine(walkRoutine);

            walkRoutine = StartCoroutine(WalkOffRoutine(exitPoint));
        }

        private IEnumerator WalkOffRoutine(Transform exitPoint)
        {
            if (exitPoint == null)
            {
                CompleteAndDestroy();
                yield break;
            }

            SetWalking(true);

            while (true)
            {
                Vector3 toTarget = exitPoint.position - transform.position;
                float sqrDist = toTarget.sqrMagnitude;
                if (sqrDist <= arriveDistance * arriveDistance)
                    break;

                Vector3 dir = toTarget.normalized;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(dir);
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    exitPoint.position,
                    walkSpeed * Time.deltaTime);

                yield return null;
            }

            SetWalking(false);
            CompleteAndDestroy();
        }

        private void SetWalking(bool value)
        {
            if (animator == null || string.IsNullOrWhiteSpace(walkAnimBool)) return;
            animator.SetBool(walkAnimBool, value);
        }

        private void CompleteAndDestroy()
        {
            OnWalkOffComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
