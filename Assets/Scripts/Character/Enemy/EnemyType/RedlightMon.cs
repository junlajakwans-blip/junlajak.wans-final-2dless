using UnityEngine;

public class RedlightMon : Enemy
{
    [SerializeField] private string _signalState = "Green";
    [SerializeField] private float _cooldownTime = 3f;
    [SerializeField] private int _spawnCarCount = 2;

    public override void Attack()
    {
        Debug.Log($"{name} spawns cars to attack!");
        SpawnCarAttack();
    }

    public void SpawnCarAttack()
    {
        Debug.Log($"{_spawnCarCount} cars rush forward!");
    }

    public void SwitchLightState()
    {
        _signalState = _signalState == "Red" ? "Green" : "Red";
        Debug.Log($"Traffic light switched to {_signalState}");
    }

    public void WarnPlayer()
    {
        Debug.Log($"{name} warns player before light changes!");
    }
}
