using UnityEngine;

public class RandomStarterCard : MonoBehaviour
{
    #region Fields
    private CardManager _cardManagerRef;
    private GameManager _gmRef;
    private bool _alreadyClaimed = false; //Prevent Clicker
    #endregion


    // Injection Method
    public void SetDependencies(CardManager manager, GameManager gm)
    {
        _cardManagerRef = manager;
        _gmRef = gm;
    }   

    public bool TrySummonCard()
    {
        // 1. ตรวจสอบการอ้างสิทธิ์ซ้ำ (ให้แค่ 1 ครั้งต่อเกม)
        if (_alreadyClaimed)
        {
            Debug.Log("[StarterCard] Already summoned this round.");
            return false;
        }

        // 2. ตรวจสอบและดึง Reference (GM + Currency)
        var gm = _gmRef;
        var currency = gm?.GetCurrency();
        
        if (currency == null)
        {
            Debug.LogError("[StarterCard] Currency missing or GameManager not initialized.");
            return false;
        }

        // 3. ใช้ Token 1 หน่วย
        if (!currency.UseToken(1))
        {
            Debug.Log("[StarterCard] Not enough Token to summon (Requires 1 Token).");
            return false;
        }

        // 4. ตรวจสอบ CardManager (ใช้ Reference ที่ถูก Inject)
        var cardManager = _cardManagerRef;  
        if (cardManager == null)
            {
                Debug.LogError("[StarterCard] CardManager missing. (Injection Failed)");
                return false;
            }

        // 5. [FIXED] เพิ่มการ์ดอาชีพแบบ Starter
        // เรียก AddStarterCard() ซึ่งเป็นเมธอดที่สร้างการ์ด Career โดยไม่นับรวมในเคาน์เตอร์จำกัด
        cardManager.AddStarterCard(); // ★ เปลี่ยนไปใช้ AddStarterCard()
        
        _alreadyClaimed = true;
        Debug.Log("<color=yellow>[StarterCard] Summon Successful: Starting card granted.</color>");
        return true;
    }

    public void ResetForNewGame()
    {
        _alreadyClaimed = false;
    }
}