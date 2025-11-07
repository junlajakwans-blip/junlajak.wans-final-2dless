using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    #region Fields
    [Header("Slot Components")]
    [SerializeField] private List<Image> _cardSlots = new List<Image>();

    [Header("Visual Settings")]
    [SerializeField] private Color _highlightColor = Color.yellow;
    [SerializeField] private Color _defaultColor = Color.white;

    [Header("Animation")]
    [SerializeField] private Animator _useAnimation;

    [Header("Card Icons")]
    [SerializeField] private List<Sprite> _cardIcons = new List<Sprite>();
    #endregion

    #region Public Methods

    public void UpdateSlots(List<Card> cards)
    {
        ResetAllSlots();

        for (int i = 0; i < cards.Count && i < _cardSlots.Count; i++)
        {
            _cardSlots[i].sprite = cards[i].Icon;
            _cardSlots[i].color = _defaultColor;
        }

        Debug.Log($" Updated {_cardSlots.Count} card slots.");
    }


    public void HighlightSlot(int index)
    {
        if (IsValidIndex(index))
            _cardSlots[index].color = _highlightColor;
    }


    public void ClearSlot(int index)
    {
        if (IsValidIndex(index))
        {
            _cardSlots[index].sprite = null;
            _cardSlots[index].color = _defaultColor;
        }
    }


    public void PlayUseAnimation(int index)
    {
        if (_useAnimation != null)
        {
            _useAnimation.SetTrigger("Use");
            Debug.Log($" Playing use animation for slot {index}");
        }
    }


    public void ResetAllSlots()
    {
        foreach (var slot in _cardSlots)
        {
            slot.sprite = null;
            slot.color = _defaultColor;
        }
    }
    #endregion

    #region Private Helpers
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _cardSlots.Count;
    }
    #endregion
}
