using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ใช้ควบคุมหน้า Select Map ใน Main Menu
/// - แสดงรูปแมพ / ชื่อ / คำอธิบาย / ระดับความยาก
/// - แสดงว่าปลดล็อกหรือยัง (ไอคอนกุญแจ + ปุ่มเล่นได้/ไม่ได้)
/// - แสดงจำนวน Key ปัจจุบัน
/// - กดลูกศรซ้ายขวาเลื่อนแมพ และกดรูปภาพหรือ Spacebar เพื่อเข้าแมพ
/// </summary>
public class MapSelectController : MonoBehaviour
{
    #region Data Model
    [System.Serializable]
    public class MapInfo
    {
        public MapType mapType;      // Enum ของแมพ (School / RoadTraffic / Kitchen)
        public string sceneName;     // ชื่อ Scene ที่จะโหลด
        public Sprite previewImage;  // รูปพรีวิวแมพ
        public string mapName;       // ชื่อแมพที่โชว์บนหัว
        public int difficultyLevel;  // ระดับความยาก 1–3 (ใช้เปิด icon)
        public string description;   // คำอธิบายแมพ
        public bool unlocked;        // ปลดล็อกแล้วหรือยัง
    }

    private MapInfo[] maps;
    private int index = 0;
    #endregion


    #region UI References
    [Header("UI References")]
    public Image previewImage;
    public TMP_Text mapNameText;
    public TMP_Text descriptionText;
    public GameObject lockedIcon;
    public GameObject mainMenuPanel;
    public GameObject mapPanel;
    public Image[] difficultyIcons;
    public Button previewImageButton;

    [Header("Map Sprites")]
    public Sprite schoolSprite;
    public Sprite roadSprite;
    public Sprite kitchenSprite;

    [Header("Map Store")]
    [SerializeField] private StoreUI storeUI;
    [SerializeField] private Image glowEffect;
    [SerializeField] private float glowDuration = 0.35f;
    [SerializeField] private Image greyOverlay;

    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private GameObject keyIcon;

    [Header("Map Store : UnlockMap")]
    [SerializeField] private GameObject unlockChoicePanel;
    [SerializeField] private TMP_Text unlockChoiceText;
    [SerializeField] private Button unlockYesButton;
    [SerializeField] private Button unlockNoButton;




    private StoreItem currentItem;

    //For Dependency Injection
    private GameManager _gameManagerRef;
    private Currency _currencyRef;
    
    #endregion


    #region Dependencies
    /// <summary>
    /// Injects required runtime dependencies.
    /// </summary>
    public void SetDependencies(GameManager gm, Currency currency)
    {
        _gameManagerRef = gm;
        _currencyRef = currency;

        if (maps != null)  
        RefreshUI();
    }
    #endregion




    #region Unity Lifecycle

    private void OnEnable()
    {
        StartCoroutine(InitAfterStoreReady());
    }


    private void Update()
    {
        // กด Spacebar เพื่อเล่นแมพที่เลือกอยู่ (เฉพาะตอน Panel นี้ active)
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            TryPlaySelectedMap();
        }
    }
    #endregion

    private IEnumerator InitAfterStoreReady()
    {
        yield return null; // รอ 1 เฟรม เพื่อให้ StoreUI และ StoreManager Initialize ให้เสร็จ

        InitializeMapSelect();
    }

    private void InitializeMapSelect()
    {
        // เตรียมข้อมูลแมพ
        maps = new MapInfo[]
        {
            CreateMapInfo(MapType.School),
            CreateMapInfo(MapType.RoadTraffic),
            CreateMapInfo(MapType.Kitchen)
        };

        storeUI.OpenMap(); // เปิดร้านแมพ

        // สมัคร event แบบปลอดภัย ไม่ซ้อน
        if (storeUI != null && storeUI.StoreMapRef != null)
        {
            storeUI.StoreMapRef.OnMapUnlockedEvent -= OnMapUnlocked;
            storeUI.StoreMapRef.OnMapUnlockedEvent += OnMapUnlocked;
        }
        else
        {
            Debug.LogWarning("[MapSelectController] ⚠ StoreMapRef ยังไม่พร้อม – Event ยังไม่เชื่อม");
        }

        previewImageButton.onClick.RemoveAllListeners();
        previewImageButton.onClick.AddListener(TryPlaySelectedMap);

        RefreshUI();
    }


    #region MapInfo Builder
    private MapInfo CreateMapInfo(MapType type)
    {
        var info = new MapInfo();
        info.mapType = type;

        switch (type)
        {
            case MapType.School:
                info.unlocked = true;
                info.sceneName = "MapSchool";
                info.previewImage = schoolSprite;
                info.mapName = "School Zone";
                info.description = "NO DUFF NO DUCK";
                info.difficultyLevel = 1;
                break;

            case MapType.RoadTraffic:
                info.sceneName = "MapRoadTraffic";
                info.previewImage = roadSprite;
                info.mapName = "City Road";
                info.description = "DUCK DUCK — HONK!!";
                info.difficultyLevel = 2;
                break;

            case MapType.Kitchen:
                info.sceneName = "MapKitchen";
                info.previewImage = kitchenSprite;
                info.mapName = "Kitchen Mayhem";
                info.description = "TODAY MENU IS ROAST DUCK, Yummy!";
                info.difficultyLevel = 3;
                break;
        }

        // ช็คสถานะปลดล็อกจาก StoreMap จริง
        if (storeUI != null && storeUI.StoreMapRef != null)
        {
            var storeItem = GetStoreItemForMap(type);
            info.unlocked = storeItem == null || storeUI.StoreMapRef.IsUnlocked(storeItem);
        }
        else
        {
            //  School เริ่มปลดล็อกเสมอ
            info.unlocked = (type == MapType.School);
        }

        return info;
        }
    #endregion


    #region Display / Refresh

    /// <summary>
    /// อัปเดตหน้าจอให้ตรงกับแมพ index ปัจจุบัน
    /// </summary>
    public void RefreshUI()
    {
        if (maps == null || maps.Length == 0)
            return;

        if (index < 0 || index >= maps.Length)
            index = 0;

        var map = maps[index];

        var storeItem = GetStoreItemForMap(map.mapType);
        if (storeItem != null && storeUI != null && storeUI.StoreMapRef != null)
            map.unlocked = storeUI.StoreMapRef.IsUnlocked(storeItem);

        // รูปและข้อความ
        previewImage.sprite = map.previewImage;
        mapNameText.text = map.mapName;
        descriptionText.text = map.description;

        if (map.mapType == MapType.School)
        {
            map.unlocked = true;
        }
        else if (storeItem != null && storeUI.StoreMapRef != null)
        {
            map.unlocked = storeUI.StoreMapRef.IsUnlocked(storeItem);
        }


        // ไอคอนความยาก
        for (int i = 0; i < difficultyIcons.Length; i++)
        {
            if (difficultyIcons[i] != null) 
            difficultyIcons[i].gameObject.SetActive(i < map.difficultyLevel);
        }


        // กรณีปลดล็อคแล้ว หรือราคา = 0 → ซ่อน UI ซื้อทั้งหมด
        if (storeItem != null && (map.unlocked || storeItem.Price == 0))
        {

            if (lockedIcon != null) lockedIcon.SetActive(false);
            if (greyOverlay != null) greyOverlay.gameObject.SetActive(false);
            if (keyText != null) keyText.gameObject.SetActive(false);
            if (keyIcon != null) keyIcon.SetActive(false);
            if (previewImageButton != null) previewImageButton.interactable = true;
            return;
        }

        // กรณีแมพยังล็อค → แสดงราคา Key
            if (lockedIcon != null) lockedIcon.SetActive(true);
            if (greyOverlay != null) greyOverlay.gameObject.SetActive(true);
            if (previewImageButton != null) previewImageButton.interactable = false;

        if (storeItem != null)
        {
            int have = _currencyRef.KeyMap;
            int need = storeItem.Price;

        if (keyText != null)
        {
            keyText.gameObject.SetActive(true);
            if (keyIcon != null) keyIcon.SetActive(true);

            keyText.text = $"{have}/{need}"; 
            keyText.color = (have >= need) ? Color.green : Color.red;
        }
        }
        else
        {
            if (keyText != null) keyText.gameObject.SetActive(false); 
            if (keyIcon != null) keyIcon.SetActive(false);
        }
    }


    #endregion

    #region Navigation
    public void NextMap()
    {
        Debug.Log("[MapSelectController] ➡ NEXT clicked");

        if (maps == null || maps.Length == 0) return;

        index++;
        if (index >= maps.Length) index = 0;

        RefreshUI();
    }

    public void PrevMap()
    {
        Debug.Log("[MapSelectController] ⬅ PREVIOUS clicked");

        if (maps == null || maps.Length == 0) return;

        index--;
        if (index < 0) index = maps.Length - 1;

        RefreshUI();
    }

    public void BackToMainMenu()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
    #endregion

    #region Play Map
    /// <summary>
    /// กดเล่นแมพปัจจุบัน ถ้ายังล็อกอยู่จะไม่ทำอะไร
    /// </summary>
    public void TryPlaySelectedMap()
    {
        if (maps == null || maps.Length == 0) return;

        var map = maps[index];
        var item = GetStoreItemForMap(maps[index].mapType);
        if (item != null)
            maps[index].unlocked = storeUI.StoreMapRef.IsUnlocked(item);

        if (map.mapType == MapType.School)
        {
            map.unlocked = true;
        }

        //  ถ้าแมพยังล็อค ให้เช็คกุญแจแทนการ return เฉยๆ
        if (!map.unlocked)
        {
            StoreItem storeItem = GetStoreItemForMap(map.mapType);
            if (storeItem == null)
            {
                Debug.LogError("[MapSelect] ❌ Cannot find StoreItem of this map.");
                return;
            }

            int have = _currencyRef.KeyMap;
            int need = storeItem.Price;

            // ยังล็อกและกุญแจไม่พอ -> ไม่ต้องเปิด Popup
            if (have < need)
            {
                Debug.Log("[MapSelect] ❌ Not enough keys to unlock.");
                return;
            }

            // ยังล็อกแต่กุญแจพอ → เปิดหน้าต่างยืนยัน
            unlockChoicePanel.SetActive(true);
            unlockChoiceText.text = $"Unlock {map.mapName}?";

            unlockYesButton.onClick.RemoveAllListeners();
            unlockYesButton.onClick.AddListener(() => ConfirmUnlock(map));

            unlockNoButton.onClick.RemoveAllListeners();
            unlockNoButton.onClick.AddListener(() => unlockChoicePanel.SetActive(false));

            return;
        }

        // ปลดล็อกแล้ว → เข้าเกมเลย
        if (_gameManagerRef == null)
        {
            Debug.LogError("[MapSelect] ❌ GameManager missing");
            return;
        }

        Debug.Log($"[MapSelect] ▶ Load scene: {map.sceneName}");
        _gameManagerRef.LoadScene(map.sceneName);
    }


    private void ConfirmUnlock(MapInfo map)
    {
        var storeItem = GetStoreItemForMap(map.mapType);
        if (storeItem == null) return;

        // ป้องกันการเรียกซื้อซ้ำ
        if (map.unlocked)
        {
            unlockChoicePanel.SetActive(false);
            TryPlaySelectedMap(); // เข้าเกมได้เลย
            return;
        }

        // ซื้อ (ปลดล็อก)
        bool ok = storeUI.StoreMapRef.Purchase(storeItem);
        if (!ok)
        {
            Debug.Log("❌ Unlock failed (not enough keys?)");
            unlockChoicePanel.SetActive(false);
            return;
        }

        // ปลดล็อกสำเร็จ
        map.unlocked = true;
        _gameManagerRef.SaveProgress();
        unlockChoicePanel.SetActive(false);

        // ⬇⬇ ปิดทุก UI ของ Locked ทันที (กัน RefreshUI ดึงค่าค้าง)
        lockedIcon?.SetActive(false);
        greyOverlay?.gameObject.SetActive(false);
        keyText?.gameObject.SetActive(false);
        keyIcon?.SetActive(false);

        // ⬇ เปิดคลิกเพื่อเล่นได้แล้ว
        previewImageButton.interactable = true;

        // ⬇ อัปเดต UI ให้ตรงสถานะล่าสุด
        RefreshUI();

        // ⬇ เอฟเฟกต์ Glow แจ้งว่าปลดล็อกแล้ว
        StartCoroutine(PlayUnlockGlow());
    }
    
    public void OnUnlockYes()
    {
        ConfirmUnlock(maps[index]);
    }

    public void OnUnlockNo()
    {
        unlockChoicePanel.SetActive(false);
        unlockChoiceText.text = "";
    }


#endregion

    #region  Buy Map on Lock

    public void BuyCurrentMap()
    {
        var map = maps[index];
        currentItem = GetStoreItemForMap(map.mapType);

        if (currentItem == null)
        {
            Debug.LogError("[MapSelectController] ❌ Cannot find StoreItem for map: " + map.mapType);
            return;
        }

        storeUI.OpenMap();                 
        storeUI.HighlightItem(currentItem.ID);  // ไฮไลต์ในร้าน
    }


    private StoreItem GetStoreItemForMap(MapType type)
    {
        foreach (var slot in storeUI.MapSlots)   // หรือ storeUI.GetMapSlots()
        {
            if (slot == null) continue;
            var item = slot.CurrentItem;
            if (item == null) continue;

            if (item.mapType == type)
                return item;
        }
        return null;
    }

    private void OnMapUnlocked(string payload)
    {
        // payload ตัวอย่าง: "MAP_7042|Kitchen"
        string[] parts = payload.Split('|');
        if (parts.Length != 2)
        {
            Debug.LogWarning($"[MapSelect] ❌ Invalid unlock payload: {payload}");
            return;
        }

        string unlockedId = parts[0];
        string unlockedMapType = parts[1];

        foreach (var m in maps)
        {
            // เทียบจาก mapType ตรง 100%
            if (m.mapType.ToString() == unlockedMapType)
            {
                m.unlocked = true;

                //  StoreMap sync runtime + save (กัน mismatch)
                storeUI.StoreMapRef.ForceUnlock(m.mapType);

                Debug.Log($"[MapSelect] ✔ Map unlocked: {m.mapType}");

                // อัปเดต UI ทันที
                RefreshUI();

                // ปิดทุก element ที่เกี่ยวกับการล็อกทันที
                lockedIcon?.SetActive(false);
                greyOverlay?.gameObject.SetActive(false);
                keyText?.gameObject.SetActive(false);
                keyIcon?.SetActive(false);

                // คลิกภาพเพื่อเข้าเล่นได้แล้ว
                previewImageButton.interactable = true;

                // เอฟเฟกต์ Glow
                StartCoroutine(PlayUnlockGlow());
                return;
            }
        }

        Debug.LogWarning($"[MapSelect] ⚠ Map unlocked but not in list: {payload}");
    }


    private IEnumerator PlayUnlockGlow()
    {
        if (glowEffect == null) yield break;

        Color c = glowEffect.color;
        float t = 0f;

        // Fade In
        while (t < 1f)
        {
            t += Time.deltaTime * (1f / glowDuration);
            c.a = Mathf.Lerp(0f, 1f, t);
            glowEffect.color = c;
            yield return null;
        }

        // Fade Out
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * (1f / glowDuration);
            c.a = Mathf.Lerp(1f, 0f, t);
            glowEffect.color = c;
            yield return null;
        }
    }

    #endregion

}
