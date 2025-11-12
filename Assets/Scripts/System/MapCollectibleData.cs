using UnityEngine;

[System.Serializable]
public class MapCollectibleData
{
    #region Fields
    [Header("Collectible Info")]
    [SerializeField] private string _collectibleID;     
    [SerializeField] private string _collectibleName;   
    [SerializeField] private CollectibleType _type;      
    [SerializeField] private RarityLevel _rarity;      
    [SerializeField] private int _rewardValue;     

    [Header("Visual / Prefab Reference")]
    [SerializeField] private GameObject _prefab;     
    [SerializeField] private Sprite _icon;     
    #endregion

    #region Properties
    public string ID => _collectibleID;
    public string Name => _collectibleName;
    public CollectibleType Type => _type;
    public RarityLevel Rarity => _rarity;
    public int RewardValue => _rewardValue;
    public GameObject Prefab => _prefab;
    public Sprite Icon => _icon;
    #endregion
}

public enum RarityLevel
{
    Common,
    Rare,
    Epic,
    Legendary
}
