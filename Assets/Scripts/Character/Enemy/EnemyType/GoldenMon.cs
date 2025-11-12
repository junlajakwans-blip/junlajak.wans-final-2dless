using UnityEngine;

public class GoldenMon : Enemy
{
    [SerializeField] private float _danceDuration = 3f;
    [SerializeField] private int _breakPlatformCount = 2;
    [SerializeField] private int _coinDropMultiplier = 5;

    public override void Move()
    {
        Debug.Log($"{name} dances elegantly across the map!");
    }

    public void DanceAttack()
    {
        Debug.Log($"{name} performs a golden dance attack!");
    }

    public void BreakPlatform()
    {
        Debug.Log($"{name} destroys {_breakPlatformCount} platforms!");
    }

    public void DropGoldenCoins()
    {
        int coins = Random.Range(10, 20) * _coinDropMultiplier;
        Debug.Log($"{name} drops {coins} GOLD coins!");
    }

    public override void Die()
    {
        base.Die();

        // GarunteeDrop 1 Card
        CardManager manager = FindFirstObjectByType<CardManager>();
        if (manager != null)
            manager.AddCareerCard();
        else
            Debug.LogWarning("[GoldenMon] CardManager not found!");

        DropGoldenCoins();
    }
}
