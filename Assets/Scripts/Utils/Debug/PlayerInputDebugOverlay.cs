using NightDriver.Character;
using NightDriver.Dialogue;
using NightDriver.UI;
using UnityEngine;

namespace NightDriver.DebugTools
{
    [AddComponentMenu("NightDriver/Debug/Player Input Debug Overlay")]
    public sealed class PlayerInputDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool show = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            string msg =
                $"dt: {Time.deltaTime:0.0000}  timeScale: {Time.timeScale:0.00}\n" +
                $"Horizontal: {h:0.00}  Vertical: {v:0.00}\n" +
                $"PhoneVisible: {PhoneUIController.IsAnyPhoneVisible}\n" +
                $"DialogueRunning: {(DialogueService.Instance != null && DialogueService.Instance.IsRunning)}\n" +
                $"VehicleSeated: {PlayerControlLock.VehicleSeated}\n";

            GUI.color = Color.black;
            GUI.Label(new Rect(14, 14, 520, 110), msg);
            GUI.color = Color.white;
            GUI.Label(new Rect(12, 12, 520, 110), msg);
        }
    }
}

