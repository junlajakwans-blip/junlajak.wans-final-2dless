using UnityEngine;

[System.Serializable]
public class Card
{
    #region Fields
    [SerializeField] private string _cardID;
    [SerializeField] private CardType _type;
    [SerializeField] private string _skillName;
    [SerializeField] private string _rarity;
    [SerializeField] private Sprite _icon;
    #endregion

    #region Properties
    public string CardID => _cardID;
    public CardType Type => _type;
    public string SkillName => _skillName;
    public string Rarity => _rarity;
    public Sprite Icon => _icon;
    #endregion

    #region Constructors
    public Card(string cardID, CardType type, string skillName, string rarity, Sprite icon = null)
    {
        _cardID = cardID;
        _type = type;
        _skillName = skillName;
        _rarity = rarity;
        _icon = icon;
    }
    #endregion

    #region Virtual Methods
     public virtual void ActivateEffect(Player player)
    {
        Debug.Log($"Card '{_skillName}' activated! Type: {_type}");
    }

    public virtual string GetDescription()
    {
        return $"{_skillName} [{_type}] — Rarity: {_rarity}";
    }
    #endregion
}
