using UnityEngine;
using System.IO;


public class SaveSystem : MonoBehaviour
{
    #region Fields
    private string _saveFilePath;
    private string _backupFilePath;
    private GameProgressData _progressData;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
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
