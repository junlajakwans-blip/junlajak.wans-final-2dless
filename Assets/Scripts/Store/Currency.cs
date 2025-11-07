using UnityEngine;


[System.Serializable]
public class Currency
{
    #region Fields
    [SerializeField] private int _coin;
    [SerializeField] private int _token;
    [SerializeField] private int _keyMap;
    #endregion

    #region Properties
    public int Coin { get => _coin; set => _coin = Mathf.Max(0, value); }
    public int Token { get => _token; set => _token = Mathf.Max(0, value); }
    public int KeyMap { get => _keyMap; set => _keyMap = Mathf.Max(0, value); }
    #endregion

    #region Methods
    public void AddCoin(int amount)
    {
        if (amount > 0) _coin += amount;
    }

    public bool UseCoin(int amount)
    {
        if (_coin >= amount)
        {
            _coin -= amount;
            return true;
        }
        return false;
    }

    public void AddToken(int amount)
    {
        if (amount > 0) _token += amount;
    }

    public bool UseToken(int amount)
    {
        if (_token >= amount)
        {
            _token -= amount;
            return true;
        }
        return false;
    }

    public void AddKey(int amount)
    {
        if (amount > 0) _keyMap += amount;
    }

    public bool UseKey(int amount)
    {
        if (_keyMap >= amount)
        {
            _keyMap -= amount;
            return true;
        }
        return false;
    }

    public void ResetAll()
    {
        _coin = 0;
        _token = 0;
        _keyMap = 0;
        Debug.Log("Currency reset complete.");
    }
    #endregion
}
