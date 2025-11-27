using UnityEngine;

/// <summary>
/// Duckling – Default starter career (ID 0)
/// No Skill, No BuffMon, No BuffMap.
/// Player can only perform basic attacks (Bonk Attack / Jump Attack).
/// </summary>
[CreateAssetMenu(menuName = "DUFFDUCK/Skill/DucklingSkill")]
public class DucklingSkill : CareerSkillBase
{
    #region Summary
    // Duckling – Default starter career (ID 0)
    // No Skill, No BuffMon, No BuffMap.
    // Player can only perform basic attacks (Bonk Attack / Jump Attack).
    #endregion

    public override void Initialize(Player player)
    {
        // ไม่มี Buff อะไรให้เปิด
        // ไม่มี Coroutine
    }

    public override void Cleanup(Player player)
    {
        // ไม่มีอะไรต้อง revert
    }

    public override void UseCareerSkill(Player player)
    {
        // Duckling ไม่มีสกิลกดใช้
        Debug.Log("[Duckling] No active skill.");
    }

    public override void PerformAttack(Player player)
    {
        // ---------------------------------------------------------
        // PlayFX Attack 
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.basicAttackFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);
        }
        // ---------------------------------------------------------

        // Overlap 1 block
        float range = 1.2f;
        Vector2 origin = player.transform.position + new Vector3(player.FaceDir * 0.8f, 0f, 0f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
            {
                player.ApplyDamage(target, 10);
            }
        }

        Debug.Log("[Duckling] Bonk Attack!");
    }

    // Duckling ไม่มี Charge/Range Attack จึงปล่อยว่างไว้ ไม่ต้องใส่ Effect
    public override void PerformChargeAttack(Player player) { }
    public override void PerformRangeAttack(Player player, Transform target) { }

    public override void OnTakeDamage(Player player, int dmg) { }
    public override bool OnBeforeDie(Player player) => false;
}