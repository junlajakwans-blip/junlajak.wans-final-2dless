using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableItem", menuName = "DUFFDUCK/Throwable Item")]
public class ThrowableItemSO : ScriptableObject
{
    [Header("Sprite ของไอเทมตอนอยู่บนพื้น / ตอนถูกถือบนหัว / ตอนถูกปา")]
    public Sprite itemSprite; 
    
    [Header("Drop Chance")]
    public float weight = 1f;     // โอกาสดรอป

    [Header("Gameplay")]
    public string poolTag;       // จะ Auto-generate ให้
    public int damage = 20;

    [Header("Map Binding")]
    public MapType mapType;

    [Header("FX When Hit")]
    public ComicEffectData hitEffect;

    [Header("Scaling")]
    public Vector3 scaleDefault = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 scaleOnHold = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 scaleOnThrow = new Vector3(0.2f, 0.2f, 0.2f);

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-generate poolTag ถ้าว่าง
        if (string.IsNullOrWhiteSpace(poolTag))
            poolTag = $"throw_{mapType}_{name}".ToLower();

        // ป้องกัน scale เป็นศูนย์ (มีคนเผลอเคลียร์)
        if (scaleOnHold == Vector3.zero)
            scaleOnHold = scaleDefault * 0.21f;

        if (scaleOnThrow == Vector3.zero)
            scaleOnThrow = scaleDefault * 0.21f;
    }
#endif
}
