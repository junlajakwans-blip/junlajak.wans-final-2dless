using UnityEngine;

public class CardUI : MonoBehaviour
{
    [Header("Auto-link: SpriteRenderer on world card")]
    [SerializeField] private SpriteRenderer cardSprite;

    private string _careerID;
    private CardType _type;

    public void SetCareerData(DuckCareerData data)
    {
        if (data == null) return;

        // auto find
        if (cardSprite == null)
            cardSprite = GetComponentInChildren<SpriteRenderer>();

        _careerID = data.CareerID.ToString();
        _type = data.CardType;

        if (cardSprite != null)
        {
            cardSprite.sprite = data.CareerCard;
            cardSprite.enabled = true; // บังคับเปิดทันที
        }

        var img = GetComponentInChildren<UnityEngine.UI.Image>();
        if (img != null) img.enabled = false;

        Debug.Log($"[CardUI] SetCareerData: {data.DisplayName}");
    }

    public string GetCareerID() => _careerID;
    public CardType GetCardType() => _type;
}
