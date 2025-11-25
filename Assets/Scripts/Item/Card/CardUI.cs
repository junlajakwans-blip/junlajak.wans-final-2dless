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

    public void SetCareerData(DuckCareerData data)
    {
        if (data == null) return;

        _careerID = data.CareerID.ToString();
        _type = data.CardType;
        cardImage.sprite = data.CareerCard;
    }


    public string GetID() => _careerID;
    public CardType GetCardType() => _type;
}
