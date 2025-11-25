using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles switching the player's career and updating appearance accordingly.
/// </summary>
public class CareerSwitcher : MonoBehaviour, ICareerSwitchable
{
    [System.Serializable]
    public class CareerBodyMap
    {
        public DuckCareer careerID;
        public GameObject bodyPrefab;
    }

    #region Fields
    [Header("Runtime State")]
    [SerializeField] private DuckCareerData _currentCareer;
    [SerializeField] private DuckCareerData _defaultCareer;

    [Header("Appearance Settings")]
    [SerializeField] private List<CareerBodyMap> _careerBodyMaps = new();
    //[SerializeField] private CharacterRigAnimator _playerAnimator;

    [Header("Career Catalog")]
    [SerializeField] private List<DuckCareerData> _allCareers = new();

    [Header("Dependencies")] // เพิ่มส่วนนี้ถ้ายังไม่มี
    [SerializeField] private SpriteRenderer _ducklingRenderer; 
    [SerializeField] private Animator _ducklingAnimator;

    [Header("Timing Settings")]
    [SerializeField, Tooltip("Cooldown (seconds) after reverting to default before switching again")]
    private float _careerCooldown = 15f;

    private GameObject _activeBody;
    private bool _isOnCooldown = false;
    private Coroutine _careerTimerRoutine;

    // Events
    public event Action<DuckCareerData> OnCareerChangedEvent;
    public event Action OnRevertToDefaultEvent;
    public event Action OnResetCareerCycle;

    public DuckCareerData CurrentCareer => _currentCareer;


    private void Start()
    {
        // ตั้งค่า _currentCareer เป็น _defaultCareer ตั้งแต่ Awake/Start (ตามที่คุณทำแล้ว)
        // เรียกใช้ ApplyCareerAppearance() ครั้งแรก เพื่อโชว์ Duckling ดั้งเดิม
        if (_currentCareer == null) _currentCareer = _defaultCareer;
        ApplyCareerAppearance();
    }


    /// <summary>
    /// Checks if the current active career is the default Duckling.
    /// </summary>
    public bool IsDuckling
    {
        get
        {
            if (_currentCareer == null)
            {
                // If no career is set, check against the default data
                return _defaultCareer != null && _defaultCareer.CareerID == DuckCareer.Duckling;
            }
            // Check the currently active career's ID
            return _currentCareer.CareerID == DuckCareer.Duckling;
        }
    }
    #endregion


    #region ICareerSwitchable Implementation
    public void SwitchCareer(DuckCareerData newCareer)
    {
        if (!CanChangeTo(newCareer))
            return;

        _currentCareer = newCareer;

        OnCareerChanged(newCareer);
    }

    public List<DuckCareer> GetAvailableCareers()
    {
        var list = new List<DuckCareer>();
        foreach (var career in _allCareers)
            list.Add(career.CareerID);
        return list;
    }

    public void OnCareerChanged(DuckCareerData newCareer)
    {
        Debug.Log($"[CareerSwitcher] Changed to career: {newCareer.DisplayName}");
        ApplyCareerAppearance();
        OnCareerChangedEvent?.Invoke(newCareer);
        // TODO: Add animation, SFX, or buff logic here
    }
    #endregion


    #region Logic Methods
    public void ApplyCareerAppearance()
    {
        if (_currentCareer == null)
            return;

        Debug.Log($"Applying appearance for {_currentCareer.DisplayName}");

        // 1. หา Prefab ของอาชีพปัจจุบันจาก Map โดยใช้ CareerID
        var mapEntry = _careerBodyMaps.Find(m => m.careerID == _currentCareer.CareerID);

        if (mapEntry == null || mapEntry.bodyPrefab == null)
        {
            Debug.LogError($"[CareerSwitcher] Prefab for career {_currentCareer.DisplayName} (ID: {_currentCareer.CareerID}) not found in map!");
            return;
        }

        // ปรับปรุง: การทำลาย/Despawn ร่างเก่า (ป้องกันการซ้อนทับ) ---
        if (_activeBody != null)
        {
            // ปิดการทำงานทันทีเพื่อป้องกันการแสดงผลและการชนซ้ำซ้อน
            _activeBody.SetActive(false); //
            
            // ทำลายร่างเก่าที่แสดงผลอยู่
            Destroy(_activeBody); 
            _activeBody = null; 
        }

        bool isDefault = _currentCareer.CareerID == DuckCareer.Duckling;

        if (isDefault)
        {
            // ถ้าเป็น Duckling: เปิดการแสดงผลของ Duckling ดั้งเดิม (Parent)
            if (_ducklingRenderer != null) _ducklingRenderer.enabled = true;
            if (_ducklingAnimator != null) _ducklingAnimator.enabled = true;
            
            Debug.Log($"[CareerSwitcher] Reverted to default Duckling appearance.");
            return; // ⭐️ หยุดการทำงาน: Duckling ไม่ใช่ Prefab ที่ถูก Instantiate
        }
        
        // 3. ถ้าเป็นอาชีพอื่น: ซ่อน Duckling ดั้งเดิม (Parent) และสร้างร่างใหม่ (Child)
        if (_ducklingRenderer != null) _ducklingRenderer.enabled = false;
        if (_ducklingAnimator != null) _ducklingAnimator.enabled = false;
        
        // 4. สร้างร่างใหม่ (Prefab) และผูกติดกับ Player (this.transform)
        GameObject newBody = Instantiate(mapEntry.bodyPrefab, this.transform);
        newBody.transform.localPosition = Vector3.zero; 
        newBody.name = mapEntry.bodyPrefab.name; // ตั้งชื่อเพื่อความสะอาด

        _activeBody = newBody;

        //  5. ปรับปรุง: ตั้งค่า Component (ควบคุมฟิสิกส์/การชน) ---
        
        // 5a. ปิด Collider 2D ทั้งหมดในร่างใหม่ทันที เพื่อป้องกันการชนกับ Player หลัก
        // (Player หลักมี Collider และ Rigidbody2D ที่ใช้ควบคุมอยู่แล้ว)
        foreach (var coll in newBody.GetComponentsInChildren<Collider2D>())
        {
            // เว้น Collider ของ PlayerInteract (ถ้ามีการย้าย Collider สำหรับ Interact ไปไว้บน Child) 
            // แต่เพื่อความปลอดภัย ให้ปิดทั้งหมดก่อน หาก Collider สำหรับ Interact อยู่บน Parent (Player.cs)
            coll.enabled = false; 
        }

        // 5b. ตรวจสอบ Rigidbody2D เพื่อไม่ให้ Physics Engine ควบคุม
        if (newBody.TryGetComponent<Rigidbody2D>(out var rb))
        {
            // ตั้งเป็น Kinematic เพื่อให้รับรู้การชน (ถ้าจำเป็น) แต่ไม่ถูกควบคุมโดยแรงภายนอก 
            // หรือตั้งเป็น None ถ้าไม่ต้องการให้รับรู้การชนเลย
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        Debug.Log($"[CareerSwitcher] Swapped body to {mapEntry.bodyPrefab.name}.");
    }

    public DuckCareerData GetCurrentCareer() => _currentCareer;

    public void RevertToDefault()
    {
        if (_defaultCareer == null)
        {
            Debug.LogWarning("[CareerSwitcher] No default career assigned!");
            return;
        }

        // แจ้ง CardManager reset cycle & unlock cards
        OnResetCareerCycle?.Invoke();

        _currentCareer = _defaultCareer;
        ApplyCareerAppearance();
        OnCareerChanged(_defaultCareer);

        StartCoroutine(CooldownRoutine());
        OnRevertToDefaultEvent?.Invoke(); // แจ้ง revert สำหรับ UI 

    }


    public void StartCareerTimer(float duration)
    {
        if (_careerTimerRoutine != null)
            StopCoroutine(_careerTimerRoutine);

        _careerTimerRoutine = StartCoroutine(CareerTimerRoutine(duration));
    }

    private IEnumerator CareerTimerRoutine(float duration)
    {
        Debug.Log($"[CareerSwitcher] {_currentCareer.DisplayName} active for {duration} seconds...");
        yield return new WaitForSeconds(duration);
        RevertToDefault();
    }

    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        Debug.Log($"[CareerSwitcher] Cooldown {_careerCooldown}s...");
        yield return new WaitForSeconds(_careerCooldown);
        _isOnCooldown = false;
        Debug.Log("[CareerSwitcher] Cooldown ended.");
    }
    #endregion


    #region Helper Methods
    public bool CanChangeTo(DuckCareerData newCareer)
    {
        if (_isOnCooldown)
        {
            Debug.LogWarning("[CareerSwitcher] Can't switch yet — on cooldown!");
            return false;
        }
        if (newCareer == null)
        {
            Debug.LogWarning("[CareerSwitcher] newCareer is null!");
            return false;
        }
        if (newCareer == _currentCareer)
        {
            Debug.LogWarning("[CareerSwitcher] Already in this career!");
            return false;
        }
        return true;
    }

    public DuckCareerData GetCareerData(DuckCareer type)
    {
        return _allCareers.Find(c => c.CareerID == type);
    }

    public DuckCareerData GetCareerDataByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || _allCareers == null) return null;

        var data = _allCareers.Find(c =>
            string.Equals(c.DisplayName, name, StringComparison.OrdinalIgnoreCase));
        if (data != null) return data;

        if (Enum.TryParse<DuckCareer>(name, true, out var careerEnum))
            return GetCareerData(careerEnum);

        return null;
    }

    public void SwitchCareerByName(string careerName)
    {
        var found = _allCareers.Find(c => c.DisplayName == careerName);
        if (found != null)
            SwitchCareer(found);
    }

    #endregion
}
