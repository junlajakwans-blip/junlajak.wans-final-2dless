using System.Collections.Generic;
using UnityEngine;


public class CardManager : MonoBehaviour
{
    #region Fields
    [Header("Card System References")]
    [SerializeField] private List<Card> _collectedCards = new List<Card>();
    [SerializeField] private int _maxCards = 5;

    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private UIEffectCharacter _uiEffectCharacter;

    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_cardSlotUI != null)
            _cardSlotUI.UpdateSlots(_collectedCards);
    }
    #endregion

    #region Public Methods

    public void AddCard(Card newCard)
    {
        if (_collectedCards.Count >= _maxCards)
        {
            Debug.Log(" Cannot add more cards — deck is full!");
            return;
        }

        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);
        Debug.Log($" Added card: {newCard.SkillName}");
    }


    public void UseCard(int index)
    {
        if (!IsValidIndex(index)) return;

        Card usedCard = _collectedCards[index];
        usedCard.ActivateEffect(FindObjectOfType<Player>());
        _uiEffectCharacter?.ShowEffect(usedCard);

        _cardSlotUI?.PlayUseAnimation(index);
        OnCardUsed(usedCard);
        RemoveCard(index);

        Debug.Log($" Used card: {usedCard.SkillName}");
    }


    public void RemoveCard(int index)
    {
        if (!IsValidIndex(index)) return;

        _collectedCards.RemoveAt(index);
        _cardSlotUI?.UpdateSlots(_collectedCards);
    }

    public bool HasFullDeck() => _collectedCards.Count >= _maxCards;


    public void ExchangeForBerserk()
    {
        Debug.Log(" Exchanging cards for Berserk Mode!");
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots();

        _careerSwitcher?.ActivateBerserkMode();
    }
    #endregion

    #region Protected / Virtual Methods
    protected virtual void OnCardUsed(Card usedCard)
    {
        Debug.Log($" OnCardUsed triggered for {usedCard.SkillName}");
    }
    #endregion

    #region Private Helpers
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _collectedCards.Count;
    }
    #endregion
}
