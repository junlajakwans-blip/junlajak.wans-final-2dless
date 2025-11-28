public interface IInteractable
{
    void Interact(Player player);
    bool CanInteract { get; }
    void ShowPrompt();
    void HidePrompt();
}
