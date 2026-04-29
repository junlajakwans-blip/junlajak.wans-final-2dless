//1P camera follow script, to be attached to the main camera in the scene. It will follow the player with a smooth transition.
// using UnityEngine;

// public class CameraFollow : MonoBehaviour
// {
//     [SerializeField] private Transform target;      // Player
//     [SerializeField] private float smoothSpeed = 5f;
//     [SerializeField] private Vector3 offset = new Vector3(3f, 1.5f, -10f);

//     private void LateUpdate()
//     {
//         if (target == null) return;

//         Vector3 desiredPosition = target.position + offset;
//         Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
//         transform.position = smoothedPosition;
//     }
// }

using UnityEngine;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    private Vector3 velocity;

    private void LateUpdate()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        List<Player> alive = new List<Player>();

        foreach (var p in players)
        {
            if (p != null && !p.IsDead)
                alive.Add(p);
        }

        if (alive.Count == 0)
            return;

        Vector3 targetPosition;

        // 🟢 มีคนเดียว
        if (alive.Count == 1)
        {
            targetPosition = alive[0].transform.position;
        }
        // 🔵 มีหลายคน → เอาค่าเฉลี่ย
        else
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in alive)
                sum += p.transform.position;

            targetPosition = sum / alive.Count;
        }

        Vector3 desired = targetPosition + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            0.2f
        );
    }
}