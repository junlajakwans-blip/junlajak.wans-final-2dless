using UnityEngine;

public class RandomStarterCard : MonoBehaviour
{
    private bool _alreadyClaimed = false; //Prevent Clicker

    public bool TrySummonCard()
    {
        if (_alreadyClaimed)
        {
            Debug.Log("[StarterCard] Already summoned this round.");
            return false;
        }

        var gm = GameManager.Instance;
        var currency = gm?.GetCurrency();
        if (currency == null)
        {
            Debug.LogError("[StarterCard] Currency missing.");
            return false;
        }

        // Use Token 1
        if (!currency.UseToken(1))
        {
            Debug.Log("[StarterCard] Not enough Token to summon.");
            return false;
        }

        var cardManager = FindFirstObjectByType<CardManager>();
        if (cardManager == null)
        {
            Debug.LogError("[StarterCard] CardManager missing.");
            return false;
        }

        cardManager.AddCareerCard();
        _alreadyClaimed = true;
        Debug.Log("<color=yellow>[StarterCard] Summon Successful: Starting card granted.</color>");
        return true;
    }

    public void ResetForNewGame()
    {
        _alreadyClaimed = false;
    }
}
