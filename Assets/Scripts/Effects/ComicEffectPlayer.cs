using UnityEngine;

public class ComicEffectPlayer : MonoBehaviour
{
    [SerializeField] private CareerEffectProfile _profile;
    public CareerEffectProfile Profile => _profile;

    private SpriteRenderer _sr;
    private float _timer;
    private ComicEffectData _data;

    private string _poolKey;
    private bool _isPlaying = false;


    // Called when loaded into pool
    public void Initialize()
    {
        if (_sr == null)
            _sr = GetComponent<SpriteRenderer>();
    }

    public void SetPoolKey(string key) => _poolKey = key;
    public string GetPoolKey() => _poolKey;


    public void Play(ComicEffectData data, Vector3 pos)
    {
        _data = data;
        _timer = data.duration;
        _isPlaying = true;

        // Position + Offset + Random
        Vector3 finalPos = pos + (Vector3)data.offset;
        finalPos.x += Random.Range(-data.randomOffsetRange.x, data.randomOffsetRange.x);
        finalPos.y += Random.Range(-data.randomOffsetRange.y, data.randomOffsetRange.y);
        transform.position = finalPos;

        // Sprite
        _sr.sprite = data.sprite;
        _sr.color = data.color;

        // Scale random
        float scale = data.baseScale + Random.Range(-data.randomScaleRange, data.randomScaleRange);
        transform.localScale = Vector3.one * scale;

        // Random rotation
        if (data.randomRotation)
        {
            float rot = Random.Range(data.rotationMin, data.rotationMax);
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
        else
            transform.rotation = Quaternion.identity;

        // (Optional future) SFX
        // if (data.sfx) AudioSource.PlayClipAtPoint(data.sfx, transform.position, data.sfxVolume);

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_isPlaying) return;
        if (!this || gameObject == null) return; // ป้องกัน error เมื่อ Scene เปลี่ยน

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        _isPlaying = false;
        ComicEffectManager.Instance.Release(this);
    }

    private void OnDisable()
    {
        // เคลียร์สถานะก่อนกลับเข้าคิว Pool
        _timer = 0;
        _isPlaying = false;
    }

    public void SetFXProfile(CareerEffectProfile profile)
    {
        _profile = profile;
    }
}
