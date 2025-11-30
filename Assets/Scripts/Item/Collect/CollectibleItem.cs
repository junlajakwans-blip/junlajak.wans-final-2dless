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

    private void OnEnable()
    {
        // คืน Collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // คืน SpriteRenderer เฉพาะ CardPickup ด้วย (เพื่อกันบาง prefab ปิด sprite)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }


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
        // ตรวจสอบว่า Collider นี้ยังเปิดอยู่ (ป้องกันการชนซ้ำหากมี Magnet ด้วย)
        if (!GetComponent<Collider2D>().enabled) return; 

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

        // ปิด Collider/Renderer ทันทีที่เก็บ
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null) 
        {
            // ปิด Collider ป้องกันการชนซ้ำ
            myCollider.enabled = false; 
        }
        
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(); 
            if (_type != CollectibleType.CardPickup)
            {
                if (sr != null) sr.enabled = false;
            }
                
        Debug.Log($"[Collectible] Player collected: {_type} ({_itemID})");
        
        // 2. Apply Effect (ซึ่งจะเรียก player.AddCoin())
        ApplyEffect(player);
        
        // 3. เรียก OnCollectedEffect เพื่อ Despawn
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
        if (TryGetComponent<CardUI>(out var cardUI))
        {
            cardUI.SetCareerData(data);
        }
        else
        {
            Debug.LogWarning("[CardPickup] CardUI component missing on prefab. Visual will not show.");
        }
        Debug.Log($"[CardPickup] Assigned career data: {(data == null ? "NULL" : (string.IsNullOrWhiteSpace(data.DisplayName) ? data.CareerID.ToString() : data.DisplayName))}");
    }

    // Card Pickup 
    private void DropCareerCard()
    {
        if (_cardManagerRef == null)
        {
            Debug.LogWarning("[CardPickup] ❌ CardManager not found! Pickup ignored.");
            return;
        }

        // เลือก career ที่จะใช้จริง
        DuckCareerData finalCareer = _careerToDisplay;

        // ถ้ายังไม่มี หรือเป็น Muscle → ใช้ random จาก CardManager แทน
        if (finalCareer == null || finalCareer.CareerID == DuckCareer.Muscle)
        {
            Debug.Log("[CardPickup] Fallback → use random career from CardManager.");
            finalCareer = _cardManagerRef.GetRandomCareerForDrop();
        }

        if (finalCareer == null)
        {
            Debug.LogWarning("[CardPickup] ❌ finalCareer is NULL → cannot add card.");
            return;
        }

        // เพิ่มการ์ดเข้ามือทันที
        Debug.Log($"[CardPickup] Collect career: {(string.IsNullOrWhiteSpace(finalCareer.DisplayName) ? finalCareer.CareerID.ToString() : finalCareer.DisplayName)}");
        _cardManagerRef.AddCareerCard_FromDrop(finalCareer);

        // บังคับให้ UI อัพเดต (ใช้ method ที่มีอยู่แล้วใน CardManager)
        _cardManagerRef.ForceUIRefresh();

        // คืนเข้าพูล / ลบ object ในฉาก
        OnCollectedEffect();
    }

    private IEnumerator ShowCardThenCollect_Fallback()
    {
        yield return new WaitForSeconds(0.6f);

        // ใช้ระบบสุ่มปกติ
        _cardManagerRef.AddCareerCard();

        OnCollectedEffect();
    }

#endregion
}

