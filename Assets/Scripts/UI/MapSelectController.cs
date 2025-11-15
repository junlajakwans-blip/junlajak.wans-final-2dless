using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Use for Manage Panel Select Map in Main Menu
/// </summary>
public class MapSelectController : MonoBehaviour
{
    #region Data
    [System.Serializable]
    public class MapInfo
    {
        public MapType mapType; //Call list from MapType Enum
        public string sceneName;
        public Sprite previewImage; //use Image in Art to Preview Map | Assets\ART\Scene\MapPreview
        public string mapName; 
        public int difficultyLevel;
        public string description;
        public bool unlocked;
    }

    private MapInfo[] maps;
    private int index = 0;
    #endregion

    #region UI
    [Header("UI References")]
    public Image previewImage;
    public TMP_Text mapNameText; //Call Auto Map Name form list
    public TMP_Text descriptionText; //For Edit Description Map in code
    public GameObject lockedIcon;
    public GameObject mainMenuPanel;
    public GameObject mapPanel;
    public Image[] difficultyIcons;
    public Button previewImageButton; 

    [Header("Map Sprites")]
    public Sprite schoolSprite;
    public Sprite roadSprite;
    public Sprite kitchenSprite;

    [Header("Key UI")]
    public TextMeshProUGUI text_KeyCount;

    #endregion

    #region Unity
    private void Start()
    {
        maps = new MapInfo[]
        {
            CreateMapInfo(MapType.School),
            CreateMapInfo(MapType.RoadTraffic),
            CreateMapInfo(MapType.Kitchen)
        };

        index = 0;
        RefreshUI();
    }

    private void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Space))
            TryPlaySelectedMap();
    }
    #endregion

    #region Build MapInfo
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

        // First Map School Awlays unlocks
        info.unlocked = type == MapType.School;

        return info;
    }
    #endregion

    #region Display
    private void RefreshUI()
    {
        Debug.Log($"Refresh map index = {index}");

        var map = maps[index];

        previewImage.sprite = map.previewImage;
        Debug.Log($" preview changed to {map.previewImage?.name}");

        mapNameText.text = map.mapName;
        descriptionText.text = map.description;

        for (int i = 0; i < difficultyIcons.Length; i++)
            difficultyIcons[i].gameObject.SetActive(i < map.difficultyLevel);

        int keyAmount = GameManager.Instance.GetCurrency().KeyMap;
        text_KeyCount.text = "x" + keyAmount;

        lockedIcon.SetActive(!map.unlocked);
        previewImageButton.interactable = map.unlocked;
    }
    #endregion

    #region Navigation
    public void NextMap()
    {
        Debug.Log("➡ NEXT clicked");
        index++;
        if (index >= maps.Length) index = 0;
        RefreshUI();
    }

    public void PrevMap()
    {
        Debug.Log("⬅ PREVIOUS clicked");
        index--;
        if (index < 0) index = maps.Length - 1; 
        RefreshUI();
    }

    public void BackToMainMenu()
    {
        mapPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    #endregion

    #region Play
    public void TryPlaySelectedMap() //If Map didn't unlock yet 
    {
        if (!maps[index].unlocked)
        {
            Debug.Log("Map Locked");
            return;
        }

        GameManager.Instance.LoadScene(maps[index].sceneName);
    }
    #endregion
}
