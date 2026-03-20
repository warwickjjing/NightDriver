using UnityEngine;

namespace NightDriver.Character.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
        string GetPrompt(GameObject interactor);
    }
}
