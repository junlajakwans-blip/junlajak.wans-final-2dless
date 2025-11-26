using UnityEngine;

public class ComicEffectPlayer : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float _timer;
    private ComicEffectData _data;

    public void Initialize()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
    }

    public void Play(ComicEffectData data, Vector3 pos)
    {
        _data = data;
        _timer = data.duration;

        // Position + Offset + Random
        Vector3 finalPos = pos + (Vector3)data.offset;
        finalPos.x += Random.Range(-data.randomOffsetRange.x, data.randomOffsetRange.x);
        finalPos.y += Random.Range(-data.randomOffsetRange.y, data.randomOffsetRange.y);
        transform.position = finalPos;

        // Sprite
        _sr.sprite = data.sprite;
        _sr.color = data.color;

        // Scale (random)
        float scale = data.baseScale + Random.Range(-data.randomScaleRange, data.randomScaleRange);
        transform.localScale = Vector3.one * scale;

        // Rotation (optional)
        if (data.randomRotation)
        {
            float rot = Random.Range(data.rotationMin, data.rotationMax);
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
        else transform.rotation = Quaternion.identity;

        // Future Sound
        //if (data.sfx != null)
        //    AudioSource.PlayClipAtPoint(data.sfx, transform.position, data.sfxVolume);

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_timer <= 0)
        {
            ComicEffectManager.Release(this);
            return;
        }
        _timer -= Time.deltaTime;
    }
}
