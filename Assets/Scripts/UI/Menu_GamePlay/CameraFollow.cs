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

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;

    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

    private void LateUpdate()
    {
        if (target1 == null && target2 == null) return;

        Vector3 center;

        if (target1 != null && target2 != null)
        {
            // หาตรงกลางระหว่าง 2 คน
            center = (target1.position + target2.position) / 2f;
        }
        else
        {
            // fallback (เหลือคนเดียว)
            center = (target1 != null) ? target1.position : target2.position;
        }

        Vector3 desiredPosition = center + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}