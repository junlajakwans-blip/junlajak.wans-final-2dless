using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    #region Fields
    [SerializeField] private string _playerName;
    [SerializeField] private string _selectedCareer;
    [SerializeField] private int _health;
    [SerializeField] private int _maxHealth;
    [SerializeField] private float _speed;
    [SerializeField] private int _collectedCoins;
    [SerializeField] private List<string> _ownedCards = new List<string>();
    [SerializeField] private List<string> _activeBuffs = new List<string>();
    #endregion

    #region Properties
    public string PlayerName { get => _playerName; set => _playerName = value; }
    public string SelectedCareer { get => _selectedCareer; set => _selectedCareer = value; }
    public int Health { get => _health; set => _health = value; }
    public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
    public float Speed { get => _speed; set => _speed = value; }
    public int CollectedCoins { get => _collectedCoins; set => _collectedCoins = value; }
    public List<string> OwnedCards { get => _ownedCards; set => _ownedCards = value; }
    public List<string> ActiveBuffs { get => _activeBuffs; set => _activeBuffs = value; }
    #endregion

    #region Core Methods
    public void TakeDamage(int amount)
    {
        _health = Mathf.Max(0, _health - amount);
        Debug.Log($"{_playerName} took {amount} damage. HP: {_health}/{_maxHealth}");
    }

    public void Heal(int amount)
    {
        _health = Mathf.Min(_maxHealth, _health + amount);
        Debug.Log($"{_playerName} healed {amount}. HP: {_health}/{_maxHealth}");
    }

    public void AddCard(string cardName)
    {
        if (!_ownedCards.Contains(cardName))
        {
            _ownedCards.Add(cardName);
            Debug.Log($"{_playerName} obtained card: {cardName}");
        }
    }

    public void AddBuff(string buffName)
    {
        if (!_activeBuffs.Contains(buffName))
        {
            _activeBuffs.Add(buffName);
            Debug.Log($"{_playerName} gained buff: {buffName}");
        }
    }

    public void ResetPlayerState()
    {
        _health = _maxHealth;
        _collectedCoins = 0;
        _activeBuffs.Clear();
        Debug.Log($"{_playerName} reset to base state.");
    }
    #endregion
}
