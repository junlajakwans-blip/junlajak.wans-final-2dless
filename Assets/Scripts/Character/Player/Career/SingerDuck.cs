using UnityEngine;

public class SingerDuck : Player
{
    [SerializeField] private GameObject _microphoneEffect;
    [SerializeField] private float _soundWaveRadius = 4f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Singer skill: Sonic Stun!");
        StunEnemiesInRange();
    }

    private void StunEnemiesInRange()
    {
        if (_microphoneEffect != null)
            Instantiate(_microphoneEffect, transform.position, Quaternion.identity);

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, _soundWaveRadius);
        foreach (var hit in enemies)
        {
            Enemy e = hit.GetComponent<Enemy>();
            if (e != null)
                Debug.Log($"Enemy {e.name} stunned!");
        }
    }
}
