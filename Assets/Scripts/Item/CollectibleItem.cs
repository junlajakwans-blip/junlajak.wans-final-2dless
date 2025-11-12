using UnityEngine;
using System.Collections;

/// <summary>
/// CollectibleItem – ใช้สำหรับ Coin, Token, Buff Item, และ Card Pickup
/// (GoldenMon จะดรอปการ์ดอาชีพ 1 ใบตามเรต PDF)
/// </summary>
public enum CollectibleType
{
    Coin,
    Token,
    GreenTea,
    Coffee,
    MooKrata,
    CardPickup
}

[System.Serializable]
public class CollectibleItem : MonoBehaviour, ICollectable
{
    [Header("Collectible Settings")]
    [SerializeField] private string _itemID;
    [SerializeField] private CollectibleType _type;
    [SerializeField] private int _value = 1;
    [SerializeField] private Sprite _icon;

    [Header("Buff Settings")]
    [SerializeField] private float _buffDuration = 5f; // ใช้กับ Coffee / MooKrata
    [SerializeField] private int _healAmount = 30;     // Coffee
    [SerializeField] private int _smallHeal = 10;      // GreenTea

    public void Collect(Player player)
    {
        if (player == null) return;

        Debug.Log($"[Collectible] Player collected: {_type} ({_itemID})");
        ApplyEffect(player);
        OnCollectedEffect();
    }

    public void OnCollectedEffect() => Destroy(gameObject);
    public string GetCollectType() => _type.ToString();

    private void ApplyEffect(Player player)
    {
        switch (_type)
        {
            // Coin
            case CollectibleType.Coin:
                player.AddCoin(_value);
                Debug.Log($"[Collectible] +{_value} Coin");
                break;

            // TOKEN When Muscle Duck Kill GoldenMon
            case CollectibleType.Token:
                player.AddToken(_value);
                Debug.Log($"[Collectible] +{_value} Token");
                break;

            // Tea → Heal +10 HP
            case CollectibleType.GreenTea:
                player.Heal(_smallHeal);
                Debug.Log($"[Buff] Green Tea: +{_smallHeal} HP");
                break;

            // Coffee → Heal +30 HP 5 s
            case CollectibleType.Coffee:
                StartCoroutine(ApplyCoffeeBuff(player));
                break;

            // Mookrata → Enemy No Attack 5 s
            case CollectibleType.MooKrata:
                StartCoroutine(ApplyMooKrataBuff());
                break;

            // การ์ด (Drop Career per rate in CardManager)
            case CollectibleType.CardPickup:
                DropCareerCard();
                break;
        }
    }

    // Coffee Buff
    private IEnumerator ApplyCoffeeBuff(Player player)
    {
        int oldHP = player.CurrentHealth;
        player.Heal(_healAmount);
        Debug.Log($"[Buff] Coffee: Heal +{_healAmount} (Temporary)");

        yield return new WaitForSeconds(_buffDuration);

        if (!player.IsDead && player.CurrentHealth > oldHP)
        {
            int diff = player.CurrentHealth - oldHP;
            player.TakeDamage(diff);
            Debug.Log($"[Buff] Coffee expired → HP reverted to {oldHP}");
        }
    }

    // MooKrata Buff
    private IEnumerator ApplyMooKrataBuff()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            //TODO Enemy no attack 5 s
            //enemy.DisableBehavior(_buffDuration);

        Debug.Log($"[Buff] MooKrata: Disable enemies for {_buffDuration}s");
        yield return new WaitForSeconds(_buffDuration);
        Debug.Log("[Buff] MooKrata ended → Enemies resume attack");
    }

    // Card Pickup 
    private void DropCareerCard()
    {
        CardManager manager = FindFirstObjectByType<CardManager>();
        if (manager == null)
        {
            Debug.LogWarning("[CardPickup] CardManager not found!");
            return;
        }

        manager.AddCareerCard(); // Call fuction in CardManager
        Debug.Log("[CardPickup] Career Card dropped (via manager).");
    }
}
