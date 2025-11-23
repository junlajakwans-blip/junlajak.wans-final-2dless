using UnityEngine;
using System.Collections;

/// <summary>
/// Centralized Buff Controller — All timed buffs, debuffs, and permanent effects.
/// </summary>
public class BuffManager : MonoBehaviour
{
    private GameManager _gameManagerRef;
    public static BuffManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            Debug.Log("[BuffManager] Singleton/Persistence handled in Awake.");
        }
        else
        {
            // ทำลายตัวเองทันทีหากมีการสร้างซ้ำซ้อน
            Destroy(gameObject);
        }
    }   

    public void Initialize(GameManager gm)
    {
        _gameManagerRef = gm;
    }

    public void ApplyCollectibleBuff(CollectibleType type, Player player, int value, float duration)
    {
        switch (type)
        {
            case CollectibleType.Coffee:
                StartCoroutine(CoffeeBuffRoutine(player, value, duration));
                break;

            case CollectibleType.MooKrata:
                StartCoroutine(MooKrataBuffRoutine(duration));
                break;

            case CollectibleType.Takoyaki:
                StartCoroutine(TakoyakiRoutine(player, value, duration));
                break;

            case CollectibleType.GreenTea:
                ApplyGreenTeaPermanent(player, value);
                break;
        }
    }

    // ───────────────────────────────────────────────
    // COFFEE — Heal first then revert after duration
    // ───────────────────────────────────────────────
    private IEnumerator CoffeeBuffRoutine(Player player, int healAmount, float duration)
    {
        if (player == null) yield break;

        int oldHP = player.CurrentHealth;
        player.Heal(healAmount);
        Debug.Log($"[BuffManager] Coffee heal +{healAmount}");

        yield return new WaitForSeconds(duration);

        if (player != null && player.gameObject.activeInHierarchy && !player.IsDead)
            {
                // คำนวณความแตกต่างเพื่อคืน HP ที่เกินมา
                int hpToRevert = player.CurrentHealth - oldHP;
                
                // ถ้า HP ปัจจุบันมากกว่า HP ก่อน Buff (เช่น ไม่ได้รับ Damage มากจน HP ลดลง)
                if (hpToRevert > 0)
                {
                    player.TakeDamage(hpToRevert);
                    Debug.Log($"[BuffManager] Coffee expired — HP reverted (-{hpToRevert} HP).");
                }
            }

    }

    // ───────────────────────────────────────────────
    // MOO KRATA — Disable enemy behaviors
    // ───────────────────────────────────────────────
    private IEnumerator MooKrataBuffRoutine(float duration)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            e.DisableBehavior(duration);

        Debug.Log($"[BuffManager] MooKrata — disabling {enemies.Length} enemies for {duration}s");
        yield return new WaitForSeconds(duration);
    }

    // ───────────────────────────────────────────────
    // TAKOYAKI — HOT then COOL
    // HOT = damage, COOL = heal
    // ───────────────────────────────────────────────
    private IEnumerator TakoyakiRoutine(Player player, int amount, float duration)
    {
        if (player == null) yield break;

        // HOT state
        player.TakeDamage(amount);
        Debug.Log($"[BuffManager] Takoyaki HOT — -{amount} HP!");

        yield return new WaitForSeconds(duration);

        // COOL state
        player.Heal(amount);
        Debug.Log($"[BuffManager] Takoyaki COOL — +{amount} HP!");
    }

    // ───────────────────────────────────────────────
    // GREEN TEA — Permanent +10 Max HP
    // ───────────────────────────────────────────────
    private void ApplyGreenTeaPermanent(Player player, int value)
    {
        if (player == null) return;

        // Heal ชั่วคราวในเกมตอนนี้
        player.Heal(value);

        Debug.Log($"[BuffManager] Green Tea : Heal +{value}");
    }
}