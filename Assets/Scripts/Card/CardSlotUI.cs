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
    [SerializeField] private Color _lockedColor = Color.gray; 

    [Header("Animation")]
    [SerializeField] private Animator _useAnimation;


    private CardManager _manager;
    #endregion

    private void Awake() //Don't Destroy When Change Scene
    {
        DontDestroyOnLoad(gameObject);
    }

    #region Public Methods

    /// <summary>
    /// Sets the manager dependency. Called by CardManager upon initialization.
    /// </summary>
    public void SetManager(CardManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Updates the visual icons in the slots based on the current deck.
    /// </summary>
    public void UpdateSlots(List<Card> cards)
    {
        ResetAllSlots();

        for (int i = 0; i < cards.Count && i < _cardSlots.Count; i++)
        {
            // Assuming Card class has a public Icon property
            _cardSlots[i].sprite = cards[i].Icon; 
            _cardSlots[i].color = _defaultColor;
        }

        Debug.Log($" Updated {cards.Count} card slots.");
    }

    /// <summary>
    /// Highlights a specific card slot (e.g., during selection).
    /// </summary>
    public void HighlightSlot(int index)
    {
        if (IsValidIndex(index))
            _cardSlots[index].color = _highlightColor;
    }

    /// <summary>
    /// Locks all card slots visually (during active career or cooldown).
    /// </summary>

    /// <summary>
    /// Highlight all slots when 5 cards are collected (MuscleDuck condition)
    /// </summary>
    public void HighlightFullHand()
    {
        foreach (var slot in _cardSlots)
        {
            if (slot.sprite != null)
                slot.color = _highlightColor;
        }
    }

    /// <summary>
    /// Removes highlight after card usage or after MuscleDuck activation
    /// </summary>
    public void ClearHighlights()
    {
        foreach (var slot in _cardSlots)
        {
            if (slot.sprite != null)
                slot.color = _defaultColor;
        }
    }



    public void LockAllSlots()
    {
        foreach (var slot in _cardSlots)
        {
            slot.color = _lockedColor;
        }
    }

    /// <summary>
    /// Unlocks all card slots visually.
    /// </summary>
    public void UnlockAllSlots()
    {
        foreach (var slot in _cardSlots)
        {
            // Only unlock slots that currently have a card icon
            if(slot.sprite != null)
                slot.color = _defaultColor;
        }
    }


    /// <summary>
    /// Clears a specific slot (icon and color).
    /// </summary>
    public void ClearSlot(int index)
    {
        if (IsValidIndex(index))
        {
            _cardSlots[index].sprite = null;
            _cardSlots[index].color = _defaultColor;
        }
    }


    /// <summary>
    /// Plays the visual animation when a card is used.
    /// </summary>
    public void PlayUseAnimation(int index)
    {
        if (_useAnimation != null)
        {
            _useAnimation.SetTrigger("Use");
            Debug.Log($" Playing use animation for slot {index}");
        }
    }


    /// <summary>
    /// Resets all visual slots to default state (no icon, default color).
    /// </summary>
    public void ResetAllSlots()
    {
        foreach (var slot in _cardSlots)
        {
            slot.sprite = null;
            slot.color = _defaultColor;
        }
    }
    
    /// <summary>
    /// PUBLIC HOOK: Called by the player clicking a button/slot on the UI.
    /// Delegates the card usage logic to the CardManager.
    /// </summary>
    /// <param name="index">The 0-based index of the slot clicked.</param>
    public void HandleSlotClick(int index)
    {
        if (_manager == null)
        {
            Debug.LogError("[CardSlotUI] Manager dependency missing. Cannot process card use.");
            return;
        }
        
        // Delegate the logic to the CardManager (which handles cooldown, usage, and removal)
        _manager.UseCard(index);
    }
    #endregion

    #region Private Helpers
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _cardSlots.Count;
    }
    #endregion
}