using System.Collections.Generic;
using UnityEngine;

public class CareerSwitcher : MonoBehaviour
{
    [SerializeField] private DuckCareerData _currentCareer;
    [SerializeField] private Dictionary<string, Sprite> _careerSprites = new();
    [SerializeField] private CharacterRigAnimator _playerAnimator;

    public void SwitchCareer(string careerName)
    {
        Debug.Log($"Switching to career: {careerName}");
        ApplyCareerAppearance();
    }

    public void ApplyCareerAppearance()
    {
        if (_playerAnimator == null || _currentCareer == null)
            return;

        Debug.Log($"Applying appearance for {_currentCareer.DisplayName}");
        _playerAnimator.SetPart("Body", _careerSprites[_currentCareer.DisplayName]);
    }

    public DuckCareerData GetCurrentCareer()
    {
        return _currentCareer;
    }
}
