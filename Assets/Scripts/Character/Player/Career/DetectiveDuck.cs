using UnityEngine;

public class DetectiveDuck : Player
{
    [SerializeField] private GameObject _magnifyGlassEffect;
    [SerializeField] private float _scanRadius = 5f;

    public override void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} uses Detective skill: Scan Area!");
        RevealHiddenItems();
    }

    private void RevealHiddenItems()
    {
        if (_magnifyGlassEffect != null)
            Instantiate(_magnifyGlassEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _scanRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("HiddenItem"))
                Debug.Log($"Revealed hidden item: {hit.name}");
        }
    }
}
