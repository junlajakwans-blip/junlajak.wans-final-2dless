using UnityEngine;

public static class ComicEffectSpawner
{
    /// <summary>
    /// Spawn effect at character position (uses Transform)
    /// Auto applies Scale / Rotation / Offset / Color / Duration from ComicEffectData.
    /// </summary>
    public static void Spawn(ComicEffectData data, Transform target)
    {
        if (data == null || target == null)
            return;

        Spawn(data, target.position);
    }

    /// <summary>
    /// Spawn at world position (Vector3) — used for JumpAttack, HitEnemy, etc.
    /// </summary>
    public static void Spawn(ComicEffectData data, Vector3 worldPos)
    {
        if (data == null)
            return;

        // เรียก Prefab จาก Pool ตามชื่อที่จะกำหนดใน Inspector เช่น "ComicFX"
        GameObject obj = ObjectPoolManager.Instance.SpawnFromPool("ComicFX", worldPos, Quaternion.identity);
        if (obj == null)
            return;

        // อ่าน component ตัวเรนเดอร์
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = data.sprite;
            sr.color = data.color;
        }

        // Random Scale
        float scale = data.baseScale + Random.Range(-data.randomScaleRange, data.randomScaleRange);
        obj.transform.localScale = Vector3.one * scale;

        // Random rotation
        float rot = data.randomRotation ? Random.Range(data.rotationMin, data.rotationMax) : 0f;
        obj.transform.rotation = Quaternion.Euler(0, 0, rot);

        // Random offset
        Vector2 randomOffset = new Vector2(
            Random.Range(-data.randomOffsetRange.x, data.randomOffsetRange.x),
            Random.Range(-data.randomOffsetRange.y, data.randomOffsetRange.y)
        );
        obj.transform.position = worldPos + (Vector3)(data.offset + randomOffset);

        // ตั้งตัวทำลายอัตโนมัติ → แต่เนื่องจากมี ObjectPool ควรคืนเข้า Pool แทน destroy
        obj.GetComponent<ComicEffectAutoRecycle>()?.BeginCountdown(data.duration);
    
        // --------------------------------------------------------
        // 🟦 FUTURE UPGRADE — COMMENTED BUT STRUCTURE READY
        // --------------------------------------------------------

        // เล่นเสียง (ถ้ามี)
        //if (data.sfx != null)
        //   AudioSource.PlayClipAtPoint(data.sfx, obj.transform.position, data.sfxVolume);

        // 1) รองรับ Random เอฟเฟกต์หลายแบบในอาชีพเดียว
        //    → ComicEffectData สามารถเปลี่ยนเป็น list แล้ว random เลือกตัวหนึ่ง
        // if (data.multipleSprites != null && data.multipleSprites.Count > 0)
        //    sr.sprite = data.multipleSprites[Random.Range(0, data.multipleSprites.Count)];

        // 2) รองรับ Animation Sheet (หลายเฟรม)
        //    → เพิ่มคอมโพเนนต์ ComicEffectAnimator แล้วส่ง Sprite[] ให้มันเล่น Loop หรือ Frame-by-frame
        // obj.GetComponent<ComicEffectAnimator>()?.Play(data.spriteSequence);

        // 3) รองรับ เอฟเฟกต์บนศัตรู (Hit Enemy)
        //    → เพิ่มพารามิเตอร์ Transform enemy แล้ว Spawn บน enemy.transform.position แทน player
        // public static void SpawnHitFX(ComicEffectData data, Enemy enemy) { }

        // 4) รองรับ Particle + Sprite + Sound + Camera Shake
        // if (data.vfxPrefab != null)
        //    Instantiate(data.vfxPrefab, obj.transform.position, Quaternion.identity);
        // CameraShake.Instance?.Shake(data.shakeIntensity, data.shakeDuration);
    }
}
