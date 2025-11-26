using UnityEngine;

public static class CameraShaker
{
    private static Transform cam;
    private static bool shaking;
    private static float shakeStrength;
    private static float shakeTime;
    private static float shakeTimer;

    public static void ShakeOnce(float duration, float strength)
    {
        if (cam == null)
            cam = Camera.main.transform;

        shakeTime = duration;
        shakeStrength = strength;
        shakeTimer = duration;

        if (!shaking)
            cam.gameObject.AddComponent<ShakeRunner>();
        shaking = true;
    }

    private class ShakeRunner : MonoBehaviour
    {
        private Vector3 originalPos;

        private void OnEnable()
        {
            originalPos = cam.position;
        }

        private void LateUpdate()
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                cam.position = originalPos + Random.insideUnitSphere * shakeStrength;
            }
            else
            {
                cam.position = originalPos;
                shaking = false;
                Destroy(this);
            }
        }
    }
}
