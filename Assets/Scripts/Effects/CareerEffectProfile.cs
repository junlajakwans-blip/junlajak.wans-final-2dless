using UnityEngine;

[CreateAssetMenu(menuName = "DUFFDUCK/Career Effect Profile", fileName = "FXProfile_")]
public class CareerEffectProfile : ScriptableObject
{
    [Header("Basic Attacks (W)")]
    public ComicEffectData basicAttackFX;

    [Header("Skill Effects (R)")]
    public ComicEffectData skillFX;

    [Header("Jump Attack (stomp)")]
    public ComicEffectData jumpAttackFX;

    [Header("Getting Hit / Taking Damage")]
    public ComicEffectData hurtFX;

    [Header("Death")]
    public ComicEffectData deathFX;

    [Header("Optional Combo / Charge / Special")]
    public ComicEffectData extraFX;
}
