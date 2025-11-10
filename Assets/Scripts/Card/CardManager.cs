using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    #region Fields
    [Header("Card System References")]
    [SerializeField] private List<Card> _collectedCards = new List<Card>();
    [SerializeField] private int _maxCards = 5;

    [Header("Dependencies")]
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private UIEffectCharacter _uiEffectCharacter;

    [Header("Runtime State")]
    private bool _isCardLocked = false;   // กันกดการ์ดซ้ำระหว่างใช้อาชีพ
    private Player _player;               // cache reference
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _player = FindObjectOfType<Player>();
        if (_player == null)
            Debug.LogWarning("[CardManager] Player reference not found in scene!");

        if (_cardSlotUI != null)
            _cardSlotUI.UpdateSlots(_collectedCards);  // ใช้ API เดิมของคุณ :contentReference[oaicite:0]{index=0}

        if (_careerSwitcher != null)
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange; // event จาก CareerSwitcher :contentReference[oaicite:1]{index=1}
        else
            Debug.LogWarning("[CardManager] Missing CareerSwitcher reference!");
    }

    private void OnDestroy()
    {
        if (_careerSwitcher != null)
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange; // เลิก subscribe :contentReference[oaicite:2]{index=2}
    }
    #endregion

    #region Public Methods
    public void AddCard(Card newCard)
    {
        if (_collectedCards.Count >= _maxCards)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CardManager] Cannot add more cards — deck is full!");
#endif
            return;
        }

        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);  // อัปเดต slot ตามไฟล์เดิมของคุณ :contentReference[oaicite:3]{index=3}

#if UNITY_EDITOR
        Debug.Log($"[CardManager] Added card: {newCard.SkillName}");
#endif
    }

    public void UseCard(int index)
    {
        if (!IsValidIndex(index)) return;

        if (_isCardLocked)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CardManager] Card usage locked — current career active or in cooldown!");
#endif
            return;
        }

        Card usedCard = _collectedCards[index];
        if (usedCard == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CardManager] Invalid card reference!");
#endif
            return;
        }

        // ตรวจสภาพ CareerSwitcher ว่าพร้อมสลับอาชีพไหม (cooldown/active)
        if (_careerSwitcher != null && !_careerSwitcher.CanSwitchCareer)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[CardManager] Cannot switch career yet (cooldown or active)!");
#endif
            return;
        }

        // ใช้เอฟเฟกต์ UI ตามระบบของคุณ (UIEffectCharacter.PlayEffect(string)) :contentReference[oaicite:4]{index=4}
        usedCard.ActivateEffect(_player);
        _uiEffectCharacter?.PlayEffect(usedCard.SkillName);
        _cardSlotUI?.PlayUseAnimation(index); // trigger Animator ที่ CardSlotUI ของคุณ :contentReference[oaicite:5]{index=5}

        // สลับอาชีพด้วย DuckCareerData จากการ์ด + เริ่มนับเวลา ตาม CareerSwitcher ของคุณ :contentReference[oaicite:6]{index=6}
        if (_careerSwitcher != null && usedCard.DuckCareerData != null)
        {
            _careerSwitcher.SwitchCareer(usedCard.DuckCareerData);
            _careerSwitcher.StartCareerTimer(usedCard.Duration);
            LockAllCards(); // ล็อคการ์ดไว้จนกว่าจะ revert
        }

        OnCardUsed(usedCard);
        RemoveCard(index);

#if UNITY_EDITOR
        Debug.Log($"[CardManager] Used card: {usedCard.SkillName}");
#endif
    }

    public void RemoveCard(int index)
    {
        if (!IsValidIndex(index)) return;

        _collectedCards.RemoveAt(index);
        _cardSlotUI?.UpdateSlots(_collectedCards); // เคลียร์ slot ตามไฟล์เดิมคุณ :contentReference[oaicite:7]{index=7}
    }

    public bool HasFullDeck() => _collectedCards.Count >= _maxCards;

    public void ExchangeForBerserk()
    {
#if UNITY_EDITOR
        Debug.Log("[CardManager] Exchanging cards for Berserk Mode!");
#endif
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots(); // reset ตาม CardSlotUI เดิมของคุณ :contentReference[oaicite:8]{index=8}

        if (_careerSwitcher != null)
        {
            _careerSwitcher.SwitchCareerByName("Muscle"); // helper ใน CareerSwitcher ของคุณ :contentReference[oaicite:9]{index=9}
            _careerSwitcher.StartCareerTimer(10f);
            LockAllCards();
        }
    }
    #endregion

    #region Protected / Virtual Methods
    protected virtual void OnCardUsed(Card usedCard)
    {
#if UNITY_EDITOR
        Debug.Log($"[CardManager] OnCardUsed triggered for {usedCard.SkillName}");
#endif
    }
    #endregion

    #region Private Helpers
    private bool IsValidIndex(int index) => index >= 0 && index < _collectedCards.Count;

    private void LockAllCards()
    {
        _isCardLocked = true;
#if UNITY_EDITOR
        Debug.Log("[CardManager] Cards locked during career duration.");
#endif
    }

    private void UnlockAllCards()
    {
        _isCardLocked = false;
#if UNITY_EDITOR
        Debug.Log("[CardManager] Cards unlocked — ready to use again.");
#endif
    }

    private void HandleCareerChange(DuckCareerData newCareer)
    {
        if (newCareer == null || _careerSwitcher == null) return;

        // เมื่อ revert กลับ Duckling → ปลดล็อกการ์ด
        if (newCareer == _careerSwitcher.GetCareerData(DuckCareer.Duckling)) // helper ใน CareerSwitcher ของคุณ :contentReference[oaicite:10]{index=10}
        {
            UnlockAllCards();
        }
    }
    #endregion
}
