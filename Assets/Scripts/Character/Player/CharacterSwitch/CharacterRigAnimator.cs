using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin wrapper/adapter for Animator to support Character Rig system
/// Supports methods used by Player: SetTrigger / SetMoveAnimation / ResetAllTriggers
/// Also retains original UML functions (SetRigPart, SetForm, PlayAnimation, ResetRig)
/// </summary>
public class CharacterRigAnimator : MonoBehaviour
{
    [Header("Rig Components")]
    [SerializeField] private Animator _animationController;
    [SerializeField] private Dictionary<string, SpriteRenderer> _bodyParts = new();

    [Header("Form State")]
    [SerializeField] private bool _isAdultForm = false;

    // Parameter names used in Animator (adjust to match your project's Animator Controller)
    private static readonly int HASH_IS_ADULT  = Animator.StringToHash("IsAdult");
    private static readonly int HASH_IS_MOVING = Animator.StringToHash("IsMoving");
    private static readonly int HASH_SPEED_X   = Animator.StringToHash("SpeedX");

    private void Awake()
    {
        if (_animationController == null)
            _animationController = GetComponent<Animator>();

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!_bodyParts.ContainsKey(sr.name))
                _bodyParts.Add(sr.name, sr);
        }
    }
    public void SetRigPart(string partName, Sprite sprite)
    {
        if (_bodyParts.TryGetValue(partName, out var sr))
            sr.sprite = sprite;
        else
            Debug.LogWarning($"[RigAnimator] No body part: {partName}");
    }

    public void SetForm(bool isAdult)
    {
        _isAdultForm = isAdult;
        if (_animationController != null)
            _animationController.SetBool(HASH_IS_ADULT, _isAdultForm);
    }

    public void PlayAnimation(string animName)
    {
        if (_animationController != null && !string.IsNullOrEmpty(animName))
            _animationController.Play(animName);
    }

    public void ResetRig()
    {
        foreach (var sr in _bodyParts.Values) sr.sprite = null;
        if (_animationController != null)
        {
            _animationController.Rebind();
            _animationController.Update(0f);
        }
        _isAdultForm = false;
    }

    #region Player Animation Methods
    /// <summary> Sets trigger parameter in Animator </summary>
    public void SetTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || _animationController == null) return;
        _animationController.SetTrigger(triggerName);
    }
    #endregion

    /// <summary>
    /// Updates movement parameters (sets bool IsMoving and float SpeedX)
    /// Takes the X-axis direction input (-1..1)
    /// </summary>
    public void SetMoveAnimation(float dirX)
    {
        if (_animationController == null) return;

        _animationController.SetBool(HASH_IS_MOVING, Mathf.Abs(dirX) > 0.01f);
        _animationController.SetFloat(HASH_SPEED_X, dirX);
    }

    /// <summary> reset trigger Animator </summary>
    public void ResetAllTriggers()
    {
        if (_animationController == null) return;

        // Iterate all parameters and reset if type is Trigger
        var count = _animationController.parameterCount;
        for (int i = 0; i < count; i++)
        {
            var p = _animationController.GetParameter(i);
            if (p.type == AnimatorControllerParameterType.Trigger)
                _animationController.ResetTrigger(p.name);
        }
    }
}
