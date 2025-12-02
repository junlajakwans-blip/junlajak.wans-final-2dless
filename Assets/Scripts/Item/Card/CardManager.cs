using System.Collections.Generic;
using UnityEngine;
using System.Collections;
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
    private const int _maxSameCardInHand = 2;

    public static CardManager Instance { get; private set; }
    public RandomStarterCard starterPanel;

    public bool IsReady { get; private set; }

    private Coroutine _careerCooldownRoutine;
    private int _muscleUseCount = 0;
    #endregion




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
    /// <summary>
    /// ใช้สำหรับ Monster Drop → สุ่มอาชีพตาม rate และไม่เกิน 2 ใบซ้ำในมือ
    /// </summary>
    public DuckCareerData GetRandomCareerForDrop()
    {
        DuckCareer career;

        // สุ่มจนกว่าอาชีพนี้จะยังไม่เกิน 2 ใบในมือ
        do
        {
            career = GetRandomCareerFromRate();
        }
        while (CheckCareerCountInHand(career) >= _maxSameCardInHand);

        return _careerSwitcher.GetCareerData(career);
    }

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

    public void AddCareerCard_FromDrop(DuckCareerData dataFromPickup)
    {
        // Fallbacks จะไป AddCareerCard() → แล้ว Refresh UI ตอนจบ
        void Finish()
        {
            _cardSlotUI?.UpdateSlots(_collectedCards);
            _cardSlotUI?.ClearHighlights();
        }

        if (dataFromPickup == null)
        {
            Debug.LogWarning("[CardManager] ❌ Drop career NULL → fallback random.");
            AddCareerCard(); // ใช้ระบบสุ่มแทน
            return;
        }

        // 1) มือเต็ม → Token แทน
        if (_collectedCards.Count >= _maxCards)
        {
            _player.AddToken(1);
            Debug.Log("[CardManager] Hand full → Token instead.");
            return;
        }

        // 2) ห้าม Muscle Duck จากการดรอปเด็ดขาด
        if (dataFromPickup.CareerID == DuckCareer.Muscle)
        {
            Debug.Log("[CardManager] ❌ Muscle cannot be dropped → fallback random.");
            AddCareerCard();
            return;
        }

        // 3) จำกัดไม่เกิน 2 ใบต่อ 1 อาชีพ
        if (CheckCareerCountInHand(dataFromPickup.CareerID) >= _maxSameCardInHand)
        {
            Debug.Log($"[CardManager] ⚠ Already have 2 cards of {dataFromPickup.DisplayName} → fallback random.");
            AddCareerCard();
            return;
        }

        Debug.Log($"[CardManager] 🎴 Added dropped card: {(string.IsNullOrWhiteSpace(dataFromPickup.DisplayName) ? dataFromPickup.CareerID.ToString() : dataFromPickup.DisplayName)} | Hand={_collectedCards.Count}/{_maxCards}");

        // 4) ถูกต้อง → เพิ่มการ์ดลงมือ
        string id = System.Guid.NewGuid().ToString();
        Card newCard = new Card(id, dataFromPickup);
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // มือเต็มพอดี → แสดง Muscle Button
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
        }

        Finish();

        Debug.Log($"[CardManager] Hand now = {_collectedCards.Count}/{_maxCards}");

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
                _careerSwitcher.StartCareerTimer(data.CareerDuration); // ⏱ ดึงจาก ScriptableObject
                HandleCareerCooldown(data); // ⛔ ล็อคการ์ดตาม cooldown ของอาชีพนี้
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
        var muscleCareer = _careerSwitcher.GetCareerData(DuckCareer.Muscle);
        _careerSwitcher.SwitchCareer(muscleCareer);

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
        {
            UnlockCards();
            _muscleUseCount = 0; // Reset muscle bonus stack
        }

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

        if (careerData == null)
        {
            Debug.LogError($"[CardManager] ❌ CareerData = NULL for career {career} — _allCareers อาจยังไม่ใส่ตัวนี้ใน CareerSwitcher");
            return;
        }

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
    /// <summary>
    /// ได้รับการ์ดจากการดรอป GoldenMon — ไม่โดน Card Lock บล็อก
    /// ถ้ามือเต็ม → ได้ Token แทน
    /// Muscle Duck → ได้การ์ด + Token (ได้การ์ดก่อน แล้ว Token จะเพิ่มหลังจากเต็ม)
    /// </summary>
    public void AddCareerCard()
    {
        // 1) ถ้ามือเต็ม → ได้ Token แทน
        if (_collectedCards.Count >= _maxCards)
        {
            _player.AddToken(1);
            Debug.Log("[CardManager] Hand full → Drop Token instead of Card.");
            return;
        }

        // 2) สุ่มอาชีพจนกว่าจะไม่เกิน 2 ใบในมือ
        DuckCareer career;
        do
        {
            career = GetRandomCareerFromRate();
        }
        while (CheckCareerCountInHand(career) >= _maxSameCardInHand);

        DuckCareerData data = _careerSwitcher?.GetCareerData(career);
        if (data == null)
        {
            Debug.LogError($"[CardManager] ❌ CareerData is NULL for career {career} — Card not created.");
            return;
        }

        // 3) สร้าง Card object
        string cardID = System.Guid.NewGuid().ToString();
        Card newCard = new Card(cardID, data);

        // 4) เพิ่มเข้ามือ — ❗ แม้ระบบการ์ดจะ Lock ก็ยังต้องเพิ่มได้
        _collectedCards.Add(newCard);
        _cardSlotUI?.UpdateSlots(_collectedCards);

        // 5) ถ้ามือเต็มพอดีหลังเพิ่ม → Highlight + Muscle button
        if (_collectedCards.Count == _maxCards)
        {
            _cardSlotUI?.HighlightFullHand();
            _muscleButton?.Show();
        }

        Debug.Log($"[CardManager] GoldenMon dropped card: {data.DisplayName}");
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

    private void HandleCareerCooldown(DuckCareerData careerData)
    {
        if (careerData.CareerID == DuckCareer.Muscle)
        {
            float time = careerData.CareerDuration + 15f * _muscleUseCount;
            _muscleUseCount++;
            _careerSwitcher.StartCareerTimer(time);
            LockCards();
            return;
        }

        float extraCooldown = careerData.CareerCooldownAfterUse;

        if (_careerCooldownRoutine != null)
            StopCoroutine(_careerCooldownRoutine);

        _careerCooldownRoutine = StartCoroutine(UnlockAfterCooldown(extraCooldown));
    }

    private IEnumerator UnlockAfterCooldown(float time)
    {
        LockCards();
        Debug.Log($"[CardManager] Extra Career cooldown: {time}s");
        yield return new WaitForSeconds(time);
        UnlockCards();
    }

    public void ForceUIRefresh()
    {
        _cardSlotUI?.UpdateSlots(_collectedCards);
    }

    #endregion
}
