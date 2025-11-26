using UnityEngine;

[CreateAssetMenu(menuName = "Career/Skill/DucklingSkill")]
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
        // ให้ Player ใช้ Bonk Attack มาตรฐาน
        // ไม่ override => เผื่อในอนาคตอยากเพิ่มอะไร
        base.PerformAttack(player);
    }

    public override void PerformChargeAttack(Player player) { }
    public override void PerformRangeAttack(Player player, Transform target) { }

    public override void OnTakeDamage(Player player, int dmg) { }
    public override bool OnBeforeDie(Player player) => false;
}
