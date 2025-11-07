using UnityEngine;

public class MuscleDuck : Player
{
    [SerializeField] private GameObject _rageEffect;
    [SerializeField] private float _damageMultiplier = 2f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Muscle skill: Berserk Mode!");
        ActivateBerserkMode();
    }

    private void ActivateBerserkMode()
    {
        if (_rageEffect != null)
            Instantiate(_rageEffect, transform.position, Quaternion.identity);

        StartCoroutine(BerserkRoutine());
    }

    private System.Collections.IEnumerator BerserkRoutine()
    {
        float originalDamage = _currentCareer.BaseSpeed;
        _currentCareer.BaseSpeed *= _damageMultiplier;

        yield return new WaitForSeconds(5f);

        _currentCareer.BaseSpeed = originalDamage;
        Debug.Log("Berserk mode ended.");
    }
}
