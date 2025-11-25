using UnityEngine;
using System;

[System.Serializable]
public class Currency
{
    public static event Action OnCurrencyChanged;

    [SerializeField] private int _coin;
    [SerializeField] private int _token;
    [SerializeField] private int _keyMap;

    private CareerSwitcher _careerSwitcher;

    // ───────────────────────────── Properties ─────────────────────────────
    public int Coin
    {
        get => _coin;
        set { _coin = Mathf.Max(0, value); OnCurrencyChanged?.Invoke(); }
    }

    public int Token
    {
        get => _token;
        set { _token = Mathf.Max(0, value); OnCurrencyChanged?.Invoke(); }
    }

    public int KeyMap
    {
        get => _keyMap;
        set { _keyMap = Mathf.Max(0, value); OnCurrencyChanged?.Invoke(); }
    }

    // ───────────────────────────── Init ─────────────────────────────
    public void Initialize(CareerSwitcher switcher)
    {
        _careerSwitcher = switcher;
    }

    // ───────────────────────────── Add / Use ─────────────────────────────
    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        bool isMuscle = _careerSwitcher != null &&
                        _careerSwitcher.CurrentCareer.CareerID == DuckCareer.Muscle;

        int final = isMuscle ? Mathf.RoundToInt(amount * 2f) : amount;
        Coin += final;
        Debug.Log($"[Currency] +{final} Coin (Total: {Coin})");
    }

    public bool UseCoin(int amount)
    {
        if (Coin < amount) return false;
        Coin -= amount;
        return true;
    }

    public void AddToken(int amount)
    {
        if (amount <= 0) return;
        Token += amount;
    }

    public bool UseToken(int amount)
    {
        if (Token < amount) return false;
        Token = Mathf.Max(0, Token - amount);
        return true;
    }
    public void AddKey(int amount)
    {
        if (amount <= 0) return;
        KeyMap += amount;
    }

    public bool UseKey(int amount)
    {
        if (KeyMap < amount) return false;
        KeyMap -= amount;
        return true;
    }

    // ───────────────────────────── Utility ─────────────────────────────
    public void ResetAll()
    {
        Coin = 0;
        Token = 0;
        KeyMap = 0;
    }

    public override string ToString()
    {
        return $"Coin: {Coin} | Token: {Token} | KeyMap: {KeyMap}";
    }
}
