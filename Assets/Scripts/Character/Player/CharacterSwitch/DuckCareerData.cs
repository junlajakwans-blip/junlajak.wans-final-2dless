using UnityEngine;

[CreateAssetMenu(fileName = "NewCareerData", menuName = "DUFFDUCK/Career Data")]

///<summary>
/// Change to ScriptableObeject to Collect Base Stat and Base Cooldown of Carreer in Asset
/// <summary>
public class DuckCareerData : ScriptableObject
{
#region Field
    [Header("Core Identity")]
    [SerializeField] private DuckCareer _careerID;
    [SerializeField] private string _displayName;
    [SerializeField] private string _skillName;
    [SerializeField] private Sprite _skillIcon;


    
    [Header("Base Stats")]
    [SerializeField] private int _baseHealth;
    [SerializeField] private float _baseSpeed;
    [SerializeField] [TextArea(3, 5)] private string _skillDescription;


    [Header("Card Settings")]
    [SerializeField] private CardType _cardType = CardType.Career;
    [SerializeField] private Sprite _careerIcon;
    [SerializeField] private Sprite _careerCard;
    public float BaseCooldown = 10f;// Card Cooldown Base
#endregion


#region Buff Data
    // hefDuck Buff Data
    [Header("ChefDuck Buff Data")]
    [SerializeField] [Tooltip("Min bonus coins for LotteryMon (ChefDuck Buff).")]
    private int _chefMonCoinMinBonusValue = 3; 

    [SerializeField] [Tooltip("Max bonus coins for LotteryMon (ChefDuck Buff).")]
    private int _chefMonCoinMaxBonusValue = 8;


    // DoctorDuck Buff Data
    [Header("DoctorDuck Buff Data")]
    [Tooltip("Chance (0.0 to 1.0) for PeterMon to skip its attack.")]
    [SerializeField] private float _peterMonAttackSkipChance = 0.30f; // 30%
    
    [SerializeField] private float _peterMonBuffDuration = 5f;



    [Header("ProgrammerDuck Buff Data")]
    [Tooltip("Flat coin bonus for LotteryMon (+10).")]
    [SerializeField] private int _programmerMonCoinBonusValue = 10; // ProgrammerDuck: LotteryMon
    [Tooltip("Chance (0.0 to 1.0) for KahootMon to be disabled (25%).")]
    [SerializeField] private float _kahootMonDisableChance = 0.25f;
#endregion



#region Property (Read-Only)

    public DuckCareer CareerID => _careerID;
    public string DisplayName => _displayName;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public string SkillName => _skillName;
    public string SkillDescription => _skillDescription;
    public Sprite SkillIcon => _skillIcon;
    public CardType CardType => _cardType;
    public Sprite CareerIcon => _careerIcon;
    public Sprite CareerCard =>  _careerCard; //All Career and Berserk have card || Duckling NO Card

    //Properties for Buff 
    //ChefDuck
    public int ChefMonCoinMinBonusValue => _chefMonCoinMinBonusValue;
    public int ChefMonCoinMaxBonusValue => _chefMonCoinMaxBonusValue;

    //DoctorDuck 
    public float PeterMonAttackSkipChance => _peterMonAttackSkipChance;
    public float PeterMonBuffDuration => _peterMonBuffDuration;

    // ProgrammerDuck 
    public int ProgrammerMonCoinBonusValue => _programmerMonCoinBonusValue;
    public float KahootMonDisableChance => _kahootMonDisableChance;

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
