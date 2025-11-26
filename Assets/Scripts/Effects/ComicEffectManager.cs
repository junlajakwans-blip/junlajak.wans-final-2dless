using UnityEngine;
using System.Collections.Generic;

public static class ComicEffectManager
{
    private static Dictionary<string, Queue<ComicEffectPlayer>> _pool 
        = new Dictionary<string, Queue<ComicEffectPlayer>>();

    public static void Register(string key, ComicEffectPlayer prefab, int preloadCount = 3)
    {
        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<ComicEffectPlayer>();

        for (int i = 0; i < preloadCount; i++)
        {
            ComicEffectPlayer obj = Object.Instantiate(prefab);
            obj.Initialize();
            obj.gameObject.SetActive(false);
            _pool[key].Enqueue(obj);
        }
    }

    public static void Play(ComicEffectData data, Vector3 pos)
    {
        if (!_pool.ContainsKey(data.name) || _pool[data.name].Count == 0)
        {
            Debug.LogWarning($"[ComicFX] Pool missing: {data.name}");
            return;
        }

        ComicEffectPlayer fx = _pool[data.name].Dequeue();
        fx.Play(data, pos);
    }

    public static void Release(ComicEffectPlayer fx)
    {
        fx.gameObject.SetActive(false);
        _pool[fx.name].Enqueue(fx);
    }
}
