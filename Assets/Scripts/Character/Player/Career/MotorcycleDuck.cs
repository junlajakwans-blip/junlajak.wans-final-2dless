using UnityEngine;
using System.Collections;

public class MotorcycleDuck : Player
{
    [Header("Motorcycle Skill Settings")]
    [SerializeField] private GameObject _dashEffect;
    [SerializeField] private float _dashMultiplier = 3f;   // เร่ง x3
    [SerializeField] private float _dashDuration = 0.5f;

    private bool _isDashing = false;

    public override void UseSkill()
    {
        Debug.Log($"{PlayerName} activates Motorcycle Dash!");
        if (!_isDashing)
            StartCoroutine(DashRoutine());
    }


    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        if (_dashEffect != null)
            Instantiate(_dashEffect, transform.position, Quaternion.identity);

        // Temporarily boost speed, system will reset after duration
        ApplySpeedModifier(_dashMultiplier, _dashDuration);

        yield return new WaitForSeconds(_dashDuration);
        _isDashing = false;
    }
}
