using UnityEngine;
using System.Collections;

public class ChefDuck : Player
{
    [SerializeField] private GameObject _panEffect;
    [SerializeField] private float _cookBuffTime = 5f;
    [SerializeField] private float _speedMultiplier = 1.5f;

    public override void UseSkill()
    {
        Debug.Log($"{PlayerName} uses Chef skill: Cooking Buff!");
        StartCoroutine(CookBuffRoutine());
    }

    private IEnumerator CookBuffRoutine()
    {
        // Pan effect
        if (_panEffect != null)
            Instantiate(_panEffect, transform.position, Quaternion.identity);

        // Temporary speed boost
        ApplySpeedModifier(_speedMultiplier, _cookBuffTime);
        Debug.Log("[ChefDuck] Cooking buff applied: increased speed!");

        yield return new WaitForSeconds(_cookBuffTime);
        Debug.Log("[ChefDuck] Cooking buff ended.");
    }
}
