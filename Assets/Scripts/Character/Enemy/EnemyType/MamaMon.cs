using UnityEngine;

public class MamaMon : Enemy
{
    [SerializeField] private int _noodleCount = 3;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _boilRange = 4f;

    public override void Attack()
    {
        Debug.Log($"{name} throws boiling noodles!");
        ThrowNoodles();
    }

    public void ThrowNoodles()
    {
        Debug.Log($"{_noodleCount} noodle bowls thrown!");
    }

    public void BoilSplash()
    {
        Debug.Log($"{name} creates boiling splash in {_boilRange}m radius!");
    }

    public void RecoverHP()
    {
        base.Heal(10); // Call the base class's Heal method
        Debug.Log($"{name} slurps noodles to heal HP!");
    }
}
