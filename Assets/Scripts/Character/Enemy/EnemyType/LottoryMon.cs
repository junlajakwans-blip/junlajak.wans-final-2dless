using UnityEngine;

public class LotteryMon : Enemy
{
    [SerializeField] private float _luckFactor = 0.25f;
    [SerializeField] private float _curseDuration = 4f;

    public override void Attack()
    {
        Debug.Log($"{name} curses the player with bad luck!");
    }

    public void ApplyBadLuck(Player player)
    {
        Debug.Log($"{player.name} got cursed for {_curseDuration} seconds!");
    }

    public void DropCoinOnDefeat()
    {
        int coinAmount = Random.Range(1, 10);
        Debug.Log($"{name} dropped {coinAmount} coins on defeat!");
    }
}
