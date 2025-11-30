using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class CardManager : MonoBehaviour
{
    #region Fields
    [Header("Card System")]
    [SerializeField] private List<Card> _collectedCards = new();
    [SerializeField] private int _maxCards = 5;

    [Header("Dependencies")]
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private UIEffectCharacter _uiEffectCharacter;
    [SerializeField] private MuscleButton _muscleButton;

    private Player _player;
    private bool _isCardLocked = false;
    private const int _maxSameCardInHand = 2;

    public static CardManager Instance { get; private set; }
    public RandomStarterCard starterPanel;

    public bool IsReady { get; private set; }

    private Coroutine _careerCooldownRoutine;
    private int _muscleUseCount = 0;
    #endregion




    #region Unity Lifecycle Card Manager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
       }

    public void Initialize(Player player)
    {
        _player = player;
        _collectedCards.Clear();
        EnsureCardUI();
        _cardSlotUI?.ResetAllSlots();

        if (_muscleButton != null)
            _muscleButton.Hide();

        if (_careerSwitcher != null) 
        {
            // 1. à¸ªà¸¡à¸±à¸„à¸£ Event à¸›à¸à¸•à¸´
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange; // à¸›à¹‰à¸­à¸‡à¸à¸±à¸™à¸à¸²à¸£à¸ªà¸¡à¸±à¸„à¸£à¸‹à¹‰à¸³
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;

            // 2. à¹ƒà¸Šà¹‰ ResetCareerDropCycle à¹à¸—à¸™à¸à¸²à¸£à¹ƒà¸Šà¹‰ Lambda à¸—à¸µà¹ˆà¸ˆà¸±à¸”à¸à¸²à¸£à¸¢à¸²à¸
            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle; // à¸›à¹‰à¸­à¸‡à¸à¸±à¸™à¸à¸²à¸£à¸ªà¸¡à¸±à¸„à¸£à¸‹à¹‰à¸³
            _careerSwitcher.OnResetCareerCycle += ResetCareerDropCycle;
        }
        
        // à¸•à¸±à¹‰à¸‡à¸„à¹ˆà¸²à¹€à¸£à¸´à¹ˆà¸¡à¸•à¹‰à¸™
        ResetCareerDropCycle(); 
    }

    // à¹à¸à¹‰à¹„à¸‚ ResetCareerDropCycle() à¹ƒà¸«à¹‰à¸£à¸§à¸¡ UnlockCards()
    private void ResetCareerDropCycle()
    {

        _isCardLocked = false;          // à¸›à¸¥à¸”à¸¥à¹‡à¸­à¸à¸à¸²à¸£à¹Œà¸”
        _cardSlotUI?.UnlockAllSlots();  // à¸›à¸¥à¸”à¸¥à¹‡à¸­à¸ UI
        _cardSlotUI?.ClearHighlights(); // à¸”à¸±à¸š Hilight à¹€à¸•à¹‡à¸¡ 5 à¹ƒà¸š
        _muscleButton?.Hide();          // à¸‹à¹ˆà¸­à¸™à¸›à¸¸à¹ˆà¸¡ Muscle
        Debug.Log("[CardManager] Reset career cycle: drop counter = 0, cards unlocked.");
    }

    private void OnDestroy()
    {
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange;
            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle; // Unsubscribe à¸ˆà¸²à¸à¹€à¸¡à¸˜à¸­à¸”à¸—à¸µà¹ˆà¹ƒà¸Šà¹‰
        }
    }

    #endregion

        public void SetCareerSwitcher(CareerSwitcher switcher)
    {
        _careerSwitcher = switcher;

        if (_careerSwitcher != null)
        {
            // bind events
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange;
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;

            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle;
            _careerSwitcher.OnResetCareerCycle += ResetCareerDropCycle;
        }

        Debug.Log($"[CardManager] CareerSwitcher assigned â†’ {switcher != null}");
    }


    #region Add / Remove Card
    /// <summary>
    /// à¹ƒà¸Šà¹‰à¸ªà¸³à¸«à¸£à¸±à¸š Monster Drop â†’ à¸ªà¸¸à¹ˆà¸¡à¸­à¸²à¸Šà¸µà¸žà¸•à¸²à¸¡ rate à¹à¸¥à¸°à¹„à¸¡à¹ˆà¹€à¸à¸´à¸™ 2 à¹ƒà¸šà¸‹à¹‰à¸³à¹ƒà¸™à¸¡à¸·à¸­
    /// </summary>
    public DuckCareerData GetRandomCareerForDrop()
    {
        DuckCareer career;

        // à¸ªà¸¸à¹ˆà¸¡à¸ˆà¸™à¸à¸§à¹ˆà¸²à¸­à¸²à¸Šà¸µà¸žà¸™à¸µà¹‰à¸ˆà¸°à¸¢à¸±à¸‡à¹„à¸¡à¹ˆà¹€à¸à¸´à¸™ 2 à¹ƒà¸šà¹ƒà¸™à¸¡à¸·à¸­
        do
        {
            career = GetRandomCareerFromRate();
        }
        while (CheckCareerCountInHand(career) >= _maxSameCardInHand);

        return _careerSwitcher.GetCareerData(career);
    }

    public void AddCard(Card newCard)
    {
        EnsureCardUI();
        // 1. à¸•à¸£à¸§à¸ˆà¸ªà¸­à¸šà¹€à¸‡à¸·à¹ˆà¸­à¸™à¹„à¸‚à¸«à¹‰à¸²à¸¡à¹€à¸žà¸´à¹ˆà¸¡: à¸–à¸¹à¸à¸¥à¹‡à¸­à¸
        if (_isCardLocked)
        {
            Debug.Log("[CardManager] Cannot add card: Card system is locked.");
            return;
        }

        // 2. à¸•à¸£à¸§à¸ˆà¸ªà¸­à¸šà¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡
        if (_collectedCards.Count >= _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show(); 
            Debug.Log("[CardManager] Hand is full. Card was not added. Showing Muscle Button.");
            return; //à¹€à¸žà¸·à¹ˆà¸­à¸šà¸¥à¹‡à¸­à¸à¸à¸²à¸£à¹€à¸žà¸´à¹ˆà¸¡à¹€à¸¡à¸·à¹ˆà¸­à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡
        }

        // 3. à¸–à¹‰à¸²à¹„à¸¡à¹ˆà¹€à¸•à¹‡à¸¡à¹à¸¥à¸°à¹„à¸¡à¹ˆà¸–à¸¹à¸à¸¥à¹‡à¸­à¸ -> à¹€à¸žà¸´à¹ˆà¸¡à¸à¸²à¸£à¹Œà¸”
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // 4. à¸•à¸£à¸§à¸ˆà¸ªà¸­à¸šà¸§à¹ˆà¸²à¸à¸²à¸£à¹€à¸žà¸´à¹ˆà¸¡à¸—à¸³à¹ƒà¸«à¹‰à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡à¸«à¸£à¸·à¸­à¹„à¸¡à¹ˆ (Trigger MuscleDuck Mode)
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
            Debug.Log("[CardManager] Hand is now full. Muscle Button is visible.");
        }
    }

    public void AddCareerCard_FromDrop(DuckCareerData dataFromPickup)
    {
        EnsureCardUI();
        // Fallbacks à¸ˆà¸°à¹„à¸› AddCareerCard() â†’ à¹à¸¥à¹‰à¸§ Refresh UI à¸•à¸­à¸™à¸ˆà¸š
        void Finish()
        {
            _cardSlotUI?.UpdateSlots(_collectedCards);
            _cardSlotUI?.ClearHighlights();
        }

        if (dataFromPickup == null)
        {
            Debug.LogWarning("[CardManager] âŒ Drop career NULL â†’ fallback random.");
            AddCareerCard(); // à¹ƒà¸Šà¹‰à¸£à¸°à¸šà¸šà¸ªà¸¸à¹ˆà¸¡à¹à¸—à¸™
            return;
        }

        // 1) à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡ â†’ Token à¹à¸—à¸™
        if (_collectedCards.Count >= _maxCards)
        {
            _player.AddToken(1);
            Debug.Log("[CardManager] Hand full â†’ Token instead.");
            return;
        }

        // 2) à¸«à¹‰à¸²à¸¡ Muscle Duck à¸ˆà¸²à¸à¸à¸²à¸£à¸”à¸£à¸­à¸›à¹€à¸”à¹‡à¸”à¸‚à¸²à¸”
        if (dataFromPickup.CareerID == DuckCareer.Muscle)
        {
            Debug.Log("[CardManager] âŒ Muscle cannot be dropped â†’ fallback random.");
            AddCareerCard();
            return;
        }

        // 3) à¸ˆà¸³à¸à¸±à¸”à¹„à¸¡à¹ˆà¹€à¸à¸´à¸™ 2 à¹ƒà¸šà¸•à¹ˆà¸­ 1 à¸­à¸²à¸Šà¸µà¸ž
        if (CheckCareerCountInHand(dataFromPickup.CareerID) >= _maxSameCardInHand)
        {
            Debug.Log($"[CardManager] âš  Already have 2 cards of {dataFromPickup.DisplayName} â†’ fallback random.");
            AddCareerCard();
            return;
        }

        Debug.Log($"[CardManager] ðŸŽ´ Added dropped card: {(string.IsNullOrWhiteSpace(dataFromPickup.DisplayName) ? dataFromPickup.CareerID.ToString() : dataFromPickup.DisplayName)} | Hand={_collectedCards.Count}/{_maxCards}");

        // 4) à¸–à¸¹à¸à¸•à¹‰à¸­à¸‡ â†’ à¹€à¸žà¸´à¹ˆà¸¡à¸à¸²à¸£à¹Œà¸”à¸¥à¸‡à¸¡à¸·à¸­
        string id = System.Guid.NewGuid().ToString();
        Card newCard = new Card(id, dataFromPickup);
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡à¸žà¸­à¸”à¸µ â†’ à¹à¸ªà¸”à¸‡ Muscle Button
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
        }

        Finish();

        Debug.Log($"[CardManager] Hand now = {_collectedCards.Count}/{_maxCards}");

    }


    public void RemoveCard(int index)
    {
        if (!IsValidIndex(index)) return;

        _collectedCards.RemoveAt(index);
        _cardSlotUI?.UpdateSlots(_collectedCards);
        _cardSlotUI?.ClearHighlights();
    }
    #endregion


    #region Use Card Logic

    public void OnClickStarterButton()
    {
        starterPanel.OpenPanel();
    }


    public void UseCard(int index)
    {
        if (_isCardLocked || !IsValidIndex(index)) return;

        Card card = _collectedCards[index];
        bool success = false;

        if (card.Type == CardType.Career)
        {
            var data = card.CareerData;
            if (_careerSwitcher != null && data != null && _careerSwitcher.CanChangeTo(data))
            {
                _careerSwitcher.SwitchCareer(data);
                _careerSwitcher.StartCareerTimer(data.CareerDuration); // â± à¸”à¸¶à¸‡à¸ˆà¸²à¸ ScriptableObject
                HandleCareerCooldown(data); // â›” à¸¥à¹‡à¸­à¸„à¸à¸²à¸£à¹Œà¸”à¸•à¸²à¸¡ cooldown à¸‚à¸­à¸‡à¸­à¸²à¸Šà¸µà¸žà¸™à¸µà¹‰
                _uiEffectCharacter?.PlayEffect(card.SkillName);
                success = true;
            }
        }
        else if (card.Type == CardType.Berserk)
        {
            ActivateMuscleDuck();
            success = true;
        }
        else
        {
            card.ActivateEffect(_player);
            success = true;
        }

        if (success)
        {
            _cardSlotUI?.PlayUseAnimation(index);
            RemoveCard(index);
        }

        if (_collectedCards.Count < _maxCards)
        {
            _cardSlotUI?.ClearHighlights();
            _muscleButton?.Hide();
        }
    }
    #endregion


    #region Muscle Duck
    public void ActivateMuscleDuck()
    {
        if (_careerSwitcher == null) return;

        // à¸¥à¸šà¸à¸²à¸£à¹Œà¸”à¸—à¸±à¹‰à¸‡ 5 à¹ƒà¸šà¸­à¸­à¸à¸ˆà¸²à¸à¹€à¸”à¹‡à¸„à¸à¹ˆà¸­à¸™
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots();

        // à¸‹à¹ˆà¸­à¸™à¸›à¸¸à¹ˆà¸¡ Muscle
        _muscleButton?.Hide();

        // à¸ªà¸¥à¸±à¸šà¸­à¸²à¸Šà¸µà¸žà¹€à¸›à¹‡à¸™ Muscle Duck
        var muscleCareer = _careerSwitcher.GetCareerData(DuckCareer.Muscle);
        _careerSwitcher.SwitchCareer(muscleCareer);

        _careerSwitcher.StartCareerTimer(10f);

        // à¸¥à¹‡à¸­à¸à¸à¸²à¸£à¹Œà¸”à¸£à¸°à¸«à¸§à¹ˆà¸²à¸‡à¹ƒà¸Šà¹‰ Muscle Duck
        LockCards();
    }

    #endregion


    #region Lock / Unlock
    private void LockCards()
    {
        _isCardLocked = true;
        _cardSlotUI?.LockAllSlots();
    }

    private void UnlockCards()
    {
        _isCardLocked = false;
        _cardSlotUI?.UnlockAllSlots();
    }

    private void HandleCareerChange(DuckCareerData newCareer)
    {
        if (newCareer == null || _careerSwitcher == null) return;

        if (newCareer == _careerSwitcher.GetCareerData(DuckCareer.Duckling))
        {
            UnlockCards();
            _muscleUseCount = 0; // Reset muscle bonus stack
        }

    }
    #endregion

    public void AddStarterCard()
    {
        // 1. à¸•à¸£à¸§à¸ˆà¸ªà¸­à¸šà¹€à¸‡à¸·à¹ˆà¸­à¸™à¹„à¸‚à¸«à¹‰à¸²à¸¡à¹€à¸žà¸´à¹ˆà¸¡: à¸–à¸¹à¸à¸¥à¹‡à¸­à¸ à¸«à¸£à¸·à¸­ à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡
        if (_isCardLocked || _collectedCards.Count >= _maxCards)
        {
             // à¹à¸ªà¸”à¸‡à¸›à¸¸à¹ˆà¸¡ Muscle à¹à¸¥à¸° Highlight à¹€à¸¡à¸·à¹ˆà¸­à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡ (à¹ƒà¸™à¸à¸£à¸“à¸µà¸—à¸µà¹ˆà¸–à¸¹à¸à¹€à¸£à¸µà¸¢à¸à¸•à¸­à¸™à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡)
             if (_collectedCards.Count >= _maxCards)
             {
                _cardSlotUI?.HighlightFullHand();
                _muscleButton?.Show();
             }
             Debug.Log("[CardManager] Cannot add Starter Card. (Locked or Full)");
             return;
        }
        
        // 2. à¸ªà¸£à¹‰à¸²à¸‡à¸à¸²à¸£à¹Œà¸”à¹à¸šà¸šà¸ªà¸¸à¹ˆà¸¡ (à¹ƒà¸Šà¹‰ Logic à¸ªà¸¸à¹ˆà¸¡à¹€à¸”à¸´à¸¡)
        DuckCareer career = GetRandomCareerFromRate();

        if (_careerSwitcher == null) 
        {
            Debug.LogError("[CardManager] CareerSwitcher is not set! Cannot add starter card.");
            return; 
        }
        DuckCareerData careerData = _careerSwitcher.GetCareerData(career);

        if (careerData == null)
        {
            Debug.LogError($"[CardManager] âŒ CareerData = NULL for career {career} â€” _allCareers à¸­à¸²à¸ˆà¸¢à¸±à¸‡à¹„à¸¡à¹ˆà¹ƒà¸ªà¹ˆà¸•à¸±à¸§à¸™à¸µà¹‰à¹ƒà¸™ CareerSwitcher");
            return;
        }

        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, careerData);

        // 3. à¹€à¸žà¸´à¹ˆà¸¡à¸à¸²à¸£à¹Œà¸” (à¹ƒà¸Šà¹‰ AddCard à¸—à¸µà¹ˆà¹à¸à¹‰à¹„à¸‚à¹à¸¥à¹‰à¸§)
        // AddCard à¸ˆà¸°à¸ˆà¸±à¸”à¸à¸²à¸£à¹€à¸£à¸·à¹ˆà¸­à¸‡ UI à¹à¸¥à¸° MuscleButton à¸•à¹ˆà¸­à¹„à¸›
        AddCard(newCard); 
        _cardSlotUI?.HighlightSlot(_collectedCards.Count - 1);


        // â˜… à¹„à¸¡à¹ˆà¸•à¹‰à¸­à¸‡à¹€à¸žà¸´à¹ˆà¸¡ _careerCardDropCount
        Debug.Log($"[CardManager] Added Starter Card: {careerData.DisplayName}");
    }

    
#region Drop from GoldenMon
    /// <summary>
    /// à¹„à¸”à¹‰à¸£à¸±à¸šà¸à¸²à¸£à¹Œà¸”à¸ˆà¸²à¸à¸à¸²à¸£à¸”à¸£à¸­à¸› GoldenMon â€” à¹„à¸¡à¹ˆà¹‚à¸”à¸™ Card Lock à¸šà¸¥à¹‡à¸­à¸
    /// à¸–à¹‰à¸²à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡ â†’ à¹„à¸”à¹‰ Token à¹à¸—à¸™
    /// Muscle Duck â†’ à¹„à¸”à¹‰à¸à¸²à¸£à¹Œà¸” + Token (à¹„à¸”à¹‰à¸à¸²à¸£à¹Œà¸”à¸à¹ˆà¸­à¸™ à¹à¸¥à¹‰à¸§ Token à¸ˆà¸°à¹€à¸žà¸´à¹ˆà¸¡à¸«à¸¥à¸±à¸‡à¸ˆà¸²à¸à¹€à¸•à¹‡à¸¡)
    /// </summary>
    public void AddCareerCard()
    {
        // 1) à¸–à¹‰à¸²à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡ â†’ à¹„à¸”à¹‰ Token à¹à¸—à¸™
        if (_collectedCards.Count >= _maxCards)
        {
            _player.AddToken(1);
            Debug.Log("[CardManager] Hand full â†’ Drop Token instead of Card.");
            return;
        }

        // 2) à¸ªà¸¸à¹ˆà¸¡à¸­à¸²à¸Šà¸µà¸žà¸ˆà¸™à¸à¸§à¹ˆà¸²à¸ˆà¸°à¹„à¸¡à¹ˆà¹€à¸à¸´à¸™ 2 à¹ƒà¸šà¹ƒà¸™à¸¡à¸·à¸­
        DuckCareer career;
        do
        {
            career = GetRandomCareerFromRate();
        }
        while (CheckCareerCountInHand(career) >= _maxSameCardInHand);

        DuckCareerData data = _careerSwitcher?.GetCareerData(career);
        if (data == null)
        {
            Debug.LogError($"[CardManager] âŒ CareerData is NULL for career {career} â€” Card not created.");
            return;
        }

        // 3) à¸ªà¸£à¹‰à¸²à¸‡ Card object
        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, data);

        // 4) à¹€à¸žà¸´à¹ˆà¸¡à¹€à¸‚à¹‰à¸²à¸¡à¸·à¸­ â€” â— à¹à¸¡à¹‰à¸£à¸°à¸šà¸šà¸à¸²à¸£à¹Œà¸”à¸ˆà¸° Lock à¸à¹‡à¸¢à¸±à¸‡à¸•à¹‰à¸­à¸‡à¹€à¸žà¸´à¹ˆà¸¡à¹„à¸”à¹‰
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // 5) à¸–à¹‰à¸²à¸¡à¸·à¸­à¹€à¸•à¹‡à¸¡à¸žà¸­à¸”à¸µà¸«à¸¥à¸±à¸‡à¹€à¸žà¸´à¹ˆà¸¡ â†’ Highlight + Muscle button
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
        }

        Debug.Log($"[CardManager] GoldenMon dropped card: {data.DisplayName}");
    }


    /// <summary>
    /// NEW: à¸™à¸±à¸šà¸ˆà¸³à¸™à¸§à¸™à¸à¸²à¸£à¹Œà¸”à¸­à¸²à¸Šà¸µà¸žà¸—à¸µà¹ˆà¸£à¸°à¸šà¸¸à¸—à¸µà¹ˆà¸¡à¸µà¸­à¸¢à¸¹à¹ˆà¹ƒà¸™à¸¡à¸·à¸­à¸œà¸¹à¹‰à¹€à¸¥à¹ˆà¸™
    /// </summary>
    private int CheckCareerCountInHand(DuckCareer careerType)
    {
        int count = 0;
        foreach (var card in _collectedCards)
        {
            // à¸•à¹‰à¸­à¸‡à¸¡à¸±à¹ˆà¸™à¹ƒà¸ˆà¸§à¹ˆà¸² Card à¸¡à¸µ property Type à¹à¸¥à¸° CareerData à¸—à¸µà¹ˆà¹ƒà¸Šà¹‰à¹„à¸”à¹‰
            if (card.Type == CardType.Career && card.CareerData != null && card.CareerData.CareerID == careerType)
            {
                count++;
            }
        }
        return count;
    }

    private DuckCareer GetRandomCareerFromRate() //Random rate only career
    {
        float roll = Random.Range(0f, 100f);
        float sum = 0f;

        // Total 100% (à¸›à¸£à¸±à¸šà¸ˆà¸²à¸ 82% à¹€à¸”à¸´à¸¡)
        // à¸­à¸±à¸•à¸£à¸²à¸ªà¹ˆà¸§à¸™à¹ƒà¸«à¸¡à¹ˆ: 16 + 15 + 15 + 12 + 11 + 12 + 12 + 7 = 100
        
        if (roll < (sum += 16f)) return DuckCareer.Dancer;     // 16% (à¹€à¸”à¸´à¸¡ 13%)
        if (roll < (sum += 15f)) return DuckCareer.Detective;  // 15% (à¹€à¸”à¸´à¸¡ 12%)
        if (roll < (sum += 15f)) return DuckCareer.Motorcycle; // 15% (à¹€à¸”à¸´à¸¡ 12%)
        if (roll < (sum += 12f)) return DuckCareer.Chef;       // 12% (à¹€à¸”à¸´à¸¡ 10%)
        if (roll < (sum += 11f)) return DuckCareer.Firefighter; // 11% (à¹€à¸”à¸´à¸¡ 9%)
        if (roll < (sum += 12f)) return DuckCareer.Programmer;  // 12% (à¹€à¸”à¸´à¸¡ 10%)
        if (roll < (sum += 12f)) return DuckCareer.Doctor;     // 12% (à¹€à¸”à¸´à¸¡ 10%)
        if (roll < (sum += 7f))  return DuckCareer.Singer;      // 7% (à¹€à¸”à¸´à¸¡ 6%)
        
        // à¹€à¸¡à¸·à¹ˆà¸­à¸£à¸§à¸¡à¸à¸±à¸™à¹€à¸›à¹‡à¸™ 100% à¹à¸¥à¹‰à¸§ à¹‚à¸„à¹‰à¸”à¸ˆà¸°à¹„à¸¡à¹ˆà¸ªà¸²à¸¡à¸²à¸£à¸–à¸¡à¸²à¸–à¸¶à¸‡à¸šà¸£à¸£à¸—à¸±à¸”à¸™à¸µà¹‰à¹„à¸”à¹‰
        return DuckCareer.Detective; // Fallback à¸ªà¸¸à¸”à¸—à¹‰à¸²à¸¢ (à¸«à¸£à¸·à¸­ return à¸„à¹ˆà¸²à¸­à¸·à¹ˆà¸™à¸—à¸µà¹ˆà¹€à¸«à¸¡à¸²à¸°à¸ªà¸¡)
    }
    #endregion


    #region Helpers
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _collectedCards.Count;
    }

    private void HandleCareerCooldown(DuckCareerData careerData)
    {
        if (careerData.CareerID == DuckCareer.Muscle)
        {
            float time = careerData.CareerDuration + 15f * _muscleUseCount;
            _muscleUseCount++;
            _careerSwitcher.StartCareerTimer(time);
            LockCards();
            return;
        }

        float extraCooldown = careerData.CareerCooldownAfterUse;

        if (_careerCooldownRoutine != null)
            StopCoroutine(_careerCooldownRoutine);

        _careerCooldownRoutine = StartCoroutine(UnlockAfterCooldown(extraCooldown));
    }

    private IEnumerator UnlockAfterCooldown(float time)
    {
        LockCards();
        Debug.Log($"[CardManager] Extra Career cooldown: {time}s");
        yield return new WaitForSeconds(time);
        UnlockCards();
    }

    private void EnsureCardUI()
    {
        if (_cardSlotUI == null)
        {
            _cardSlotUI = FindFirstObjectByType<CardSlotUI>();
            _cardSlotUI?.SetManager(this);
        }
    }

    public void ForceUIRefresh()
    {
        _cardSlotUI?.UpdateSlots(_collectedCards);
    }

    #endregion
}



