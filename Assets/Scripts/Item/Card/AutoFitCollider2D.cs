using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AutoFitCollider2D : MonoBehaviour
{
    private void Reset()
    {
        Fit();
    }

    private void OnValidate()
    {
        Fit();
    }

    private void Fit()
    {
        var collider = GetComponent<BoxCollider2D>();
        var rect = GetComponent<RectTransform>();

        // แปลงขนาด RectTransform → world size
        Vector2 size = rect.rect.size;
        size /= 100f; // UI 100 unit → 1 unity world (เหมาะกับ พื้นที่ 2D)

        collider.size = size;
        collider.offset = Vector2.zero;
    }
}
