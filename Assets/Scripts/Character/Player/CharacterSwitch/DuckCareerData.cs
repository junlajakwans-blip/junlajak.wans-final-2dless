using UnityEngine;

[System.Serializable]
public class DuckCareerData
{
    [SerializeField] private DuckCareer _careerID;
    [SerializeField] private string _displayName;
    [SerializeField] private int _baseHealth;
    [SerializeField] private float _baseSpeed;
    [SerializeField] private string _skillDescription;
    [SerializeField] private Sprite _skillIcon;

    public DuckCareer CareerID => _careerID;
    public string DisplayName => _displayName;
    public int BaseHealth => _baseHealth;
    public float BaseSpeed => _baseSpeed;
    public string SkillDescription => _skillDescription;
    public Sprite SkillIcon => _skillIcon;

    public string GetCareerStats()
    {
        return $"{_displayName}: HP {_baseHealth}, Speed {_baseSpeed}";
    }

    public void ActivateSkill(Player player)
    {
        Debug.Log($"Activate skill for {_careerID}");
    }
}
