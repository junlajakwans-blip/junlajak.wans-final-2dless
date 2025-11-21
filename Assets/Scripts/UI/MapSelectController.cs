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
                info.sceneName = "Map_School";
                info.previewImage = schoolSprite;
                info.mapName = "School Zone";
                info.description = "NO DUFF NO DUCK";
                info.difficultyLevel = 1;
                break;

            case MapType.RoadTraffic:
                info.sceneName = "Map_Road";
                info.previewImage = roadSprite;
                info.mapName = "City Road";
                info.description = "DUCK DUCK — HONK!!";
                info.difficultyLevel = 2;
                break;

            case MapType.Kitchen:
                info.sceneName = "Map_Kitchen";
                info.previewImage = kitchenSprite;
                info.mapName = "Kitchen Mayhem";
                info.description = "TODAY MENU IS ROAST DUCK, Yummy!";
                info.difficultyLevel = 3;
                break;
        }

        // ด่านแรก (School) ปลดล็อกเสมอ
        info.unlocked = (type == MapType.School);

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
        {
            Debug.LogWarning("[MapSelectController] maps is empty.");
            return;
        }

        if (index < 0 || index >= maps.Length)
        {
            Debug.LogWarning("[MapSelectController] index out of range.");
            index = 0;
        }

        var map = maps[index];
        Debug.Log($"[MapSelectController] Refresh map index = {index}, type = {map.mapType}");

        // รูปและข้อความหลัก
        if (previewImage != null)
            previewImage.sprite = map.previewImage;

        if (mapNameText != null)
            mapNameText.text = map.mapName;

        if (descriptionText != null)
            descriptionText.text = map.description;

        // ไอคอนความยาก (เปิดเฉพาะที่น้อยกว่า difficultyLevel)
        if (difficultyIcons != null)
        {
            for (int i = 0; i < difficultyIcons.Length; i++)
            {
                if (difficultyIcons[i] != null)
                    difficultyIcons[i].gameObject.SetActive(i < map.difficultyLevel);
            }
        }

        // ไอคอนล็อก และคลิกภาพได้/ไม่ได้
        if (lockedIcon != null)
            lockedIcon.SetActive(!map.unlocked);

        if (previewImageButton != null)
            previewImageButton.interactable = map.unlocked;

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

        if (!map.unlocked)
        {
            Debug.Log("[MapSelectController] Map locked – cannot play.");
            return;
        }

        var gm = _gameManagerRef;
        if (gm == null)
        {
            Debug.LogError("[MapSelectController] GameManager.Instance is null – cannot load scene.");
            return;
        }

        Debug.Log($"[MapSelectController] Load scene: {map.sceneName}");
        gm.LoadScene(map.sceneName);
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

    private void OnMapUnlocked(string id)
    {
        foreach (var m in maps)
        {
            if (m.mapType.ToString() == id || m.mapType.ToSceneName() == id)
            {
                m.unlocked = true;
                Debug.Log($"[MapSelectController] 🟢 Map unlocked → {m.mapType}");
                RefreshUI();
                return;
            }
        }

        Debug.LogWarning($"[MapSelectController] ⚠ Map unlocked from store but not found in MapSelect list: {id}");
    }


    #endregion

}
