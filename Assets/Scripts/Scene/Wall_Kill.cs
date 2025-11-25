using UnityEngine;
using System;

/// <summary>
/// Endless Wall — pushes player forward and kills on contact.
/// Works with Object Pool & MapGeneratorBase.
/// </summary>

namespace DuffDuck.Stage
{
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
        // ใช้ FindFirstObjectByType<Player>()? เพื่อหา Player
        _player = FindFirstObjectByType<Player>()?.transform; 
    }
    
    // **NEW**: ย้ายการเคลื่อนที่มาที่ Update() เพื่อให้เคลื่อนที่ราบรื่น (ใช้ Time.deltaTime)
    private void Update()
    {
        // เช็คสถานะก่อนเคลื่อนที่
        if (!_isPushing) return; 

        // Move horizontally only (ใช้ Time.deltaTime ใน Update() เพื่อให้การเคลื่อนที่ราบรื่น)
        transform.Translate(Vector3.right * _pushSpeed * Time.deltaTime);
    }
    

    /// <summary>
    /// Called by MapGeneratorBase to sync speed and toggle state
    /// </summary>
    public void SetPushState(float speed, bool enabled)
    {
        _pushSpeed = speed;
        _isPushing = enabled;
        
        // Invoke event ทันทีที่ความเร็วเปลี่ยน
        OnWallSpeedChanged?.Invoke(_pushSpeed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            player.TakeDamage(9999); // instant kill
            Debug.Log("[Wall] Player touched the wall → instant death.");
        }
    }

    public static event Action<float> OnWallSpeedChanged;
}
}