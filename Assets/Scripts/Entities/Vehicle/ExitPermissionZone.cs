using UnityEngine;

namespace NightDriver.Vehicle
{
    /// <summary>
    /// 특정 구역에 진입하면 하차 가능하도록 토글하는 트리거.
    /// - BoxCollider(Trigger) 등에 붙여 사용
    /// - 목적지 도착 판정(Zone)으로도 재사용 가능
    /// </summary>
    [AddComponentMenu("NightDriver/Vehicle/Exit Permission Zone")]
    public sealed class ExitPermissionZone : MonoBehaviour
    {
        [SerializeField] private VehicleSeatInteraction targetVehicle;
        [SerializeField] private bool canExitWhileInside = true;

        private void Reset()
        {
            var collider = GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (targetVehicle == null) return;
            if (!other.CompareTag("Player")) return;
            targetVehicle.SetCanExit(canExitWhileInside);
        }

        private void OnTriggerExit(Collider other)
        {
            if (targetVehicle == null) return;
            if (!other.CompareTag("Player")) return;
            targetVehicle.SetCanExit(!canExitWhileInside);
        }
    }
}

