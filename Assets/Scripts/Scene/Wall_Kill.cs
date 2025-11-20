using UnityEngine;

/// <summary>
/// Endless Wall — pushes player forward and kills on contact.
/// Works with Object Pool & MapGeneratorBase.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WallPushController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _pushSpeed = 1.0f; // overwritten by MapGenerator
    [SerializeField] private bool _isPushing = true;

    [Header("Runtime")]
    private Transform _player;

    private void Start()
    {
        _player = FindFirstObjectByType<Player>()?.transform;
    }

    private void Update()
    {
        if (!_isPushing) return;

        // Move horizontally only
        transform.Translate(Vector3.right * _pushSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Called by MapGeneratorBase to sync speed and toggle state
    /// </summary>
    public void SetPushState(float speed, bool enabled)
    {
        _pushSpeed = speed;
        _isPushing = enabled;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            player.TakeDamage(9999); // instant kill
            Debug.Log("[Wall] Player touched the wall → instant death.");
        }
    }
}
