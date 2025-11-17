using UnityEngine;

[CreateAssetMenu(fileName = "NewCareerData", menuName = "Duck/Career Data")]

///<summary>
/// Change to ScriptableObeject to Collect Base Stat and Base Cooldown of Carreer in Asset
/// <summary>
public class DuckCareerData : ScriptableObject
{
#region Field
    [Header("Core Identity")]
    [SerializeField] private DuckCareer _careerID;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _skillIcon;

    
    [Header("Base Stats")]
    [SerializeField] private int _baseHealth;
    [SerializeField] private float _baseSpeed;
    [SerializeField] [TextArea(3, 5)] private string _skillDescription;


    [Header("Card Settings")] 
    public float BaseCooldown = 10f;// Card Cooldown Base
#endregion

#region Property (Read-Only)

    public DuckCareer CareerID => _careerID;
    public string DisplayName => _displayName;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public string SkillDescription => _skillDescription;

#endregion


#region  methods

    public string GetCareerStats()
    {
        return $"{_displayName}: HP {_baseHealth}, Speed {_baseSpeed}";
    }

    public void ActivateSkill(Player player)
    {
        Debug.Log($"Activate skill for {_careerID}");
    }

    #endregion
}
