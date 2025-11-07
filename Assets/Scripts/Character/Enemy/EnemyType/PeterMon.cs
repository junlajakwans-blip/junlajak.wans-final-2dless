using UnityEngine;

public class PeterMon : Enemy
{
    [SerializeField] private float _flySpeed = 7f;
    [SerializeField] private int _attackDropDamage = 20;
    [SerializeField] private float _hoverHeight = 5f;

    public override void Move()
    {
        Debug.Log($"{name} hovers at height {_hoverHeight}");
    }

    public override void Attack()
    {
        Debug.Log($"{name} dive-bombs the player!");
        DropAttack();
    }

    public void FlyPattern()
    {
        Debug.Log($"{name} circles overhead in a pattern.");
    }

    public void DropAttack()
    {
        Debug.Log($"{name} performs a dropping attack dealing {_attackDropDamage} damage!");
    }
}
