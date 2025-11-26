using UnityEngine;
using System.Collections;

public class ComicEffectAutoRecycle : MonoBehaviour
{
    private string _tag;

    private void Awake()
    {
        // รับ tag จากชื่อ prefab เพื่อ ReturnToPool ถูกตัว
        _tag = gameObject.name.Replace("(Clone)", "").Trim();
    }

    public void BeginCountdown(float delay)
    {
        StopAllCoroutines();
        StartCoroutine(DelayRecycle(delay));
    }

    private IEnumerator DelayRecycle(float delay)
    {
        yield return new WaitForSeconds(delay);

        ObjectPoolManager.Instance.ReturnToPool(_tag, gameObject);
    }
}
