using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CardManager : MonoBehaviour
{
    #region Fields
    [Header("Card System")]
    [SerializeField] private List<Card> _collectedCards = new();
    [SerializeField] private int _maxCards = 5;

    [Header("Dependencies")]
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private UIEffectCharacter _uiEffectCharacter;
    [SerializeField] private MuscleButton _muscleButton;

    private Player _player;
    private bool _isCardLocked = false;
    private int _careerCardDropCount = 0;
    private const int _maxSameCardInHand = 2;


    public static CardManager Instance { get; private set; }
    public RandomStarterCard starterPanel;
    #endregion


    public bool IsReady { get; private set; }

    #region Unity Lifecycle Card Manager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
       }

    public void Initialize(Player player)
    {
        _player = player;
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots();
        _cardSlotUI?.SetManager(this);

        if (_muscleButton != null)
            _muscleButton.Hide();

        if (_careerSwitcher != null) 
        {
            // 1. สมัคร Event ปกติ
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange; // ป้องกันการสมัครซ้ำ
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;

            // 2. ใช้ ResetCareerDropCycle แทนการใช้ Lambda ที่จัดการยาก
            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle; // ป้องกันการสมัครซ้ำ
            _careerSwitcher.OnResetCareerCycle += ResetCareerDropCycle;
        }
        
        // ตั้งค่าเริ่มต้น
        ResetCareerDropCycle(); 
    }

    // แก้ไข ResetCareerDropCycle() ให้รวม UnlockCards()
    private void ResetCareerDropCycle()
    {
        _careerCardDropCount = 0;
        _isCardLocked = false;          // ปลดล็อกการ์ด
        _cardSlotUI?.UnlockAllSlots();  // ปลดล็อก UI
        _cardSlotUI?.ClearHighlights(); // ดับ Hilight เต็ม 5 ใบ
        _muscleButton?.Hide();          // ซ่อนปุ่ม Muscle
        Debug.Log("[CardManager] Reset career cycle: drop counter = 0, cards unlocked.");
    }

    private void OnDestroy()
    {
        if (_careerSwitcher != null)
        {
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange;
            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle; // Unsubscribe จากเมธอดที่ใช้
        }
    }

    #endregion

        public void SetCareerSwitcher(CareerSwitcher switcher)
    {
        _careerSwitcher = switcher;

        if (_careerSwitcher != null)
        {
            // bind events
            _careerSwitcher.OnCareerChangedEvent -= HandleCareerChange;
            _careerSwitcher.OnCareerChangedEvent += HandleCareerChange;

            _careerSwitcher.OnResetCareerCycle -= ResetCareerDropCycle;
            _careerSwitcher.OnResetCareerCycle += ResetCareerDropCycle;
        }

        Debug.Log($"[CardManager] CareerSwitcher assigned → {switcher != null}");
    }


    #region Add / Remove Card
    public void AddCard(Card newCard)
    {
        // 1. ตรวจสอบเงื่อนไขห้ามเพิ่ม: ถูกล็อก
        if (_isCardLocked)
        {
            Debug.Log("[CardManager] Cannot add card: Card system is locked.");
            return;
        }

        // 2. ตรวจสอบมือเต็ม
        if (_collectedCards.Count >= _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show(); 
            Debug.Log("[CardManager] Hand is full. Card was not added. Showing Muscle Button.");
            return; //เพื่อบล็อกการเพิ่มเมื่อมือเต็ม
        }

        // 3. ถ้าไม่เต็มและไม่ถูกล็อก -> เพิ่มการ์ด
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // 4. ตรวจสอบว่าการเพิ่มทำให้มือเต็มหรือไม่ (Trigger MuscleDuck Mode)
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
            Debug.Log("[CardManager] Hand is now full. Muscle Button is visible.");
        }
    }

    public void RemoveCard(int index)
    {
        if (!IsValidIndex(index)) return;

        _collectedCards.RemoveAt(index);
        _cardSlotUI?.UpdateSlots(_collectedCards);
        _cardSlotUI?.ClearHighlights();
    }
    #endregion


    #region Use Card Logic

    public void OnClickStarterButton()
    {
        starterPanel.OpenPanel();
    }


    public void UseCard(int index)
    {
        if (_isCardLocked || !IsValidIndex(index)) return;

        Card card = _collectedCards[index];
        bool success = false;

        if (card.Type == CardType.Career)
        {
            var data = card.CareerData;
            if (_careerSwitcher != null && data != null && _careerSwitcher.CanChangeTo(data))
            {
                _careerSwitcher.SwitchCareer(data);
                _uiEffectCharacter?.PlayEffect(card.SkillName);
                success = true;
            }
        }
        else if (card.Type == CardType.Berserk)
        {
            ActivateMuscleDuck();
            success = true;
        }
        else
        {
            card.ActivateEffect(_player);
            success = true;
        }

        if (success)
        {
            _cardSlotUI?.PlayUseAnimation(index);
            RemoveCard(index);
        }

        if (_collectedCards.Count < _maxCards)
        {
            _cardSlotUI?.ClearHighlights();
            _muscleButton?.Hide();
        }
    }
    #endregion


    #region Muscle Duck
    public void ActivateMuscleDuck()
    {
        if (_careerSwitcher == null) return;

        // ลบการ์ดทั้ง 5 ใบออกจากเด็คก่อน
        _collectedCards.Clear();
        _cardSlotUI?.ResetAllSlots();

        // ซ่อนปุ่ม Muscle
        _muscleButton?.Hide();

        // สลับอาชีพเป็น Muscle Duck
        _careerSwitcher.SwitchCareerByName("Muscle");
        _careerSwitcher.StartCareerTimer(10f);

        // ล็อกการ์ดระหว่างใช้ Muscle Duck
        LockCards();
    }

    #endregion


    #region Lock / Unlock
    private void LockCards()
    {
        _isCardLocked = true;
        _cardSlotUI?.LockAllSlots();
    }

    private void UnlockCards()
    {
        _isCardLocked = false;
        _cardSlotUI?.UnlockAllSlots();
    }

    private void HandleCareerChange(DuckCareerData newCareer)
    {
        if (newCareer == null || _careerSwitcher == null) return;

        if (newCareer == _careerSwitcher.GetCareerData(DuckCareer.Duckling))
            UnlockCards();
    }
    #endregion

    public void AddStarterCard()
    {
        // 1. ตรวจสอบเงื่อนไขห้ามเพิ่ม: ถูกล็อก หรือ มือเต็ม
        if (_isCardLocked || _collectedCards.Count >= _maxCards)
        {
             // แสดงปุ่ม Muscle และ Highlight เมื่อมือเต็ม (ในกรณีที่ถูกเรียกตอนมือเต็ม)
             if (_collectedCards.Count >= _maxCards)
             {
                _cardSlotUI?.HighlightFullHand();
                _muscleButton?.Show();
             }
             Debug.Log("[CardManager] Cannot add Starter Card. (Locked or Full)");
             return;
        }
        
        // 2. สร้างการ์ดแบบสุ่ม (ใช้ Logic สุ่มเดิม)
        DuckCareer career = GetRandomCareerFromRate();

        if (_careerSwitcher == null) 
        {
            Debug.LogError("[CardManager] CareerSwitcher is not set! Cannot add starter card.");
            return; 
        }
        DuckCareerData careerData = _careerSwitcher.GetCareerData(career);

        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, careerData);

        // 3. เพิ่มการ์ด (ใช้ AddCard ที่แก้ไขแล้ว)
        // AddCard จะจัดการเรื่อง UI และ MuscleButton ต่อไป
        AddCard(newCard); 
        _cardSlotUI?.HighlightSlot(_collectedCards.Count - 1);


        // ★ ไม่ต้องเพิ่ม _careerCardDropCount
        Debug.Log($"[CardManager] Added Starter Card: {careerData.DisplayName}");
    }

    
    #region Drop from GoldenMon
    public void AddCareerCard()
    {
        if (_isCardLocked || _collectedCards.Count >= _maxCards)
        {
            Debug.Log("[CardManager] Cannot add new career card. (Locked or Full)");
            return;
        }

        if (_careerSwitcher == null)
        {
             Debug.LogError("[CardManager] CareerSwitcher dependency is missing. Cannot add career card.");
             return;
        }

        DuckCareer career;
        
        do
        {
            career = GetRandomCareerFromRate();
        } while (CheckCareerCountInHand(career) >= _maxSameCardInHand);
        DuckCareerData careerData = _careerSwitcher.GetCareerData(career);

        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, careerData);

        AddCard(newCard);
        _careerCardDropCount++;

        Debug.Log($"[CardManager] Dropped career card: {careerData.DisplayName} ({_careerCardDropCount}/5)");
}


    /// <summary>
    /// NEW: นับจำนวนการ์ดอาชีพที่ระบุที่มีอยู่ในมือผู้เล่น
    /// </summary>
    private int CheckCareerCountInHand(DuckCareer careerType)
    {
        int count = 0;
        foreach (var card in _collectedCards)
        {
            // ต้องมั่นใจว่า Card มี property Type และ CareerData ที่ใช้ได้
            if (card.Type == CardType.Career && card.CareerData != null && card.CareerData.CareerID == careerType)
            {
                count++;
            }
        }
        return count;
    }

    private DuckCareer GetRandomCareerFromRate() //Random rate only career
    {
        float roll = Random.Range(0f, 100f);
        float sum = 0f;

        // Total 100% (ปรับจาก 82% เดิม)
        // อัตราส่วนใหม่: 16 + 15 + 15 + 12 + 11 + 12 + 12 + 7 = 100
        
        if (roll < (sum += 16f)) return DuckCareer.Dancer;     // 16% (เดิม 13%)
        if (roll < (sum += 15f)) return DuckCareer.Detective;  // 15% (เดิม 12%)
        if (roll < (sum += 15f)) return DuckCareer.Motorcycle; // 15% (เดิม 12%)
        if (roll < (sum += 12f)) return DuckCareer.Chef;       // 12% (เดิม 10%)
        if (roll < (sum += 11f)) return DuckCareer.Firefighter; // 11% (เดิม 9%)
        if (roll < (sum += 12f)) return DuckCareer.Programmer;  // 12% (เดิม 10%)
        if (roll < (sum += 12f)) return DuckCareer.Doctor;     // 12% (เดิม 10%)
        if (roll < (sum += 7f))  return DuckCareer.Singer;      // 7% (เดิม 6%)
        
        // เมื่อรวมกันเป็น 100% แล้ว โค้ดจะไม่สามารถมาถึงบรรทัดนี้ได้
        return DuckCareer.Detective; // Fallback สุดท้าย (หรือ return ค่าอื่นที่เหมาะสม)
    }
    #endregion


    #region Helpers
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _collectedCards.Count;
    }


    #endregion
}
