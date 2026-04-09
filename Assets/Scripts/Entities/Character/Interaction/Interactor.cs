using UnityEngine;

namespace NightDriver.Character.Interaction
{
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] private float radius = 2.0f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        private readonly Collider[] buffer = new Collider[16];

        public IInteractable FindBest()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, interactableLayers, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                var col = buffer[i];
                if (col == null) continue;

                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;
                if (!interactable.CanInteract(gameObject)) continue;

                var d = (col.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = interactable;
                }
            }

            return best;
        }

        public bool TryInteract()
        {
            var target = FindBest();
            if (target == null) return false;
            target.Interact(gameObject);
            return true;
        }
    }
}
