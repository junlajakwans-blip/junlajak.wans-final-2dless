using System;
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
    [SerializeField] private DuckCareerData _currentCareer;
    [SerializeField] private DuckCareerData _defaultCareer;

    [Header("Appearance Settings")]
    [SerializeField] private Dictionary<string, Sprite> _careerSprites = new(); 
    [SerializeField] private CharacterRigAnimator _playerAnimator;

    [Header("Career Catalog")]
    [SerializeField] private List<DuckCareerData> _allCareers = new();

    [Header("Timing Settings")]
    [SerializeField, Tooltip("Cooldown (seconds) after reverting to default before switching again")]
    private float _careerCooldown = 3f;

    private bool _isOnCooldown = false;
    private Coroutine _careerTimerRoutine;

    // Events
    public event Action<DuckCareerData> OnCareerChangedEvent;
    public event Action OnRevertToDefaultEvent;

    public DuckCareerData CurrentCareer => _currentCareer;

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
        if (!_CanChangeTo(newCareer))
            return;

        _currentCareer = newCareer;
        ApplyCareerAppearance();
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
        OnCareerChangedEvent?.Invoke(newCareer);
        // TODO: Add animation, SFX, or buff logic here
    }
    #endregion


    #region Logic Methods
    public void ApplyCareerAppearance()
    {
        if (_playerAnimator == null || _currentCareer == null)
            return;

        Debug.Log($"Applying appearance for {_currentCareer.DisplayName}");
        // TODO: Replace animator state or sprite
    }

    public DuckCareerData GetCurrentCareer() => _currentCareer;

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

        StartCoroutine(CooldownRoutine());
        OnRevertToDefaultEvent?.Invoke(); // 🔹 แจ้ง CardManager ปลดล็อกการ์ด
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
