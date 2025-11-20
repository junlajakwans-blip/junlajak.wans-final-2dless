using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; // Add for GetRandomCareerFromRate() if needed

/// <summary>
/// Manages the player's card collection, usage, and interactions with career switching.
/// </summary>
public class CardManager : MonoBehaviour
{
    #region Fields
    [Header("Card System References")]
    [SerializeField] private List<Card> _collectedCards = new List<Card>();
    [SerializeField] private int _maxCards = 5;

    [Header("Dependencies")]
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private UIEffectCharacter _uiEffectCharacter;
    

    [Header("Runtime State")]
    private bool _isCardLocked = false;
    private Player _player;
    public static CardManager Instance { get; private set; }
    #endregion


    #region Unity Lifecycle
    
    /// <summary>
    /// Sets the Player dependency. Called by GameManager upon initialization.
    /// </summary>


    private void Awake()
    {
        // 1. จัดการ Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            // 2.ให้คงอยู่เมื่อเปลี่ยน Scene
            DontDestroyOnLoad(gameObject); 
            Debug.Log("[CardManager] Created and set as persistent.");
        }
        else
        {
            // ทำลายตัวเองหากถูกสร้างซ้ำซ้อน
            Destroy(gameObject);
            Debug.LogWarning("[CardManager] Duplicate instance destroyed.");
        }
    }

    public void Initialize(Player player) 
    {
        // 1. ตั้งค่า Player Dependency
        _player = player;

        if (_player == null)
            Debug.LogWarning("[CardManager] Player reference not found in scene!");
            
        // 2. อัปเดต UI ทันที (หาก UI ถูกตั้งค่าใน Inspector)
        if (_cardSlotUI != null)
            _cardSlotUI.SetManager(this);  
            _cardSlotUI.UpdateSlots(_collectedCards);
            _cardSlotUI = FindAnyObjectByType<CardSlotUI>();
            
        // 3. Subscribe Event (ย้ายจาก Start() เดิม)
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;
        }
        else
        {
            // ถ้า CareerSwitcher หายไป จะเกิดปัญหาเมื่อใช้การ์ด Career
            Debug.LogError("[CardManager] Missing CareerSwitcher reference! Card functionality will be limited.");
        }
    }

    public void SetDependencies(Player player)
    {
        _player = player;
    }
    
 


    private void OnDestroy()
    {
        if (_careerSwitcher != null)
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange;
    }
    #endregion

    #region Public Methods
    public void AddCard(Card newCard)
    {
        if (_collectedCards.Count >= _maxCards)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CardManager] Cannot add more cards — deck is full!");
#endif
            return;
        }

        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

#if UNITY_EDITOR
        Debug.Log($"[CardManager] Added card: {newCard.SkillName}");
#endif

    if (HasFullDeck())
    {
        _cardSlotUI.HighlightFullHand();
        MuscleButton.Instance.Show();
    }

    }


    /// <summary>
    /// Uses the card at the specified index.
    /// This is the CORE logic connecting the UI click to the game effect.
    /// </summary>
    public void UseCard(int index)
    {
        if (index < 0 || index >= _collectedCards.Count || _isCardLocked) return;

        Card card = _collectedCards[index];
        if (card == null) return;

        bool cardUsedSuccessfully = false; // Flag check delete card

        //Check card type is Career
        if (card.Type == CardType.Career) //
        {
            
            if (_careerSwitcher == null)
            {
                Debug.LogError("[CardManager] CareerSwitcher is missing!");
                return; 
            }


            DuckCareerData data = card.CareerData;

            if (data != null && _careerSwitcher.CanChangeTo(data))
            {
                // A.switchcareer
                _careerSwitcher.SwitchCareer(data); 
                
                // Play effect
                if (_uiEffectCharacter != null)
                {
                    // เราใช้ชื่อในการ์ด (เช่น "Chef", "Singer") ไปสั่งให้ UIEffect เล่น
                    _uiEffectCharacter.PlayEffect(card.SkillName); //
                }

                cardUsedSuccessfully = true;
            }
            else
            {
                Debug.LogWarning($"[CardManager] Cannot switch to {card.SkillName} (Cooldown or Same Career). Card not used.");

            }
        }
        else if (card.Type == CardType.Berserk) 
        {
     
            Debug.LogWarning("[CardManager] Berserk card logic not yet implemented.");
 
        }
        else
        {
            
            card.ActivateEffect(_player);
            cardUsedSuccessfully = true; 
        }


        // Check when alredy use card | delete after use this card
        if (cardUsedSuccessfully)
        {

            _cardSlotUI?.PlayUseAnimation(index); 
            RemoveCard(index);
            _cardSlotUI.ClearHighlights();
            MuscleButton.Instance.Hide();
        }
    }
    
    public void RemoveCard(int index) // remove card after use
    {
        if (!IsValidIndex(index)) return;

        _collectedCards.RemoveAt(index);
        _cardSlotUI?.UpdateSlots(_collectedCards);
    }

    public bool HasFullDeck() => _collectedCards.Count >= _maxCards; //check if deck is full

    public void ExchangeForBerserk() //exchange 5 cards for MuscleDuck
    {
#if UNITY_EDITOR
        Debug.Log("[CardManager] Exchanging cards for Berserk Mode!");
#endif
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots();

        if (_careerSwitcher != null)
        {
            _careerSwitcher.SwitchCareerByName("Muscle");
            _careerSwitcher.StartCareerTimer(10f);
            _cardSlotUI.ClearHighlights();
            LockAllCards();
        }
    }
    #endregion

    #region Protected / Virtual Methods
    protected virtual void OnCardUsed(Card usedCard)
    {
#if UNITY_EDITOR
        Debug.Log($"[CardManager] OnCardUsed triggered for {usedCard.SkillName}");
#endif
    }
    #endregion

    #region Private Helpers
    private bool IsValidIndex(int index) => index >= 0 && index < _collectedCards.Count;

    private void LockAllCards()
    {
        _isCardLocked = true;
#if UNITY_EDITOR
        Debug.Log("[CardManager] Cards locked during career duration.");
#endif
    }

    private void UnlockAllCards() //unlock to use cards again
    {
        _isCardLocked = false;
#if UNITY_EDITOR
        Debug.Log("[CardManager] Cards unlocked — ready to use again.");
#endif
    }



    private void HandleCareerChange(DuckCareerData newCareer) // when career changed back to default
    {
        if (newCareer == null || _careerSwitcher == null) return;


        if (newCareer == _careerSwitcher.GetCareerData(DuckCareer.Duckling))
        {
            UnlockAllCards();
        }
    }
    #endregion


    #region Career Card Drop (GoldenMon)
    /// <summary>
    /// Called when a GoldenMon dies.
    /// Adds exactly 1 career card based on defined PDF drop rates.
    /// </summary>
public void AddCareerCard()
    {
        if (_isCardLocked || _collectedCards.Count >= _maxCards)
        {
            Debug.Log("[CardManager] Cannot add new career card. (Locked or Full)");
            return;
        }

        DuckCareer career = GetRandomCareerFromRate();

        if (_careerSwitcher == null)
        {
            Debug.LogError("[CardManager] CareerSwitcher reference is missing!");
            return;
        }
        
        DuckCareerData careerData = _careerSwitcher.GetCareerData(career);
        if (careerData == null)
        {
            Debug.LogError($"[CardManager] Cannot find CareerData for {career}");
            return;
        }

        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, careerData);

        AddCard(newCard);
        Debug.Log($"[CardManager] Dropped career card: {careerData.DisplayName}");
    }

    /// <summary>
    /// Randomly selects a career based on DUFFDUCK_CAREER.pdf drop rate table.
    /// </summary>
    private DuckCareer GetRandomCareerFromRate()
    {
        float roll = Random.Range(0f, 100f);
        float sum = 0f;

        // B-tier
        if (roll < (sum += 13f)) return DuckCareer.Dancer;        // 13%
        else if (roll < (sum += 12f)) return DuckCareer.Detective; // 25%
        else if (roll < (sum += 12f)) return DuckCareer.Motorcycle; // 37%

        // A-tier
        else if (roll < (sum += 10f)) return DuckCareer.Chef;       // 47%
        else if (roll < (sum += 9f)) return DuckCareer.Firefighter; // 56%
        else if (roll < (sum += 10f)) return DuckCareer.Programmer; // 66%

        // S-tier
        else if (roll < (sum += 10f)) return DuckCareer.Doctor;     // 76%
        else if (roll < (sum += 6f)) return DuckCareer.Singer;      // 82%

        // No Drop (should not happen for GoldenMon)
        return DuckCareer.None;
    }
    #endregion
}
