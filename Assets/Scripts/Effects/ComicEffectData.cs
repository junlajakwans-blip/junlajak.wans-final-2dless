using UnityEngine;

/// <summary>
/// เก็บข้อมูลที่จำเป็นสำหรับการเล่นเอฟเฟกต์ เช่น
/// - ชื่อเอฟเฟกต์
/// - Sprite หรือ Animation ที่ใช้
/// - Duration (เวลาที่ effect แสดงก่อนปิด)
/// - Scale
/// - Random rotation, random position offset
/// - Sound (หากต้องการในอนาคต)
/// - Particle หลายเฟรม (option ถ้าจะทำ animation แบบ sprite sheet)
/// - แสดงผลใน ComicEffectPrefab
/// </summary>
[CreateAssetMenu(menuName = "DUFFDUCK/Comic Effect Data", fileName = "ComicEffect_")]
public class ComicEffectData : ScriptableObject
{
    [Header("Visual")]
    public Sprite sprite;
    public Color color = Color.white;

    [Header("Timing")]
    public float duration = 0.6f;

    [Header("Scale")]
    public float baseScale = 1f;
    public float randomScaleRange = 0.2f; // ± random

    [Header("Rotation")]
    public bool randomRotation = true;
    public float rotationMin = -20f;
    public float rotationMax = 20f;

    [Header("Offset")]
    public Vector2 offset = new Vector2(0, 1.2f);
    public Vector2 randomOffsetRange = new Vector2(0.3f, 0.4f);

    //[Header("Future Ready: Sound FX")]
    //public AudioClip sfx;
    //public float sfxVolume = 1f;

    // 🔵 FUTURE (commented) — animation sheet
    // public Sprite[] spriteSequence;

    // 🔵 FUTURE (commented) — particle prefab or camera shake
    // public GameObject vfxPrefab;
    // public float shakeIntensity = 0.2f;
    // public float shakeDuration = 0.1f;
}