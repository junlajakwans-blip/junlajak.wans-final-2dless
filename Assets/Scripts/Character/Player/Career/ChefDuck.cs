using UnityEngine;

public class ChefDuck : Player
{
    [SerializeField] private GameObject _panEffect;
    [SerializeField] private float _cookBuffTime = 5f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Chef skill: Cooking Buff!");
        ApplyCookBuff();
    }

    private void ApplyCookBuff()
    {
        if (_panEffect != null)
        {
            Instantiate(_panEffect, transform.position, Quaternion.identity);
        }

        StartCoroutine(CookBuffRoutine());
    }

    private System.Collections.IEnumerator CookBuffRoutine()
    {
        float originalSpeed = _playerData.Speed;
        _playerData.Speed *= 1.5f;
        Debug.Log("Cooking buff applied: speed increased!");

        yield return new WaitForSeconds(_cookBuffTime);

        _playerData.Speed = originalSpeed;
        Debug.Log("Cooking buff ended.");
    }
}
