using UnityEngine;
using System.Collections.Generic;

public class KahootMon : Enemy
{
    [SerializeField] private List<string> _questionList = new List<string>();
    [SerializeField] private string[] _blockColors = new string[4];
    [SerializeField] private float _attackInterval = 3f;
    [SerializeField] private string _activeQuestion;
    [SerializeField] private Dictionary<Color, string> _statusEffects = new();

    public void ShowQuestion()
    {
        int randomIndex = Random.Range(0, _questionList.Count);
        _activeQuestion = _questionList[randomIndex];
        Debug.Log($"KahootMon asks: {_activeQuestion}");
    }

    public void FireBlock(Color color)
    {
        Debug.Log($"KahootMon fires block of color {color}");
    }

    public void ActivateEffect(Player player)
    {
        Debug.Log($"KahootMon applies quiz effect to {player.name}");
    }

    public void DoubleSpeedMode()
    {
        _speed *= 2;
        Debug.Log($"KahootMon enters DOUBLE SPEED MODE!");
    }
}
