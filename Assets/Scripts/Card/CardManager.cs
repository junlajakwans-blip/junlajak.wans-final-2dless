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
    /// Uses a card at the specified index.
    /// </summary>
    /// <param name="index"></param>
    public void UseCard(int index)
    {
        if (!IsValidIndex(index) || _isCardLocked) return;

        Card usedCard = _collectedCards[index];
        if (usedCard == null) return;

        // เล่นเอฟเฟกต์
        _uiEffectCharacter?.PlayEffect(usedCard.SkillName);
        _cardSlotUI?.PlayUseAnimation(index);

        switch (usedCard.Type)
        {
            case CardType.Career:
                // 🔹 การ์ดอาชีพ — เปลี่ยนอาชีพชั่วคราว
                if (_careerSwitcher != null)
                {
                    var data = _careerSwitcher.GetCareerDataByName(usedCard.SkillName);
                    if (data != null)
                    {
                        _careerSwitcher.SwitchCareer(data);
                        _careerSwitcher.StartCareerTimer(10f); // duration 10 seconds
                        LockAllCards(); // lock cards during career duration until revert
                    }
                }
                break;

            case CardType.Berserk:
                // call MuscleDuck
                ExchangeForBerserk();
                break;
        }

        RemoveCard(index);
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
}
