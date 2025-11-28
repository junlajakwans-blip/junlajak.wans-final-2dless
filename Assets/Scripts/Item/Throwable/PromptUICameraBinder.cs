using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Canvas))]
public class PromptUICameraBinder : MonoBehaviour
{
    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        StartCoroutine(BindCameraRoutine());
    }

    private IEnumerator BindCameraRoutine()
    {
        // รอจนกว่า Scene โหลดและ Camera.main ถูกสร้าง
        while (_canvas.worldCamera == null)
        {
            if (Camera.main != null)
                _canvas.worldCamera = Camera.main;

            yield return null;
        }
    }
}
