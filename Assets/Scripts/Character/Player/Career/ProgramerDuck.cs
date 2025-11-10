using UnityEngine;
using System.Collections;

public class ProgrammerDuck : Player
{
    [SerializeField] private GameObject _codeEffect;
    [SerializeField] private float _hackDuration = 3f;

    public override void UseSkill()
    {
        Debug.Log($"{PlayerName} uses Programmer skill: Hack enemies!");
        FreezeEnemyAI();
    }

    private void FreezeEnemyAI()
    {
        if (_codeEffect != null)
            Instantiate(_codeEffect, transform.position, Quaternion.identity);

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            e.enabled = false;
        }

        StartCoroutine(UnfreezeAfterDelay());
    }

    private System.Collections.IEnumerator UnfreezeAfterDelay()
    {
        yield return new WaitForSeconds(_hackDuration);
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            e.enabled = true;

        Debug.Log("Hack duration ended. Enemies resumed.");
    }
}
