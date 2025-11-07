public interface ICollectable
{
    void Collect(Player player);
    void OnCollectedEffect();
    string GetCollectType();
}
