using UnityEngine;

public class DancerDuck : Player
{
    [SerializeField] private GameObject _danceEffect;
    [SerializeField] private float _comboBoost = 1.3f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Dancer skill: Evade Attack!");
        EvadeIncomingAttack();
    }

    private void EvadeIncomingAttack()
    {
        if (_danceEffect != null)
            Instantiate(_danceEffect, transform.position, Quaternion.identity);

        StartCoroutine(EvadeRoutine());
    }

    private System.Collections.IEnumerator EvadeRoutine()
    {
        _isInvincible = true;
        _moveSpeed *= _comboBoost;
        yield return new WaitForSeconds(2f);
        _isInvincible = false;
        _moveSpeed /= _comboBoost;
        Debug.Log("Evade buff ended.");
    }
}
