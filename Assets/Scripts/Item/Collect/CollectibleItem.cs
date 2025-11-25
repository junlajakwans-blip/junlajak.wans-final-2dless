using UnityEngine;
using System.Collections;

/// <summary>
/// CollectibleItem – Used for Coin, Token, Buff Item, and Card Pickup.
/// This item now delegates all time-based effects to the BuffManager.
/// </summary>
public enum CollectibleType
{
    Coin,
    Token,
    GreenTea,
    Coffee,
    MooKrata,
    Takoyaki,
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
    [SerializeField] private float _buffDuration = 5f; // Used for Coffee / MooKrata
    [SerializeField] private int _healAmount = 30;     // Coffee
    [SerializeField] private int _smallHeal = 10;      // GreenTea
    

    private CardManager _cardManagerRef;
    private CollectibleSpawner _spawnerRef; //use for return pool cause it know tag
    private BuffManager _buffManagerRef; 
    
    public CollectibleType GetCollectibleType() => _type;


#region Dependencies

    public void SetDependencies(CardManager manager, CollectibleSpawner spawner, BuffManager buffManager)
    {
        _cardManagerRef = manager;
        _spawnerRef = spawner;
        this._buffManagerRef = buffManager;
    }

#endregion

    #region Trigger Event

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<Player>();
        if (player == null) return;
        
        // เรียก Collect() ทันทีที่ชน
        Collect(player);
            
    }

    #endregion

#region  Collect


    public void Collect(Player player)
    {
        if (player == null) return;

        Debug.Log($"[Collectible] Player collected: {_type} ({_itemID})");
        ApplyEffect(player);
        OnCollectedEffect();
    }

    public void OnCollectedEffect()
    {
        if (_spawnerRef != null)
        {
            //return to Pool pass Spawner (Spawner call ObjectPoolManager)
            _spawnerRef.Despawn(gameObject); 
        }
        else
        {
            // Fallback
            Destroy(gameObject); 
        }
    }
    public string GetCollectType() => _type.ToString();

    private void ApplyEffect(Player player)
 {
    // [NEW FIX 2]: ตรวจสอบความปลอดภัยตามหลัก DI
    // เปลี่ยนการตรวจสอบความปลอดภัยให้เป็นไปตาม Reference ที่ถูก Inject
    if (_type == CollectibleType.Coffee || _type == CollectibleType.MooKrata || _type == CollectibleType.Takoyaki)
    {
        // OLD: if (BuffManager.Instance == null) // <<< เราต้องการลบการพึ่งพา Instance
        if (_buffManagerRef == null) // <<< FIX: ตรวจสอบ Reference ที่เรามีอยู่
        {
            Debug.LogWarning("[Collectible] BuffManager NOT INJECTED! Timed buff will not apply.");
            return;
        }
    }
        
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

            // Tea → Heal +10 HP (Instant effect)
            case CollectibleType.GreenTea:
                player.Heal(_smallHeal);
                Debug.Log($"[Buff] Green Tea: +{_smallHeal} HP");
                break;

            // Coffee → Heal +30 HP 5 s (Timed effect - delegated)
            case CollectibleType.Coffee:
                // DELEGATE: Send command to BuffManager to run the routine
                _buffManagerRef.ApplyCollectibleBuff(_type, player, _healAmount, _buffDuration);
                break;

            // Mookrata → Enemy No Attack 5 s (Timed effect - delegated)
            case CollectibleType.MooKrata:
                // DELEGATE: Send command to BuffManager to run the routine
                _buffManagerRef.ApplyCollectibleBuff(_type, player, 0, _buffDuration);
                break;

            // Takoyaki → HOT then COOL (Timed effect - delegated) || Hot -> Damage | Cool -> Heal
            case CollectibleType.Takoyaki:
                // ใช้ _healAmount เป็นค่า Damage/Heal ใน TakoyakiRoutine
                _buffManagerRef.ApplyCollectibleBuff(_type, player, _healAmount, _buffDuration); 
            break;

            // Card Pickup (Instant effect - delegated to CardManager)
            case CollectibleType.CardPickup:
                DropCareerCard();
                break;
        }
    }

    private DuckCareerData _careerToDisplay;

    public void AssignCareer(DuckCareerData data)
    {
        _careerToDisplay = data;
        GetComponent<CardUI>()?.SetCareerData(data);
    }

    // Card Pickup 
    private void DropCareerCard()
    {
        CardManager manager = _cardManagerRef;
        if (manager == null)
        {
            Debug.LogWarning("[CardPickup] CardManager not found!");
            return;
        }

        manager.AddCareerCard();
        Debug.Log("[CardPickup] Added card to hand: " + _careerToDisplay?.DisplayName);
    }


#endregion
}