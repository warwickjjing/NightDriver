using UnityEngine;
using UnityEngine.UI;

namespace NightDriver.UI
{
    /// <summary>
    /// Each click toggles a particle hierarchy on or off: plays all <see cref="ParticleSystem"/> when on,
    /// stops with clear and deactivates the root when off. Works with a prefab asset or a scene instance.
    /// </summary>
    public sealed class ParticleEffectUIButton : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Button that toggles the particle effect on/off each click.")]
        [SerializeField] private Button triggerButton;

        [Header("Effect")]
        [Tooltip("Prefab asset (instantiated once under spawnParent) or an object already in the scene.")]
        [SerializeField] private GameObject particleRoot;

        [Tooltip("Parent for instantiated prefab. If null, the instance is created at the root of the scene.")]
        [SerializeField] private Transform spawnParent;

        [Tooltip("When turning on, clears particle state before Play so playback starts from the beginning.")]
        [SerializeField] private bool replayFromStart = true;

        private GameObject _runtimeInstance;
        private bool _effectOn;

        private void Awake()
        {
            if (particleRoot == null)
                return;

            if (particleRoot.scene.IsValid())
            {
                _runtimeInstance = particleRoot;
                _effectOn = particleRoot.activeSelf;
            }
        }

        private void OnEnable()
        {
            if (triggerButton != null)
                triggerButton.onClick.AddListener(ToggleParticleEffect);
        }

        private void OnDisable()
        {
            if (triggerButton != null)
                triggerButton.onClick.RemoveListener(ToggleParticleEffect);
        }

        /// <summary>Wire from a Button OnClick in the Inspector (works without assigning <see cref="triggerButton"/>).</summary>
        public void ToggleParticleEffect()
        {
            if (particleRoot == null)
                return;

            _effectOn = !_effectOn;

            if (!_effectOn)
            {
                if (_runtimeInstance != null)
                    StopAndHide(_runtimeInstance);
                return;
            }

            var root = EnsureInstance();
            if (root == null)
                return;

            root.SetActive(true);

            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (replayFromStart)
                    ps.Clear(true);
                ps.Play(true);
            }
        }

        /// <summary>Previous name; still toggles on each call.</summary>
        public void PlayParticleEffect() => ToggleParticleEffect();

        private static void StopAndHide(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            root.SetActive(false);
        }

        private GameObject EnsureInstance()
        {
            if (_runtimeInstance != null)
                return _runtimeInstance;

            if (particleRoot.scene.IsValid())
            {
                _runtimeInstance = particleRoot;
                return _runtimeInstance;
            }

            _runtimeInstance = spawnParent != null
                ? Instantiate(particleRoot, spawnParent, false)
                : Instantiate(particleRoot);

            return _runtimeInstance;
        }
    }
}
