using UnityEngine;
using NightDriver.Character;
using NightDriver.Dialogue;
using NightDriver.UI;

namespace NightDriver.Character.Camera
{
    /// <summary>
    /// Legacy Input 기반 1인칭 카메라.
    /// - 마우스 X: 플레이어(Yaw)
    /// - 마우스 Y: 카메라(Pitch)
    /// </summary>
    public sealed class FirstPersonCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Yaw(좌우 회전)를 적용할 플레이어 루트 Transform")]
        [SerializeField] private Transform playerRoot;
        [Tooltip("Pitch(상하 회전)를 적용할 카메라 Transform (보통 Main Camera)")]
        [SerializeField] private Transform cameraPivot;

        [Header("Sensitivity")]
        [SerializeField] private float sensitivityX = 2.0f;
        [SerializeField] private float sensitivityY = 2.0f;
        [SerializeField] private bool invertY = false;

        [Header("Yaw Fallback")]
        [Tooltip("cameraPivot이 playerRoot의 자식이 아니면, 시야가 같이 돌도록 카메라에도 Yaw를 같이 적용합니다.")]
        [SerializeField] private bool applyYawToCameraWhenNotChild = true;
        
        [Header("Vehicle Seat Look")]
        [Tooltip("차량 탑승 중에는 몸(playerRoot)을 돌리지 않고 시야만 좌우로 둘러볼 수 있게 합니다.")]
        [SerializeField] private bool rotateViewOnlyWhenSeated = true;

        [Header("Clamp Pitch")]
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnEnable = true;
        [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

        [Header("Time")]
        [Tooltip("True면 Time.timeScale 영향을 받지 않음")]
        [SerializeField] private bool useUnscaledDeltaTime = false;

        private float pitch;
        private bool cursorLocked;

        /// <summary>차량 탑승 중 카메라 좌우(Yaw). localRotation을 pitch만 쓰면 매 프레임 yaw가 지워지므로 별도 보관.</summary>
        private float seatedYaw;
        private bool wasVehicleSeated;

        private void Reset()
        {
            playerRoot = transform;
            cameraPivot = GetComponentInChildren<UnityEngine.Camera>()?.transform;
        }

        private void OnEnable()
        {
            if (lockCursorOnEnable)
            {
                SetCursorLocked(true);
            }
        }

        private void Start()
        {
            if (cameraPivot != null)
            {
                pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
            }
        }

        private void Update()
        {
            // 폰 UI 열림 또는 대화 중에는 카메라 회전 입력을 잠급니다.
            if (PhoneUIController.IsAnyPhoneVisible
                || (DialogueService.Instance != null && DialogueService.Instance.IsRunning)
                )
            {
                return;
            }

            if (Input.GetKeyDown(toggleCursorKey))
            {
                SetCursorLocked(!cursorLocked);
            }

            if (!cursorLocked) return;

            float dt = useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX * dt * 60f;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY * (invertY ? 1f : -1f) * dt * 60f;

            bool seated = PlayerControlLock.VehicleSeated && rotateViewOnlyWhenSeated;

            // 탑승 직후: 좌석에 카메라가 identity로 붙으므로 시야 yaw는 0부터
            if (seated && !wasVehicleSeated)
                seatedYaw = 0f;
            wasVehicleSeated = seated;

            if (seated)
            {
                // 탑승 중: 몸은 돌리지 않고, 카메라에 pitch + yaw를 한 번에 적용 (Euler(pitch,0,0)만 쓰면 yaw가 매 프레임 삭제됨)
                seatedYaw += mouseX;
                pitch = Mathf.Clamp(pitch + mouseY, minPitch, maxPitch);
                if (cameraPivot != null)
                    cameraPivot.localRotation = Quaternion.Euler(pitch, seatedYaw, 0f);
            }
            else
            {
                if (playerRoot != null)
                    playerRoot.Rotate(Vector3.up, mouseX, Space.World);
                else if (cameraPivot != null)
                    cameraPivot.Rotate(Vector3.up, mouseX, Space.World);

                if (applyYawToCameraWhenNotChild && playerRoot != null && cameraPivot != null && !cameraPivot.IsChildOf(playerRoot))
                    cameraPivot.Rotate(Vector3.up, mouseX, Space.World);

                if (cameraPivot != null)
                {
                    pitch = Mathf.Clamp(pitch + mouseY, minPitch, maxPitch);
                    cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }

        public void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private static float NormalizePitch(float xDegrees)
        {
            // Unity Euler가 0~360으로 나오는 경우를 -180~180으로 정규화
            if (xDegrees > 180f) xDegrees -= 360f;
            return xDegrees;
        }
    }
}
