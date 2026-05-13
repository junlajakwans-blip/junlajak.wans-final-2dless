using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

[InitializeOnLoad]
public class AIControlWindow : EditorWindow
{
    private static HttpListener listener;
    private static Thread listenerThread;
    private static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private static List<string> consoleLogs = new List<string>();
    private static int activePort = 8221;

    static AIControlWindow()
    {
        // FORCE BACKGROUND EXECUTION
        PlayerSettings.runInBackground = true;
        EditorPrefs.SetBool("VerifyOutsideModify", false); // Don't ask for reload
        
        Application.logMessageReceivedThreaded += (log, stack, type) => {
            lock(consoleLogs) {
                consoleLogs.Add($"[{type}] {log}");
                if (consoleLogs.Count > 50) consoleLogs.RemoveAt(0);
            }
        };
        
        EditorApplication.update -= UpdateLoopStatic;
        EditorApplication.update += UpdateLoopStatic;
        StartServer();
    }

    [MenuItem("Window/Antigravity/AI Control Center")]
    public static void ShowWindow()
    {
        GetWindow<AIControlWindow>("AI Control");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI COMMAND CENTER - (BACKGROUND ACTIVE)", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        GUILayout.Label($"Status: {(EditorApplication.isPlaying ? "PLAYING" : "STOPPED")}");
        GUILayout.Label($"Port: {activePort}");
        
        if (GUILayout.Button("ATTACH BRIDGE TO SCENE")) AttachBridge();
        if (GUILayout.Button("FORCE PLAY")) EditorApplication.isPlaying = true;
        if (GUILayout.Button("FORCE STOP")) EditorApplication.isPlaying = false;
        
        EditorGUILayout.Space();
        GUILayout.Label("Recent Logs:", EditorStyles.miniBoldLabel);
        lock(consoleLogs) {
            foreach(var log in consoleLogs.TakeLast(5)) GUILayout.Label(log, EditorStyles.miniLabel);
        }

        if (GUILayout.Button("START SCHOOL LEVEL (SOLO)"))
        {
            var gm = GameObject.FindObjectOfType<GameManager>();
            if (gm != null) gm.LoadGameLevel("MapSchool", MapType.School);
        }
    }

    private static void AttachBridge()
    {
        var existing = GameObject.Find("AI_Command_Bridge");
        if (existing == null)
        {
            var go = new GameObject("AI_Command_Bridge");
            go.AddComponent<AICommandBridge>();
            Debug.Log("🚀 [AI-BRIDGE] Bridge attached and active in background!");
        }
    }

    private static void UpdateLoopStatic()
    {
        while (mainThreadActions.TryDequeue(out Action action)) { action.Invoke(); }
        
        // Force Unity to keep updating even if not focused
        if (EditorApplication.isPlaying && !EditorApplication.isPaused)
        {
            // This helps keep the editor "alive" for the bridge
        }
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
            Debug.Log($"<color=lime>🚀 [AI-CONTROL] Editor Server active on Port {activePort} (Background Mode)</color>");
        } catch (Exception) { }
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
                if (path.EndsWith("/play")) { EditorApplication.isPlaying = true; SendResponse(context.Response, "{\"status\":\"playing\"}"); }
                else if (path.EndsWith("/stop")) { EditorApplication.isPlaying = false; SendResponse(context.Response, "{\"status\":\"stopped\"}"); }
                else if (path.EndsWith("/attach")) { AttachBridge(); SendResponse(context.Response, "{\"status\":\"attached\"}"); }
                else if (path.EndsWith("/logs")) { 
                    lock(consoleLogs) { SendResponse(context.Response, "{\"logs\":[" + string.Join(",", consoleLogs.Select(l => "\"" + l.Replace("\"", "\\\"").Replace("\n", " ") + "\"")) + "]}"); }
                }
                else if (path.EndsWith("/status")) { 
                    string status = "{\"isPlaying\":" + EditorApplication.isPlaying.ToString().ToLower() + ",\"scene\":\"" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\"}";
                    SendResponse(context.Response, status); 
                }
                else if (path.EndsWith("/hierarchy")) { SendResponse(context.Response, GetHierarchy()); }
                else if (path.EndsWith("/click")) {
                    string body = new StreamReader(context.Request.InputStream).ReadToEnd();
                    SendResponse(context.Response, ClickButton(body));
                }
                else { SendResponse(context.Response, "{\"error\":\"Not Found\"}", 404); }
            } catch (Exception e) { SendResponse(context.Response, "{\"error\":\"" + e.Message + "\"}", 500); }
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
        return "{\"status\":\"success\"}";
    }
}
