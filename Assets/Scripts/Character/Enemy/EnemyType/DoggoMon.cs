using UnityEngine;

public class DoggoMon : Enemy
{
    [SerializeField] private float _chaseSpeed = 5f;
    [SerializeField] private float _barkRange = 3f;
    [SerializeField] private int _biteDamage = 10;

    public override void Move()
    {
        Debug.Log($"{name} runs after the player!");
    }

    public override void Attack()
    {
        Debug.Log($"{name} bites with {_biteDamage} damage!");
    }

    public void Bark()
    {
        Debug.Log($"{name} barks to alert other monsters!");
    }

    public void ChasePlayer(Player player)
    {
        Debug.Log($"{name} chases {player.name} aggressively!");
    }
}
