using UnityEngine;
using System.Collections.Generic;

public class ComicEffectManager : MonoBehaviour
{
    public static ComicEffectManager Instance { get; private set; }

    private Dictionary<string, Queue<ComicEffectPlayer>> _pool
        = new Dictionary<string, Queue<ComicEffectPlayer>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(string key, ComicEffectPlayer prefab, int preloadCount = 3)
    {
        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<ComicEffectPlayer>();

        for (int i = 0; i < preloadCount; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.Initialize();
            obj.SetPoolKey(key);
            obj.gameObject.SetActive(false);
            _pool[key].Enqueue(obj);
        }
    }

    public void Play(ComicEffectData data, Vector3 pos)
    {
        if (data == null) return;

        string key = data.name;

        // 🔥 Auto-register ถ้า pool ยังไม่มี
        if (!_pool.ContainsKey(key))
        {
            Debug.LogWarning($"[ComicFX] Auto-register missing FX pool: {key}");
            var prefab = GetComponentInChildren<ComicEffectPlayer>();
            if (prefab == null) return;

            Register(key, prefab, 3);
        }

        if (_pool[key].Count == 0)
        {
            Debug.LogWarning($"[ComicFX] Pool empty, creating more: {key}");
            var prefab = GetComponentInChildren<ComicEffectPlayer>();
            var obj = Instantiate(prefab, transform);
            obj.Initialize();
            obj.SetPoolKey(key);
            obj.gameObject.SetActive(false);
            _pool[key].Enqueue(obj);
        }

        var fx = _pool[key].Dequeue();
        fx.SetPoolKey(key);
        fx.Play(data, pos);
    }


    public void Release(ComicEffectPlayer fx)
    {
        if (fx == null) return;
        string key = fx.GetPoolKey();
        if (string.IsNullOrEmpty(key)) return;

        fx.gameObject.SetActive(false);

        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<ComicEffectPlayer>();

        _pool[key].Enqueue(fx);
    }

    /// <summary>
    /// Initialize FX pools from player career profiles (called by GameManager)
    /// </summary>
    public void Initialize(Player player)
    {
        if (player == null) return;

        var switcher = player.GetComponent<CareerSwitcher>();
        if (switcher == null)
        {
            Debug.LogWarning("[ComicFX] CareerSwitcher missing — cannot init FX pools");
            return;
        }

        // ใช้ ComicEffectPlayer ตัวที่อยู่ใต้ Player เป็น prefab กลาง
        var fxPrefab = player.GetComponentInChildren<ComicEffectPlayer>();
        if (fxPrefab == null)
        {
            Debug.LogWarning("[ComicFX] ComicEffectPlayer prefab missing on Player — cannot init FX pools");
            return;
        }

        foreach (var entry in switcher.CareerBodyMaps)
        {
            if (entry == null || entry.fxProfile == null)
                continue;

            CareerEffectProfile p = entry.fxProfile;

            AddFXToPool(p.switchFX, fxPrefab);
            AddFXToPool(p.basicAttackFX, fxPrefab);
            AddFXToPool(p.skillFX, fxPrefab);
            AddFXToPool(p.jumpAttackFX, fxPrefab);
            AddFXToPool(p.hurtFX, fxPrefab);
            AddFXToPool(p.deathFX, fxPrefab);
            AddFXToPool(p.extraFX, fxPrefab);
        }

    }

    private void AddFXToPool(ComicEffectData data, ComicEffectPlayer prefab)
    {
        if (data == null) return;

        string key = data.name;
        if (_pool.ContainsKey(key)) return;

        Register(key, prefab, 3);
        Debug.Log($"[ComicFX] Registered FX pool → {key}");
    }



}
