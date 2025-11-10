using UnityEngine;

public class DoctorDuck : Player
{
    [SerializeField] private GameObject _healEffect;
    [SerializeField] private int _healAmount = 25;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Doctor skill: Heal nearby allies!");
        HealNearbyAllies();
    }

    private void HealNearbyAllies()
    {
        if (_healEffect != null)
            Instantiate(_healEffect, transform.position, Quaternion.identity);

        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var ally in allies)
        {
            Player teammate = ally.GetComponent<Player>();
            if (teammate != null && teammate != this)
            {
                Debug.Log($"Healed ally: {teammate.name}");
            }
        }
    }
}
