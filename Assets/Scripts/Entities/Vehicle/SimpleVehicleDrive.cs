using NightDriver.Character;
using NightDriver.Dialogue;
using NightDriver.UI;
using UnityEngine;

namespace NightDriver.Vehicle
{
    [AddComponentMenu("NightDriver/Vehicle/Simple Vehicle Drive")]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SimpleVehicleDrive : MonoBehaviour
    {
        [Header("Enable Condition")]
        [Tooltip("true면 플레이어가 탑승(VehicleSeated) 상태일 때만 입력을 받습니다.")]
        [SerializeField] private bool onlyWhenSeated = true;

        [Header("Input")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;
        [SerializeField] private KeyCode brakeKey = KeyCode.Space;

        [Header("Movement (Arcade)")]
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float maxSpeed = 22f;
        [SerializeField] private float steerDegreesPerSecond = 80f;
        [SerializeField] private float brakeDrag = 4f;
        [SerializeField] private float normalDrag = 0.15f;
        [SerializeField] private float uprightStabilize = 4f;

        [Header("Stability / Setup")]
        [Tooltip("차량의 실제 전방 기준. 비우면 이 오브젝트 transform.forward를 사용합니다.")]
        [SerializeField] private Transform forwardReference;
        [Tooltip("무게중심 오프셋. y를 음수로 두면 전복/곤두박질이 크게 줄어듭니다.")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
        [Tooltip("true면 X/Z 회전을 고정해서 아케이드처럼 안정화합니다.")]
        [SerializeField] private bool freezePitchAndRoll = true;

        private Rigidbody rb;
        private Vector3 originalCenterOfMass;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.drag = normalDrag;

            originalCenterOfMass = rb.centerOfMass;
            rb.centerOfMass = originalCenterOfMass + centerOfMassOffset;

            if (freezePitchAndRoll)
                rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (PhoneUIController.IsAnyPhoneVisible
                || (DialogueService.Instance != null && DialogueService.Instance.IsRunning))
                return;

            if (onlyWhenSeated && !PlayerControlLock.VehicleSeated)
                return;

            float throttle = (Input.GetKey(forwardKey) ? 1f : 0f) + (Input.GetKey(backKey) ? -1f : 0f);
            float steer = (Input.GetKey(rightKey) ? 1f : 0f) + (Input.GetKey(leftKey) ? -1f : 0f);

            // forward/back
            var vel = rb.velocity;
            var flatVel = new Vector3(vel.x, 0f, vel.z);
            var fwd = (forwardReference != null ? forwardReference.forward : transform.forward);
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

            if (flatVel.magnitude < maxSpeed || Mathf.Sign(throttle) != Mathf.Sign(Vector3.Dot(flatVel, fwd)))
            {
                rb.AddForce(fwd * (throttle * acceleration), ForceMode.Acceleration);
            }

            // steer (yaw)
            float yaw = steer * steerDegreesPerSecond * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            // brake
            bool braking = Input.GetKey(brakeKey);
            rb.drag = braking ? brakeDrag : normalDrag;

            // keep upright (optional)
            if (uprightStabilize > 0f)
            {
                var currentUp = transform.up;
                var torqueAxis = Vector3.Cross(currentUp, Vector3.up);
                rb.AddTorque(torqueAxis * uprightStabilize, ForceMode.Acceleration);
            }
        }
    }
}

