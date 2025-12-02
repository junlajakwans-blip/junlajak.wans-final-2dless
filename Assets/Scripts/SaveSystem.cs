using UnityEngine;
using System.IO;


public class SaveSystem : MonoBehaviour
{
    #region Fields
    private string _saveFilePath;
    private string _backupFilePath;
    public static SaveSystem Instance;
    private GameProgressData _progressData;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // --- Singleton & Persist ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();   // โหลดไฟล์หลังทำ Singleton
    }
    #endregion

    #region Methods
    public void Initialize()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        _backupFilePath = Path.Combine(Application.persistentDataPath, "save_backup.json");

        _progressData = new GameProgressData();

        LoadData();
    }

    public GameProgressData GetProgressData()
    {
        return _progressData;
    }

    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(_progressData, true);
            File.WriteAllText(_saveFilePath, json);
            BackupSaveFile();
            Debug.Log($" Save successful: {_saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($" Save failed: {e.Message}");
        }
    }

    public void LoadData()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                string json = File.ReadAllText(_saveFilePath);
                _progressData = JsonUtility.FromJson<GameProgressData>(json);
                Debug.Log($"Loaded save file: {_saveFilePath}");
            }
            else
            {
                Debug.Log(" No save file found, creating new progress data.");
                _progressData = new GameProgressData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($" Load failed: {e.Message}");
        }
    }


    /// <summary>
    /// ตั้งค่า Global High Score ใหม่และบันทึกทันที
    /// </summary>
    public void SetGlobalHighScore(int newScore)
    {
        if (_progressData == null)
        {
            Debug.LogError("[SaveSystem] Progress Data is null! Cannot save high score.");
            return;
        }
        _progressData.GlobalHighScore = newScore;
        SaveData(); // บันทึกไฟล์ทันที
    }

    /// <summary>
    /// ดึงค่า Global High Score ที่บันทึกไว้
    /// </summary>
    public int GetGlobalHighScore()
    {
        if (_progressData == null)
        {
            LoadData(); 
            if (_progressData == null) return 0; // ป้องกัน Null
        }
        return _progressData.GlobalHighScore;
    }


    public void ResetData()
    {
        _progressData = new GameProgressData();
        SaveData();
        Debug.Log("Save data reset complete.");
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
                Debug.Log("🗑️ Save file deleted");
            }

            if (File.Exists(_backupFilePath))
            {
                File.Delete(_backupFilePath);
                Debug.Log("🗑️ Backup save deleted");
            }

            //  reset ค่า runtime ด้วย
            _progressData = new GameProgressData();

            // เซฟไฟล์ใหม่แบบค่าว่างเพื่อป้องกันการโหลดผิดในอนาคต
            SaveData();

            Debug.Log("🟩 Save deleted → recreated as empty GameProgressData");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ DeleteSave failed: {e.Message}");
        }
    }



    public void BackupSaveFile()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                File.Copy(_saveFilePath, _backupFilePath, true);
                Debug.Log($"Backup created at {_backupFilePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($" Backup failed: {e.Message}");
        }
    }
    #endregion
}
