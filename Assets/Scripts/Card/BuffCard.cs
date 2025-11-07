using UnityEngine;

[System.Serializable]
public class BuffCard : Card
{
    #region Fields
    [SerializeField] private string _buffType;  
    [SerializeField] private float _duration;   
    [SerializeField] private float _magnitude;   
    #endregion

    #region Properties
    public string BuffType => _buffType;
    public float Duration => _duration;
    public float Magnitude => _magnitude;
    #endregion

    #region Constructor
    public BuffCard(
        string cardID,
        string skillName,
        string rarity,
        string buffType,
        float duration,
        float magnitude,
        Sprite icon = null)
        : base(cardID, CardType.Buff, skillName, rarity, icon)
    {
        _buffType = buffType;
        _duration = duration;
        _magnitude = magnitude;
    }
    #endregion

    #region Buff Logic
    public void ApplyBuff(Player player)
    {
        Debug.Log($" Applying buff '{_buffType}' (+{_magnitude}) to {player.name} for {_duration}s.");
    }

    public void RemoveBuff(Player player)
    {
        Debug.Log($" Buff '{_buffType}' expired on {player.name}.");
    }
    #endregion

    #region Overrides
    public override void ActivateEffect(Player player)
    {
        Debug.Log($" Activating BuffCard: {BuffType}");
        ApplyBuff(player);
    }

    public override string GetDescription()
    {
        return $"Buff: {_buffType} (+{_magnitude}) for {_duration}s — Rarity: {Rarity}";
    }
    #endregion
}
