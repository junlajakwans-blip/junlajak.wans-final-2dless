using UnityEngine;
using UnityEngine.UI;
using DUFFDUCK.EditorTools; 

public class CardUI : MonoBehaviour
{
    [Header("DO NOT ASSIGN — auto linked by CardUI.Set()")]
    [Tooltip("ระบบจะเติมให้อัตโนมัติจาก DuckCareerData.CareerCard — ห้ามใส่เอง")]
    [SerializeField, ReadOnly] private Image cardImage;

    private string _careerID;
    private CardType _type;

    public void Set(DuckCareerData data)
    {
        _careerID = data.CareerID.ToString();
        _type = data.CardType;

        // เติมรูปให้อัตโนมัติ
        cardImage.sprite = data.CareerCard;
    }

    public string GetID() => _careerID;
    public CardType GetCardType() => _type;
}
