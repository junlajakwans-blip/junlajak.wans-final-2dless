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
        public CareerEffectProfile fxProfile; 
    }

    #region Fields
    [Header("Runtime State")]
    [SerializeField] private DuckCareerData _currentCareer;
    [SerializeField] private DuckCareerData _defaultCareer;

    [Header("Appearance Settings")]
    [SerializeField] private List<CareerBodyMap> _careerBodyMaps = new();
    public IReadOnlyList<CareerBodyMap> CareerBodyMaps => _careerBodyMaps;
    //[SerializeField] private CharacterRigAnimator _playerAnimator;

    [Header("Career Catalog")]
    [SerializeField] private List<DuckCareerData> _allCareers = new();

    [Header("Dependencies")] // เพิ่มส่วนนี้ถ้ายังไม่มี
    [SerializeField] private SpriteRenderer _ducklingRenderer; 
    [SerializeField] private Animator _ducklingAnimator;
    [SerializeField] private ComicEffectPlayer _fxPlayer;


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

        Player player = GetComponent<Player>(); // 🔥 ต้องดึง player มาก่อน

        // 1) Cleanup skill อาชีพเก่า
        if (_currentCareer != null && _currentCareer.CareerSkill != null)
            _currentCareer.CareerSkill.Cleanup(player);

        // 2) เปลี่ยนอาชีพ
        _currentCareer = newCareer;

        // 3) Assign FX Profile จาก Body Map
        var mapEntry = _careerBodyMaps.Find(m => m.careerID == newCareer.CareerID);
        if (mapEntry != null && mapEntry.fxProfile != null)
            player.SetFXProfile(mapEntry.fxProfile);
        else
            player.SetFXProfile(null);

        // 4) Initialize Skill
        _currentCareer.CareerSkill?.Initialize(player);

        // 5) Callback อื่น
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
        var player = GetComponent<Player>();

        _currentCareer = newCareer;

        Debug.Log($"[CareerSwitcher] Changed to career: {newCareer.DisplayName}");
        ApplyCareerAppearance();
        OnCareerChangedEvent?.Invoke(newCareer);
        // TODO: Add animation, SFX, or buff logic here
    }
    #endregion


    #region Logic Methods
#region Logic Methods
    public void ApplyCareerAppearance()
    {
        if (_currentCareer == null)
            return;

        Debug.Log($"Applying appearance for {_currentCareer.DisplayName}");

        var mapEntry = _careerBodyMaps.Find(m => m.careerID == _currentCareer.CareerID);

        // Auto assign duckling renderer / animator
        if (_ducklingRenderer == null)
        {
            _ducklingRenderer = GetComponentInChildren<SpriteRenderer>();
            Debug.LogWarning("[CareerSwitcher] Auto-assigned Duckling SpriteRenderer");
        }
        if (_ducklingAnimator == null)
        {
            _ducklingAnimator = GetComponentInChildren<Animator>();
            Debug.LogWarning("[CareerSwitcher] Auto-assigned Duckling Animator");
        }

        bool isDefault = _currentCareer.CareerID == DuckCareer.Duckling;

        // 🢂 กลับร่าง Duckling (ไม่ต้อง continue logic ใด ๆ)
        if (isDefault)
        {
            if (_activeBody != null)
            {
                Destroy(_activeBody);
                _activeBody = null;
            }

            if (_ducklingRenderer != null) _ducklingRenderer.enabled = true;
            if (_ducklingAnimator != null) _ducklingAnimator.enabled = true;

            Debug.Log("[CareerSwitcher] Reverted to default Duckling appearance.");
            return;
        }

        // 🢂 ถ้าไม่ใช่ Duckling ต้องมี mapEntry
        if (mapEntry == null || mapEntry.bodyPrefab == null)
        {
            Debug.LogError($"[CareerSwitcher] ❌ bodyPrefab missing for {_currentCareer.CareerID}");
            return;
        }

        // ซ่อน Duckling
        if (_ducklingRenderer != null) _ducklingRenderer.enabled = false;
        if (_ducklingAnimator != null) _ducklingAnimator.enabled = false;

        // ลบร่างเก่า (ถ้ามี)
        if (_activeBody != null)
        {
            Destroy(_activeBody);
            _activeBody = null;
        }

        // สร้างร่างใหม่
        GameObject newBody = Instantiate(mapEntry.bodyPrefab, this.transform);
        newBody.transform.localPosition = Vector3.zero;
        newBody.name = mapEntry.bodyPrefab.name;
        _activeBody = newBody;

        // ปิด Collider / Physics
        foreach (var coll in newBody.GetComponentsInChildren<Collider2D>())
            coll.enabled = false;
        if (newBody.TryGetComponent<Rigidbody2D>(out var rb))
            rb.bodyType = RigidbodyType2D.Kinematic;

        // Assign FX Profile (ไม่บังคับว่าต้องมี)
        if (_fxPlayer == null)
            _fxPlayer = GetComponentInChildren<ComicEffectPlayer>();
        if (_fxPlayer != null)
            _fxPlayer.SetFXProfile(mapEntry.fxProfile);

        // เล่น FX แบบปลอดภัย
        TryPlayCareerFX();

        Debug.Log($"[CareerSwitcher] Swapped body to {mapEntry.bodyPrefab.name}.");
    }

    private void TryPlayCareerFX()
    {
        if (_fxPlayer == null || _fxPlayer.Profile == null || _fxPlayer.Profile.switchFX == null)
        {
            Debug.Log($"[CareerSwitcher] ⚠ ไม่มี FX สำหรับอาชีพ {_currentCareer.DisplayName}");
            return;
        }
        ComicEffectManager.Instance.Play(_fxPlayer.Profile.switchFX, transform.position);
    }
#endregion


    private IEnumerator PlaySwitchFXNextFrame()
    {
        yield return null; // wait 1 frame
        if (_fxPlayer != null && _fxPlayer.Profile?.switchFX != null)
            ComicEffectManager.Instance.Play(_fxPlayer.Profile.switchFX, transform.position);
    }

    public DuckCareerData GetCurrentCareer() => _currentCareer;

    public void RevertToDefault()
    {
        if (_defaultCareer == null)
        {
            Debug.LogWarning("[CareerSwitcher] No default career assigned!");
            return;
        }

        // Cleanup Skill ของอาชีพปัจจุบันก่อน revert
        var player = GetComponent<Player>();
        _currentCareer?.CareerSkill?.Cleanup(player);

        // แจ้ง CardManager reset cycle & unlock cards
        OnResetCareerCycle?.Invoke();

        _fxPlayer?.StopAllEffects();
        

        //  หยุด / เคลียร์ FX ของอาชีพที่เพิ่งหมดเวลาก่อน
        if (_fxPlayer != null)
            _fxPlayer.StopAllEffects();

        _currentCareer = _defaultCareer;

        // 🔄 เซ็ต FX Profile ให้กลับเป็น Duckling
        var duckEntry = _careerBodyMaps.Find(m => m.careerID == DuckCareer.Duckling);
        if (duckEntry != null && duckEntry.fxProfile != null)
        _fxPlayer.SetFXProfile(duckEntry.fxProfile);

        ApplyCareerAppearance();

          if (_fxPlayer != null && duckEntry != null && duckEntry.fxProfile != null)
        _fxPlayer.SetFXProfile(duckEntry.fxProfile);
        
        OnCareerChanged(_defaultCareer);

        StartCoroutine(CooldownRoutine());
        OnRevertToDefaultEvent?.Invoke();
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
