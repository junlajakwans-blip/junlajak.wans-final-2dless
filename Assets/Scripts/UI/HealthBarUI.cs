using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private Image _healthBarFill;
    [SerializeField] private TextMeshProUGUI _healthText;

    [Header("Runtime Values")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth;
    #endregion

    #region Public Methods
    public void InitializeHealth(int maxHP)
    {
        _maxHealth = maxHP;
        _currentHealth = maxHP;
        UpdateHealth(_currentHealth);
    }

    public void UpdateHealth(int currentHP)
    {
        _currentHealth = Mathf.Clamp(currentHP, 0, _maxHealth);

        if (_healthBarFill != null)
            _healthBarFill.fillAmount = (float)_currentHealth / _maxHealth;

        if (_healthText != null)
            _healthText.text = $"{_currentHealth} / {_maxHealth}";
    }

    public void AnimateDamageEffect()
    {
        Debug.Log(" Damage effect animation triggered!");
    }

    public void AnimateHealEffect()
    {
        Debug.Log(" Heal effect animation triggered!");
    }
    #endregion
}
