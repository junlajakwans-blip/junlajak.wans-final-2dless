using UnityEngine;


[System.Serializable]
public class Currency
{
    #region Economy Settings
    public const int COIN_PER_TOKEN = 399;   // Conin to token for random starter card
    public const int COIN_PER_KEY   = 799;   // Use coin to exchange for Key to unlock map
    public const int KEYS_PER_MAP   = 3;     // Keys required to unlock a new map
    public const float MUSCLE_MULTIPLIER = 2f; // Multiplier for coins earned while being MuscleDuck

    public static int COIN_PER_MAP => COIN_PER_KEY * KEYS_PER_MAP; // Total coin cost to unlock a map
    #endregion


    #region Fields
    [SerializeField] private int _coin;
    [SerializeField] private int _token;
    [SerializeField] private int _keyMap;
    #endregion

    #region Properties
    public int Coin { get => _coin; set => _coin = Mathf.Max(0, value); }
    public int Token { get => _token; set => _token = Mathf.Max(0, value); }
    public int KeyMap { get => _keyMap; set => _keyMap = Mathf.Max(0, value); }

    private CareerSwitcher _careerSwitcher; // reference to check current career
    #endregion


#region Initialization
    public void Initialize(CareerSwitcher switcher)
    {
        _careerSwitcher = switcher;
    }
#endregion

    #region Methods
    public void AddCoin(int amount) //add coin when collect during gameplay
    {
        if (amount <= 0) return;

        bool isMuscle = _careerSwitcher != null &&
                        _careerSwitcher.CurrentCareer.CareerID == DuckCareer.Muscle;

        int final = isMuscle ? Mathf.RoundToInt(amount * MUSCLE_MULTIPLIER) : amount;
        Coin += final;

        Debug.Log($"[Currency] +{final} coin (Total: {Coin}) | MuscleMode={isMuscle}");
    }

    /// <summary> 
    /// Use coin for Upgrade
    /// Use coin for Exchange token for Random Starter Card
    /// Use coin for Exchange Key for Map
    /// </summary>
    public bool UseCoin(int amount)
    {
        if (Coin < amount)
        {
            Debug.LogWarning($"[Currency] Not enough coin! Need {amount}, have {Coin}");
            return false;
        }

        Coin -= amount;
        Debug.Log($"[Currency] Used {amount} coin (Remaining: {Coin})");
        return true;
    }

    public void AddToken(int amount) // Add token when exchange from coin
    {
        if (amount <= 0) return;
        Token += amount;
        Debug.Log($"[Currency] +{amount} token (Total: {Token})");
    }

    public bool UseToken(int amount) // Use token to get random starter card
    {
        if (Token < amount)
        {
            Debug.LogWarning($"[Currency] Not enough token! Need {amount}, have {Token}");
            return false;
        }

        Token -= amount;
        Debug.Log($"[Currency] Used {amount} token (Remaining: {Token})");
        return true;
    }

    public void AddKey(int amount) // Add key when exchange from coin
    {
        if (amount <= 0) return;
        KeyMap += amount;
        Debug.Log($"[Currency] +{amount} key (Total: {KeyMap})");
    }

    public bool UseKey(int amount) // Use key to unlock map
    {
        if (KeyMap < amount) // Check enough key
        {
            Debug.LogWarning($"[Currency] Not enough key! Need {amount}, have {KeyMap}");
            return false;
        }

        KeyMap -= amount;
        Debug.Log($"[Currency] Used {amount} key (Remaining: {KeyMap})");
        return true;
    }
    #endregion

    #region Exchange System
    /// <summary>
    /// Exchange Coin → Token (399 Coin per Token)
    /// </summary>
    public bool ExchangeCoinToToken(int count = 1)
    {
        int cost = COIN_PER_TOKEN * Mathf.Max(1, count);
        if (!UseCoin(cost)) return false;

        AddToken(count);
        Debug.Log($"[Exchange] {cost} coin → {count} token");
        return true;
    }

    /// <summary>
    /// Exchange Coin → KeyMap (799 Coin per Key)
    /// </summary>
    public bool ExchangeCoinToKey(int count = 1)
    {
        int cost = COIN_PER_KEY * Mathf.Max(1, count);
        if (!UseCoin(cost)) return false;

        AddKey(count);
        Debug.Log($"[Exchange] {cost} coin → {count} key");
        return true;
    }

    /// <summary>
    /// Need 3 KeyMap to unlock 1 Map
    /// </summary>
    public bool UnlockMap()
    {
        if (KeyMap < KEYS_PER_MAP)
        {
            Debug.LogWarning($"[Currency] Not enough keys to unlock map! ({KeyMap}/{KEYS_PER_MAP})");
            return false;
        }

        KeyMap -= KEYS_PER_MAP;
        Debug.Log($"[Exchange] Used {KEYS_PER_MAP} keys → Unlocked 1 map!");
        return true;
    }
    #endregion


    #region Utility
    public void ResetAll()
    {
        Coin = 0;
        Token = 0;
        KeyMap = 0;
        Debug.Log("[Currency] Reset complete.");
    }

    public override string ToString()
    {
        return $"Coin: {Coin} | Token: {Token} | KeyMap: {KeyMap}";
    }
    #endregion
}