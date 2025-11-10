using UnityEngine;
using System.Collections;

public class DancerDuck : Player
{
    [SerializeField] private GameObject _danceEffect;
    [SerializeField] private float _speedBoost = 1.75f;
    [SerializeField] private float _boostDuration = 5f;

    public override void UseSkill()
    {
        Debug.Log($"{PlayerName} uses Dancer skill: Graceful Step!");
        StartCoroutine(SpeedBoostRoutine());
    }

    private IEnumerator SpeedBoostRoutine()
    {
        if (_danceEffect != null)
            Instantiate(_danceEffect, transform.position, Quaternion.identity);

        ApplySpeedModifier(_speedBoost, _boostDuration);
        Debug.Log("[DancerDuck] Speed boosted!");

        yield return new WaitForSeconds(_boostDuration);
        Debug.Log("[DancerDuck] Boost ended.");
    }
}
