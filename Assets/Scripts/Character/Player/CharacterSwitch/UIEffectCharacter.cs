using System.Collections.Generic;
using UnityEngine;

public class UIEffectCharacter : MonoBehaviour
{
    [SerializeField] private Dictionary<string, GameObject> _effects = new();
    [SerializeField] private float _effectDuration = 1.5f;
    private GameObject _currentEffect;

    public void RegisterEffect(string name, GameObject effect)
    {
        if (!_effects.ContainsKey(name))
            _effects.Add(name, effect);
    }

    public void PlayEffect(string name)
    {
        if (_effects.TryGetValue(name, out var effect))
        {
            if (_currentEffect != null)
                Destroy(_currentEffect);

            _currentEffect = Instantiate(effect, transform);
            Invoke(nameof(StopCurrentEffect), _effectDuration);
        }
    }

    public void StopCurrentEffect()
    {
        if (_currentEffect != null)
        {
            Destroy(_currentEffect);
            _currentEffect = null;
        }
    }

    public bool IsEffectPlaying()
    {
        return _currentEffect != null;
    }
}
