using UnityEngine;

[CreateAssetMenu(fileName = "CardUIPool", menuName = "DUFFDUCK/CardUIPool")]
public class CardUIPool : ScriptableObject
{
    [Header("Resources Folder (auto-load SO)")]
    public string resourcesFolder = "Careers";

    [Header("Card UI Prefabs")]
    public GameObject careerCardPrefab;   // สำหรับ 8 อาชีพ
    public GameObject muscleCardPrefab;   // สำหรับ Berserk

    private DuckCareerData[] allCareers;

    private void OnEnable()
    {
        allCareers = Resources.LoadAll<DuckCareerData>(resourcesFolder);
        Debug.Log($"[CardUIPool] Loaded {allCareers.Length} SO Careers (Career + Berserk)");

        // ───────────────────────────────────────────
        // Debug Count ─ แยกตาม CardType
        // ───────────────────────────────────────────
        int none = 0, career = 0, berserk = 0;

        for (int i = 0; i < allCareers.Length; i++)
        {
            switch (allCareers[i].CardType)
            {
                case CardType.None:   none++; break;
                case CardType.Career: career++; break;
                case CardType.Berserk: berserk++; break;
            }
        }

        Debug.Log(
            $"[CardUIPool] Loaded {allCareers.Length} DuckCareerData | None: {none} | Career: {career}  (normal gameplay drop) | Berserk: {berserk} (MuscleDuck)"
        );        
    }

    public DuckCareerData GetData(string id)
    {
        for (int i = 0; i < allCareers.Length; i++)
            if (allCareers[i].CareerID.ToString() == id)
                return allCareers[i];

        Debug.LogWarning($"[CardUIPool] CareerID not found: {id}");
        return null;
    }

    public DuckCareerData GetRandomCareerForDrop()
    {
        while (true)
        {
            int index = Random.Range(0, allCareers.Length);
            if (allCareers[index].CardType == CardType.Career)  // หลีกเลี่ยง Muscle
                return allCareers[index];
        }
    }

    private GameObject GetPrefab(CardType type)
    {
        return type == CardType.Berserk ? muscleCardPrefab : careerCardPrefab;
    }

    // จุดเดียวที่สร้าง UI Prefab สำหรับการ์ด
    public GameObject CreateCardUI(DuckCareerData data, Transform parent)
    {
        GameObject prefab = GetPrefab(data.CardType);
        GameObject obj = Instantiate(prefab, parent);

        if (obj.TryGetComponent<CardUI>(out var ui))
            ui.Set(data);
        else
            Debug.LogError("[CardUIPool] Prefab missing CardUI component!");

        obj.name = $"CardUI_{data.CareerID}";

        return obj;
    }
}
