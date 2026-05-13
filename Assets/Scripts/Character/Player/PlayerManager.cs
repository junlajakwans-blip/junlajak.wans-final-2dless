using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// PlayerManager - จัดการการเกิดของ Player และ UI HealthBar
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private Player _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPointP1;
    [SerializeField] private Transform _spawnPointP2;

    [Header("UI Prefab")]
    [SerializeField] private HealthBarUI _healthBarPrefab;

    private Transform _anchorP1;
    private Transform _anchorP2;
    private List<Player> _players = new List<Player>();

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 1. โหลด HealthBar Prefab จาก Resources ถ้าใน Inspector ไม่ได้ลากใส่ไว้
        if (_healthBarPrefab == null)
        {
            _healthBarPrefab = Resources.Load<HealthBarUI>("UI/Panel_HealthBar");
        }

        if (_healthBarPrefab == null)
        {
            Debug.LogError("[PlayerManager] Load prefab failed: Resources/UI/Panel_HealthBar");
            return;
        }

        // 2. ค้นหา Anchor สำหรับเกาะ UI
    var canvas = GameObject.Find("Canvas_HUD"); 
        if (canvas != null)
        {
            // ใช้ GetChild หรือ Find แบบไม่ระบุ Path เต็ม
            _anchorP1 = RecursiveFind(canvas.transform, "UI_HealthBar_P1");
            _anchorP2 = RecursiveFind(canvas.transform, "UI_HealthBar_P2");
        }

        if (_anchorP1 == null || _anchorP2 == null)
            Debug.LogError("[PlayerManager] UI Anchors still missing! Check Object Names.");
    }

    private Transform RecursiveFind(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = RecursiveFind(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Clamp a RectTransform's anchoredPosition so it stays inside its parent's rect (with padding)
    private void ClampToParent(RectTransform rt, float padding = 8f)
    {
        if (rt == null || rt.parent == null) return;
        var parentRt = rt.parent as RectTransform;
        if (parentRt == null) return;

        Vector2 anchored = rt.anchoredPosition;
        Vector2 halfSize = rt.rect.size * 0.5f;
        Rect parentRect = parentRt.rect;

        float minX = parentRect.xMin + halfSize.x + padding;
        float maxX = parentRect.xMax - halfSize.x - padding;
        float minY = parentRect.yMin + halfSize.y + padding;
        float maxY = parentRect.yMax - halfSize.y - padding;

        anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
        anchored.y = Mathf.Clamp(anchored.y, minY, maxY);

        rt.anchoredPosition = anchored;
    }

    private void Start()
    {
        FindSpawnPoints();
        StartCoroutine(SpawnPlayers());
    }

    private IEnumerator SpawnPlayers()
    {

        _players.Clear();

        // ดึงจำนวน Player จาก GameModeManager
        int playerCount = GameModeManager.Instance != null ? GameModeManager.Instance.PlayerCount : 1;

        Transform[] spawnPositions = new Transform[] { _spawnPointP1, _spawnPointP2 };

        for (int i = 0; i < playerCount; i++)
        {
            int playerID = i + 1;
            Transform spawnPoint = spawnPositions[i];

            // ถ้าหาจุดเกิดไม่เจอ ให้ Backup ไปที่ P1
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[PlayerManager] SpawnPoint P{playerID} missing! Fallback to P1.");
                spawnPoint = _spawnPointP1;
            }

            if (spawnPoint == null) continue;

            // สปาว Player
            var player = Instantiate(_playerPrefab, spawnPoint.position, Quaternion.identity);
            player.name = $"Player_{playerID}";
            _players.Add(player);

            // เก็บ Reference
            if (playerID == 1) Player1 = player;
            else if (playerID == 2) Player2 = player;

            // ตั้งค่า UI และ ID
            SetupPlayer(player, playerID);

            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        Debug.Log($"[PlayerManager] Successfully spawned {_players.Count} players");
        Debug.Log("MODE = " + GameModeManager.Instance.CurrentMode);
        Debug.Log("COUNT = " + GameModeManager.Instance.PlayerCount);
        
        // If ScoreUI is available, ensure each player has its own ScoreUI instance (clone for P2 if needed)
        if (UIManager.Instance != null && GameManager.Instance != null)
        {
            var scoreUI = UIManager.Instance.GetScoreUI();
            int baseline = GameManager.Instance.GetCurrency() != null ? GameManager.Instance.GetCurrency().Coin : 0;
            if (scoreUI != null)
            {
                // Create distinct ScoreUI instances per player to avoid sharing one UI between players
                ScoreUI scoreUI_P1 = null;
                ScoreUI scoreUI_P2 = null;

                var canvasHUD = GameObject.Find("Canvas_HUD");
                Transform scoreParent = canvasHUD != null ? RecursiveFind(canvasHUD.transform, "Panel_Score")?.parent : null;
                var parentForInstantiate = scoreParent != null ? scoreParent : (canvasHUD != null ? canvasHUD.transform : null);

                if (_players.Count >= 2)
                {
                    // Instantiate two separate UI clones for P1 and P2 so they won't overlap or conflict
                    if (parentForInstantiate != null)
                    {
                        var go1 = Instantiate(scoreUI.gameObject, parentForInstantiate);
                        go1.name = "ScoreUI_P1";
                        scoreUI_P1 = go1.GetComponent<ScoreUI>();

                        var go2 = Instantiate(scoreUI.gameObject, parentForInstantiate);
                        go2.name = "ScoreUI_P2";
                        scoreUI_P2 = go2.GetComponent<ScoreUI>();

                        if (scoreUI_P1 != null) scoreUI_P1.SetPlayerNumber(1);
                        if (scoreUI_P2 != null) scoreUI_P2.SetPlayerNumber(2);
                        Debug.Log($"[PlayerManager] Spawned ScoreUI_P1 and ScoreUI_P2 under {parentForInstantiate.name}.");
                    }
                    else
                    {
                        // No Canvas_HUD found — fallback to using existing scoreUI as P1 and try to clone for P2
                        scoreUI_P1 = scoreUI;
                        var go2 = Instantiate(scoreUI.gameObject);
                        go2.name = "ScoreUI_P2";
                        scoreUI_P2 = go2.GetComponent<ScoreUI>();
                        if (scoreUI_P1 != null) scoreUI_P1.SetPlayerNumber(1);
                        if (scoreUI_P2 != null) scoreUI_P2.SetPlayerNumber(2);
                        Debug.Log("[PlayerManager] Canvas_HUD missing — created ScoreUI_P2 without specific parent.");
                    }
                }
                else
                {
                    // Single player — use the existing ScoreUI as P1
                    scoreUI_P1 = scoreUI;
                    if (scoreUI_P1 != null) scoreUI_P1.SetPlayerNumber(1);
                }

                // 🔥 ROBUST UI PLACEMENT (Fallback for missing anchors)
                if (scoreUI_P1 != null && _anchorP1 == null)
                {
                    var rt = scoreUI_P1.GetComponent<RectTransform>();
                    if (rt != null) {
                        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                        rt.anchoredPosition = new Vector2(150, -50);
                    }
                }
                if (scoreUI_P2 != null && _anchorP2 == null)
                {
                    var rt = scoreUI_P2.GetComponent<RectTransform>();
                    if (rt != null) {
                        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                        rt.anchoredPosition = new Vector2(-150, -50);
                    }
                }

                // Prefer anchoring per-player ScoreUI above the healthbar anchors (UL positions)
                bool anchoredPerHealthbar = false;
                if (_anchorP1 != null)
                {
                    var rt1 = scoreUI_P1.GetComponent<RectTransform>();
                    var rtAnchor1 = _anchorP1.GetComponent<RectTransform>();
                    if (rt1 != null && rtAnchor1 != null)
                    {
                        scoreUI_P1.transform.SetParent(rtAnchor1, false);
                            // place above the healthbar and to the left edge
                            var parentRt1 = rt1.parent as RectTransform;
                            float marginX = 80f;
                            float yPos1 = rtAnchor1.rect.height + 20f;
                            float xPos1 = 0f;
                            if (parentRt1 != null)
                            {
                                xPos1 = parentRt1.rect.xMin + marginX + rt1.rect.width * 0.5f;
                            }
                            rt1.anchoredPosition = new Vector2(xPos1, yPos1);
                            ClampToParent(rt1);
                        scoreUI_P1.SetSideMode(false); // use central score text for clarity
                        try { scoreUI_P1.ForceShowAll(); } catch { }
                        anchoredPerHealthbar = true;
                    }
                }

                if (scoreUI_P2 != null)
                {
                    scoreUI_P2.SetPlayerNumber(2);
                    if (_anchorP2 != null)
                    {
                        var rt2 = scoreUI_P2.GetComponent<RectTransform>();
                        var rtAnchor2 = _anchorP2.GetComponent<RectTransform>();
                        if (rt2 != null && rtAnchor2 != null)
                        {
                            scoreUI_P2.transform.SetParent(rtAnchor2, false);
                            // place above the healthbar and to the right edge
                            var parentRt2 = rt2.parent as RectTransform;
                            float yPos2 = rtAnchor2.rect.height + 20f;
                            float xPos2 = 0f;
                            float marginXR = 80f;
                            if (parentRt2 != null)
                            {
                                xPos2 = parentRt2.rect.xMax - marginXR - rt2.rect.width * 0.5f;
                            }
                            rt2.anchoredPosition = new Vector2(xPos2, yPos2);
                            ClampToParent(rt2);
                            scoreUI_P2.SetSideMode(false);
                            try { scoreUI_P2.ForceShowAll(); } catch { }
                            anchoredPerHealthbar = true;
                        }
                    }
                }

                // If we didn't anchor to healthbars, fall back to coin-area side-mode behaviour
                if (!anchoredPerHealthbar && _players.Count >= 2)
                {
                    scoreUI_P1.SetSideMode(true);
                    try { scoreUI_P1.ForceShowAll(); } catch { }
                    if (scoreUI_P2 != null)
                    {
                        scoreUI_P2.SetSideMode(true);
                        try { scoreUI_P2.ForceShowAll(); } catch { }
                    }
                }

                // If two players, try to place each ScoreUI near the coin placeholders (left/right) so score appears at coin locations
                if (_players.Count >= 2)
                {
                    var canvasHudRef = GameObject.Find("Canvas_HUD");
                    if (canvasHudRef != null)
                    {
                        // try a few common coin placeholder names
                        string[] coinNames = new string[] { "Image_Coin_P1", "Text_Coin_P1", "Image_Coin", "Text_Coin", "Coin", "Coins" };
                        Transform coinP1 = null;
                        Transform coinP2 = null;

                        var panelHud = RecursiveFind(canvasHudRef.transform, "Panel_HUD");
                        var panelScore = panelHud != null ? RecursiveFind(panelHud, "Panel_Score") : RecursiveFind(canvasHudRef.transform, "Panel_Score");

                        foreach (var n in coinNames)
                        {
                            if (coinP1 == null && panelScore != null)
                                coinP1 = RecursiveFind(panelScore, n);
                            if (coinP1 == null)
                                coinP1 = RecursiveFind(canvasHudRef.transform, n);
                            if (coinP1 != null) break;
                        }

                        // try to find a P2 placeholder specifically
                        string[] coinNamesP2 = new string[] { "Image_Coin_P2", "Text_Coin_P2", "Image_Coin", "Text_Coin" };
                        foreach (var n in coinNamesP2)
                        {
                            if (coinP2 == null && panelScore != null)
                                coinP2 = RecursiveFind(panelScore, n);
                            if (coinP2 == null)
                                coinP2 = RecursiveFind(canvasHudRef.transform, n);
                            if (coinP2 != null && coinP2 != coinP1) break;
                            // ensure p2 is different from p1
                            if (coinP2 == coinP1) coinP2 = null;
                        }

                        // Re-parent scoreUI_P1 to coinP1 parent and match position
                        if (coinP1 != null && scoreUI_P1 != null)
                        {
                            var rtSrc = scoreUI_P1.GetComponent<RectTransform>();
                            var rtDst = coinP1.GetComponent<RectTransform>();
                            if (rtDst != null && rtSrc != null)
                            {
                                scoreUI_P1.transform.SetParent(rtDst.parent, false);
                                rtSrc.anchoredPosition = rtDst.anchoredPosition;
                                ClampToParent(rtSrc);
                            }
                            else
                            {
                                scoreUI_P1.transform.SetParent(coinP1.parent, false);
                                scoreUI_P1.transform.localPosition = coinP1.localPosition;
                            }
                        }

                        // For P2, if specific placeholder found use it, otherwise place near coinP1 with offset
                        if (scoreUI_P2 != null)
                        {
                            if (coinP2 != null)
                            {
                                var rtSrc2 = scoreUI_P2.GetComponent<RectTransform>();
                                var rtDst2 = coinP2.GetComponent<RectTransform>();
                                if (rtDst2 != null && rtSrc2 != null)
                                {
                                    scoreUI_P2.transform.SetParent(rtDst2.parent, false);
                                    rtSrc2.anchoredPosition = rtDst2.anchoredPosition;
                                    ClampToParent(rtSrc2);
                                }
                                else
                                {
                                    scoreUI_P2.transform.SetParent(coinP2.parent, false);
                                    scoreUI_P2.transform.localPosition = coinP2.localPosition;
                                }
                            }
                            else if (coinP1 != null)
                            {
                                // place P2 beside P1 coin placeholder (use P1 rect as reference)
                                var rtP1 = scoreUI_P1.GetComponent<RectTransform>();
                                var rtP2 = scoreUI_P2.GetComponent<RectTransform>();
                                if (rtP1 != null && rtP2 != null)
                                {
                                    // parent P2 to same parent as P1 so anchored positions are comparable
                                    scoreUI_P2.transform.SetParent(rtP1.parent, false);
                                    // try place to the right; if overflow, place to left
                                    rtP2.anchoredPosition = rtP1.anchoredPosition + new Vector2(120f, 0f);
                                    ClampToParent(rtP2);
                                    // if still outside bounds (clamped to same pos), try left side
                                    if (Mathf.Approximately(rtP2.anchoredPosition.x, rtP1.anchoredPosition.x))
                                    {
                                        rtP2.anchoredPosition = rtP1.anchoredPosition + new Vector2(-120f, 0f);
                                        ClampToParent(rtP2);
                                    }
                                }
                                else if (rtP1 != null)
                                {
                                    scoreUI_P2.transform.SetParent(rtP1.parent, false);
                                    scoreUI_P2.transform.localPosition = scoreUI_P1.transform.localPosition + new Vector3(120f, 0f, 0f);
                                    var rtTmp = scoreUI_P2.GetComponent<RectTransform>();
                                    ClampToParent(rtTmp);
                                }
                                else
                                {
                                    scoreUI_P2.transform.localPosition = scoreUI_P1.transform.localPosition + new Vector3(120f, 0f, 0f);
                                    var rtTmp2 = scoreUI_P2.GetComponent<RectTransform>();
                                    ClampToParent(rtTmp2);
                                }
                            }
                        }
                    }
                }

                // Hook players: P1 -> scoreUI_P1, P2 -> scoreUI_P2 (fallback to P1 if P2 UI missing)
                if (Player1 != null)
                    Player1.HookScoreUI(scoreUI_P1, baseline, 1);
                if (Player2 != null)
                    Player2.HookScoreUI(scoreUI_P2 ?? scoreUI_P1, baseline, 2);

                Debug.Log("[PlayerManager] Hooked ScoreUI to spawned players.");
            
                // Debug: list all ScoreUI instances after spawn to diagnose placement/visibility
                var allScoreUIs = Object.FindObjectsByType<ScoreUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"[PlayerManager] ScoreUI count after spawn: {allScoreUIs.Length}");
                foreach (var s in allScoreUIs)
                {
                    var rt = s.GetComponent<RectTransform>();
                    Debug.Log($"[PlayerManager] ScoreUI Instance: {s.name} | parent={s.transform.parent?.name} | activeInHierarchy={s.gameObject.activeInHierarchy} | anchoredPos={(rt != null ? rt.anchoredPosition.ToString() : "n/a")}");
                    try { s.DebugLogBindings(); } catch { Debug.Log("[PlayerManager] ScoreUI DebugLogBindings() missing on instance"); }
                }
            }
        }

        // Debug: list all HealthBarUI instances after spawn to help diagnose visibility issues
        var allHealthBars = Object.FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[PlayerManager] HealthBarUI count after spawn: {allHealthBars.Length}");
        foreach (var hb in allHealthBars)
        {
            Debug.Log($"[PlayerManager] HealthBar Instance: {hb.name} | parent={hb.transform.parent?.name} | activeInHierarchy={hb.gameObject.activeInHierarchy}");
        }
        
        
    }

    private void FindSpawnPoints()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var root in roots)
        {
            var p1 = root.transform.Find("Spawn_P1");
            var p2 = root.transform.Find("Spawn_P2");

            if (p1 != null) _spawnPointP1 = p1;
            if (p2 != null) _spawnPointP2 = p2;
        }

        if (_spawnPointP1 == null)
            Debug.LogError("Spawn_P1 NOT FOUND");

        if (_spawnPointP2 == null)
            Debug.LogError("Spawn_P2 NOT FOUND");
    }

    private void SetupPlayer(Player player, int id)
    {
        // 0. Setup Player Data & ID (Crucial for score/duckling/FX logic)
        PlayerData data = (id == 1) 
            ? (GameManager.Instance != null ? GameManager.Instance.GetPlayer1Data() : null)
            : (GameManager.Instance != null ? GameManager.Instance.GetPlayer2Data() : null);
            
        player.SetupPlayer(id, data);

        // 1. ตั้งค่า PlayerID ให้ Controller
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetPlayerID(id);

        // 2. จัดการ UI HealthBar
        Transform anchor = (id == 1) ? _anchorP1 : _anchorP2;

        if (anchor != null && _healthBarPrefab != null)
        {
            // สปาว UI ลงไปเกาะที่ Anchor (explicit parent + reset local transform)
            var uiInstance = Instantiate(_healthBarPrefab, anchor.position, Quaternion.identity, anchor);
            uiInstance.name = $"HealthBar_P{id}";
            // Ensure proper local placement
            uiInstance.transform.localPosition = Vector3.zero;
            uiInstance.transform.localRotation = Quaternion.identity;
            uiInstance.gameObject.SetActive(true);

            // If RectTransform is used, reset anchored position too
            var rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;

            uiInstance.Setup(player);

            // If anchors for P1 and P2 are identical, apply a small offset to P2 to avoid overlap
            if (id == 2 && _anchorP1 != null && _anchorP2 != null)
            {
                Vector3 p1pos = _anchorP1.position;
                Vector3 p2pos = _anchorP2.position;
                if (Vector3.Distance(p1pos, p2pos) < 0.001f)
                {
                    if (rt != null)
                    {
                        rt.anchoredPosition += new Vector2(100f, 0f);
                        Debug.Log("[PlayerManager] Anchor positions identical — applied offset to HealthBar_P2 anchoredPosition.");
                    }
                    else
                    {
                        uiInstance.transform.localPosition += new Vector3(0.5f, 0f, 0f);
                        Debug.Log("[PlayerManager] Anchor positions identical — applied small world offset to HealthBar_P2.");
                    }
                }
            }

            Debug.Log($"[PlayerManager] P{id} UI Attached to {anchor.name} | activeInHierarchy={uiInstance.gameObject.activeInHierarchy} | parent={uiInstance.transform.parent?.name}");
        }
        else
        {
            Debug.LogError($"[PlayerManager] Cannot setup UI for P{id}: Anchor or Prefab is NULL");
        }
    }

    public Player GetAlivePlayer()
    {
        return _players.FirstOrDefault(p => p != null && !p.IsDead);
    }

    public List<Player> GetAllPlayers()
    {
        return _players;
    }
}