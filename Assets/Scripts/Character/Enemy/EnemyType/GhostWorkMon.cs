using UnityEngine;

public class GhostWorkMon : Enemy
{
    [SerializeField] private float _fadeDuration = 1.5f;
    [SerializeField] private float _hauntRange = 6f;
    [SerializeField] private float _teleportCooldown = 5f;

    public override void Move()
    {
        Debug.Log($"{name} floats silently toward the player...");
    }

    public override void Attack()
    {
        Debug.Log($"{name} haunts the player!");
    }

    public void Teleport(Vector3 target)
    {
        if (Vector3.Distance(transform.position, target) < _hauntRange)
        {
            transform.position = target;
            Debug.Log($"{name} teleported behind you!");
        }
    }
}
