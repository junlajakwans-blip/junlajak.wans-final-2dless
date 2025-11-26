using UnityEngine;

public abstract class CareerSkillBase : ScriptableObject
{
    [Header("FX Profile")]
    public CareerEffectProfile FXProfile;
    /// <summary>
    /// เรียกเมื่อเริ่มอาชีพ
    /// </summary>
    /// <param name="player"></param>
    public virtual void Initialize(Player player) {}    

    /// <summary>
    /// เรียกเมื่อใช้สกิล (QWER / Key Input / Card หรือระบบอื่น ๆ)
    /// </summary>
    public abstract void UseCareerSkill(Player player);

    /// <summary>
    /// เรียกเมื่อ Player ใช้การโจมตีปกติ
    /// </summary>
    public virtual void PerformAttack(Player player) {}

    /// <summary>
    /// เรียกเมื่อ Player กด Charge Attack
    /// power จะถูกเก็บใน Player (_chargePower) ถ้าต้องอ่านจาก Skill
    /// </summary>
    public virtual void PerformChargeAttack(Player player) {}

    /// <summary>
    /// เรียกเมื่อ Player ใช้สกิลโจมตีแบบระยะไกล
    /// </summary>
    public virtual void PerformRangeAttack(Player player, Transform target) {}

    /// <summary>
    /// เรียกทันทีเมื่อ Revert กลับเป็น Duckling
    /// ใช้เพื่อล้าง Buff / Reset Speed / Cancel Coroutine Skill / ปิด FX
    /// </summary>
    public virtual void Cleanup(Player player) {}

    /// <summary>
    /// เรียกเมื่อเข้าสู่ Overdrive Mode (ถ้าเกมมีระบบนี้)
    /// </summary>
    public virtual void OnEnterOverdrive(Player player) {}

    /// <summary>
    /// เรียกเมื่อออกจาก Overdrive Mode
    /// </summary>
    public virtual void OnExitOverdrive(Player player) {}

    /// <summary>
    /// Callback เมื่อโดน damage
    /// </summary>
    /// <param name="player"></param>
    /// <param name="damage"></param>
    public virtual void OnTakeDamage(Player player, int damage) {}  

    /// <summary>
    /// For Career Can Revive
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public virtual bool OnBeforeDie(Player player) => false;
}
