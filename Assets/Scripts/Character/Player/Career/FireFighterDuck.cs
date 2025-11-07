using UnityEngine;

public class FireFighterDuck : Player
{
    [SerializeField] private GameObject _waterHoseEffect;
    [SerializeField] private float _sprayPower = 5f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Firefighter skill: Water Hose!");
        ApplyFireExtinguishEffect();
    }

    private void ApplyFireExtinguishEffect()
    {
        if (_waterHoseEffect != null)
        {
            GameObject hose = Instantiate(_waterHoseEffect, transform.position, Quaternion.identity);
            Rigidbody2D rb = hose.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(transform.right * _sprayPower, ForceMode2D.Impulse);
        }
    }
}
