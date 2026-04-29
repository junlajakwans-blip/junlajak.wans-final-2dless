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
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    private Transform target1;
    private Transform target2;

    public void SetTargets(Transform p1, Transform p2 = null)
    {
        target1 = p1;
        target2 = p2;
    }

    private void LateUpdate()
    {
        if (target1 == null) return;

        Vector3 targetPosition;

        // 🟢 Solo
        if (target2 == null)
        {
            targetPosition = target1.position;
        }
        // 🔵 2 Player
        else
        {
            targetPosition = (target1.position + target2.position) / 2f;
        }

        Vector3 desired = targetPosition + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}