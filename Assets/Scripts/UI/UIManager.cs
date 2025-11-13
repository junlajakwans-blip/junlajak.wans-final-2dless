using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Fields
    [Header("Main UI References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] private StoreUI _storeUI;

    [Header("System References")]
    [SerializeField] private StoreManager _storeManager;
    
    #endregion


        
    #region Health UI
    public void InitializeHealth(int maxHP)
    {
        _healthBarUI?.InitializeHealth(maxHP);
    }

    public void UpdateHealth(int currentHP)
    {
        _healthBarUI?.UpdateHealth(currentHP);
    }

    public void ShowDamageEffect()
    {
        _healthBarUI?.AnimateDamageEffect();
    }

    public void ShowHealEffect()
    {
        _healthBarUI?.AnimateHealEffect();
    }
    #endregion

    #region Score UI
    public void InitializeScore(int startScore)
    {
        _scoreUI?.InitializeScore(startScore);
    }

    public void UpdateScore(int newScore)
    {
        _scoreUI?.UpdateScore(newScore);
    }

    public void ShowComboEffect(int combo)
    {
        _scoreUI?.ShowComboEffect(combo);
    }

    public void DisplayHighScore(int score)
    {
        _scoreUI?.DisplayHighScore(score);
    }
    #endregion

    #region Card UI
    public void UpdateCardSlots(System.Collections.Generic.List<Card> cards)
    {
        _cardSlotUI?.UpdateSlots(cards);
    }

    public void HighlightCard(int index)
    {
        _cardSlotUI?.HighlightSlot(index);
    }

    public void ResetCardSlots()
    {
        _cardSlotUI?.ResetAllSlots();
    }
    #endregion

    #region Menu UI
    public void ShowPauseMenu(bool isActive)
    {
        _menuUI?.ShowPauseMenu(isActive);
    }

    public void ShowResultMenu(int score, int coins)
    {
        _menuUI?.ShowResultMenu(score, coins);
    }

    public void CloseAllMenus()
    {
        _menuUI?.CloseAllPanels();
    }

    public bool IsAnyMenuOpen()
    {
        return _menuUI != null && _menuUI.IsAnyPanelActive();
    }

    public void ShowStoreMenu(bool isActive)
    {
        _menuUI?.ShowStoreMenu(isActive);
    }
    #endregion

    #region Store UI
    public void InitializeStore(System.Collections.Generic.List<StoreBase> stores, StoreManager manager)
{
        // Store the manager instance locally (optional, but good for quick access)
        _storeManager = manager; 
        
        if (_storeUI != null && stores.Count > 0)
        {
            // Pass the manager and the full store list to StoreUI
            _storeUI.InitializeStore(manager, stores); 
            
            // Immediately update currency display
            if (_storeManager.Currency != null)
            {
                 UpdateStoreCurrency(_storeManager.Currency.Coin, _storeManager.Currency.Token, _storeManager.Currency.KeyMap);
            }
        }
    }

    public void UpdateStoreCurrency(int coins, int tokens, int keys)
    {
        _storeUI?.UpdateCurrencyDisplay(coins, tokens, keys);
    }
    #endregion
}
