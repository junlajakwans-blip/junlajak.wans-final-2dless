using UnityEngine;
using System.Collections;

public class MuscleDuck : Player
{
    [SerializeField] private GameObject _rageEffect;
    [SerializeField] private float _damageMultiplier = 2f;
    [SerializeField] private float _duration = 5f;

    public override void UseSkill()
    {
        Debug.Log($"{PlayerName} uses Muscle skill: Berserk Mode!");
        StartCoroutine(BerserkRoutine());
    }

    private IEnumerator BerserkRoutine()
    {
        if (_rageEffect != null)
            Instantiate(_rageEffect, transform.position, Quaternion.identity);

        Debug.Log("[MuscleDuck] Berserk buff applied: double damage & speed boost!");
        ApplySpeedModifier(_damageMultiplier, _duration);

        yield return new WaitForSeconds(_duration);

        Debug.Log("[MuscleDuck] Berserk mode ended.");
    }
}
