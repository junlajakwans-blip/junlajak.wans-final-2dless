using System.Collections.Generic;
using UnityEngine;

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
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _player = FindFirstObjectByType<Player>();
        if (_player == null)
            Debug.LogWarning("[CardManager] Player reference not found in scene!");

        if (_cardSlotUI != null)
            _cardSlotUI.UpdateSlots(_collectedCards);

        if (_careerSwitcher != null)
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;
        else
            Debug.LogWarning("[CardManager] Missing CareerSwitcher reference!");
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


            DuckCareerData data = _careerSwitcher.GetCareerDataByName(card.SkillName); //

            if (data != null && _careerSwitcher.CanChangeTo(data)) //
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
        else if (card.Type == CardType.Berserk) //
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
        
        string skillName = careerData.DisplayName; 
        string description = careerData.SkillDescription;
        Sprite icon = careerData.SkillIcon; 
        
        Card newCard = new Card(cardID, CardType.Career, skillName, description, icon);

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
