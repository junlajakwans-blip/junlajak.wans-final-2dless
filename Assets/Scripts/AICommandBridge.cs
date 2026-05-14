using UnityEngine;
using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine.UI;

public class AICommandBridge : MonoBehaviour
{
    private static HttpListener listener;
    private static Thread listenerThread;
    private static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private static int activePort = 8222;
    private static string lastStatus = "Idle";
    private static Color statusColor = Color.green;

    private bool showGUI = false;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        StartServer();
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action)) { action.Invoke(); }

        // Toggle visibility with '*' key (matches DevCheat)
        if (Input.GetKeyDown(KeyCode.KeypadMultiply) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha8)))
        {
            showGUI = !showGUI;
        }
    }

    void OnDestroy()
    {
        StopServer();
    }

    void OnGUI()
    {
        if (!showGUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;

        Rect rect = new Rect(10, 10, 250, 80);
        GUI.Box(rect, "", style);

        GUILayout.BeginArea(new Rect(20, 20, 230, 70));
        GUILayout.Label($"<b><color=#00ff00>ANTIGRAVITY AI</color></b>", style);
        GUILayout.Label($"Status: {lastStatus}", style);
        GUILayout.Label($"Bridge: localhost:{activePort}", style);
        GUILayout.EndArea();
    }

    private static void StartServer()
    {
        if (listener != null && listener.IsListening) return;
        try {
            StopServer();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{activePort}/unity/");
            listener.Start();
            listenerThread = new Thread(ListenForRequests);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            lastStatus = "Bridge Active";
            Debug.Log($"<color=cyan>🌐 [AI-BRIDGE] Game Bridge active on Port {activePort}</color>");
        } catch (Exception) { lastStatus = "Bridge Error"; }
    }

    private static void StopServer()
    {
        try { if (listener != null) { listener.Stop(); listener.Close(); } } catch {}
        listener = null;
    }

    private static void ListenForRequests()
    {
        while (listener != null && listener.IsListening)
        {
            try { var context = listener.GetContext(); ProcessRequest(context); } catch { }
        }
    }

    private static void ProcessRequest(HttpListenerContext context)
    {
        string path = context.Request.Url.AbsolutePath;
        mainThreadActions.Enqueue(() => {
            try {
                lastStatus = $"Request: {path}";
                if (path.EndsWith("/status")) { 
                    string status = "{\"isPlaying\":true,\"scene\":\"" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\"}";
                    SendResponse(context.Response, status); 
                }
                else if (path.EndsWith("/hierarchy")) { SendResponse(context.Response, GetHierarchy()); }
                else if (path.EndsWith("/click")) {
                    string body = new StreamReader(context.Request.InputStream).ReadToEnd();
                    SendResponse(context.Response, ClickButton(body));
                }
                else { SendResponse(context.Response, "{\"error\":\"Not Found\"}", 404); }
            } catch (Exception e) { 
                lastStatus = "Error: " + e.Message;
                SendResponse(context.Response, "{\"error\":\"" + e.Message + "\"}", 500); 
            }
        });
    }

    private static void SendResponse(HttpListenerResponse response, string data, int statusCode = 200)
    {
        try {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            response.StatusCode = statusCode;
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        } catch {}
    }

    private static string GetHierarchy()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        return "[" + string.Join(",", roots.Select(r => SerializeGameObject(r))) + "]";
    }

    private static string SerializeGameObject(GameObject go)
    {
        string active = go.activeInHierarchy ? "[+]" : "[-]";
        string children = string.Join(",", Enumerable.Range(0, go.transform.childCount).Select(i => SerializeGameObject(go.transform.GetChild(i).gameObject)));
        return "{\"name\":\"" + active + " " + go.name + "\", \"children\":[" + children + "]}";
    }

    [Serializable] private class ClickData { public string objectName; }
    private static string ClickButton(string jsonBody)
    {
        var data = JsonUtility.FromJson<ClickData>(jsonBody);
        Button btn = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b => b.name.Trim() == data.objectName.Trim());
        if (btn == null) return "{\"error\":\"Not found\"}";
        btn.onClick.Invoke();
        lastStatus = $"Clicked: {btn.name}";
        return "{\"status\":\"success\"}";
    }
}
