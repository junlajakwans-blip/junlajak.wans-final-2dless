using UnityEngine;

public class MooPingMon : Enemy
{
    [SerializeField] private int _fireDamage = 10;
    [SerializeField] private float _smokeRadius = 3f;
    [SerializeField] private float _detectRange = 8f;

    public void MovePattern()
    {
        Debug.Log($"{name} patrols and sniffs around...");
    }

    public void ThrowSkewer()
    {
        Debug.Log($"{name} throws a skewer at the player!");
    }

    public void FanFire()
    {
        Debug.Log($"{name} spreads fire in a cone!");
    }

    public void OnDefeat()
    {
        Debug.Log($"{name} extinguishes and drops BBQ loot.");
    }
}
