using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles switching the player's career and updating appearance accordingly.
/// </summary>
public class CareerSwitcher : MonoBehaviour, ICareerSwitchable
{
    #region Fields
    [Header("Runtime State")]
    [SerializeField] private DuckCareerData _currentCareer;  // Current career of the player
    [SerializeField] private DuckCareerData _defaultCareer;  // Default career (e.g., "Duckling")

    [Header("Appearance Settings")]
    [SerializeField] private Dictionary<string, Sprite> _careerSprites = new(); // key = careerName
    [SerializeField] private CharacterRigAnimator _playerAnimator; // Animator of the Player

    [Header("Career Catalog")]
    [SerializeField] private List<DuckCareerData> _allCareers = new(); // All available careers

    [Header("Timing Settings")]
    [SerializeField, Tooltip("Cooldown (seconds) after reverting to default before switching again")]
    private float _careerCooldown = 3f;

    private bool _isOnCooldown = false;
    private Coroutine _careerTimerRoutine;

    // Event สำหรับ broadcast เมื่อเปลี่ยนอาชีพ
    public event System.Action<DuckCareerData> OnCareerChangedEvent;

    public DuckCareerData CurrentCareer => _currentCareer; // ICareerSwitchable Implementation
    public bool CanSwitchCareer => !_isOnCooldown && _currentCareer == _defaultCareer;
    #endregion


    #region ICareerSwitchable Implementation
    public void SwitchCareer(DuckCareerData newCareer)
    {
        if (!_CanChangeTo(newCareer))
            return;

        _currentCareer = newCareer;
        ApplyCareerAppearance();
        OnCareerChanged(newCareer);
        OnCareerChangedEvent?.Invoke(newCareer);
    }

    public List<DuckCareer> GetAvailableCareers() //list of enum
    {
        var list = new List<DuckCareer>();
        foreach (var career in _allCareers)
            list.Add(career.CareerID); // Return enum
        return list;
    }

    public void OnCareerChanged(DuckCareerData newCareer)
    {
        Debug.Log($"[CareerSwitcher] Changed to career: {newCareer.DisplayName}");
        // TODO: Add UI effect, SFX, or stat buff update here
    }
    #endregion


    #region Logic Methods
    //TODO: Update player appearance based on career
    public void ApplyCareerAppearance()
    {
        if (_playerAnimator == null || _currentCareer == null)
            return;

        Debug.Log($"Applying appearance for {_currentCareer.DisplayName}");
    }

    public DuckCareerData GetCurrentCareer() => _currentCareer; // Get the current career data

    /// <summary>
    /// Revert to default (Duckling) after timer or manually
    /// </summary>
    public void RevertToDefault()
    {
        if (_defaultCareer == null)
        {
            Debug.LogWarning("[CareerSwitcher] No default career assigned!");
            return;
        }

        _currentCareer = _defaultCareer;
        ApplyCareerAppearance();
        OnCareerChanged(_defaultCareer);
        OnCareerChangedEvent?.Invoke(_defaultCareer);

        StartCoroutine(CooldownRoutine());
    }

    /// <summary>
    /// Start timer when card has limited duration
    /// </summary>
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
        Debug.Log("[CareerSwitcher] Cooldown ended. Ready to switch again.");
    }
    #endregion


    #region Helper Methods
    public DuckCareerData GetCareerData(DuckCareer type) //get CarreerData by enum
    {
        return _allCareers.Find(c => c.CareerID == type);
    }

    public void SwitchCareerByName(string careerName)
    {
        var found = _allCareers.Find(c => c.DisplayName == careerName);
        if (found != null)
            SwitchCareer(found);
    }

    private bool _CanChangeTo(DuckCareerData newCareer)
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
    #endregion
}
