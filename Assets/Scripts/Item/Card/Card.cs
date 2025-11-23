using UnityEngine;
using System.Collections;

[System.Serializable]
public class Card
{
    #region Fields
    [SerializeField] private string _cardID;
    [SerializeField] private string _rarity;
    [SerializeField] private DuckCareerData _careerData; 
    
    #endregion

    #region Properties
    public string CardID => _cardID;
    public string Rarity => _rarity;
    public CardType Type => _careerData?.CardType ?? CardType.None;
    public string SkillName => _careerData?.DisplayName ?? "Unknown";
    public Sprite Icon => _careerData?.CareerCard;
    public DuckCareerData CareerData => _careerData;
    
    #endregion

    #region Constructors
    
    /// <summary>
    /// [NEW CONSTRUCTOR] สำหรับการสร้างการ์ดอาชีพโดยตรงจาก DuckCareerData.
    /// </summary>
    public Card(string cardID, DuckCareerData careerData)
    {
        _cardID = cardID;
        _careerData = careerData; 
        _rarity = careerData.CardType.ToString();

    }
    #endregion

 #region Virtual Methods
     public virtual void ActivateEffect(Player player)
    {
        Debug.Log($"Card '{SkillName}' activated! Type: {Type}");
    }

    public virtual string GetDescription()
    {
        // ใช้ Properties ที่ดึงมาจาก DuckCareerData
        return $"{SkillName} [{Type}] — Rarity: {_rarity}"; 
    }
    #endregion
}

