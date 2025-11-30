using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(100)] // รันหลังกล้องถูกสร้าง
public class PromptUICameraBinder : MonoBehaviour
{
    private Canvas _canvas;

    private void OnEnable()
    {
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();

        StartCoroutine(BindCameraRoutine());
    }

    private IEnumerator BindCameraRoutine()
    {
        // รอจนกว่า Camera.main พร้อม
        while (Camera.main == null)
            yield return null;

        _canvas.worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // ให้ป้ายหันเข้าหาผู้เล่นตลอด (หมุนตามมุมกล้อง)
        if (_canvas.worldCamera != null)
            transform.rotation = _canvas.worldCamera.transform.rotation;
    }
}
