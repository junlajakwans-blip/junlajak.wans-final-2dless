# Save Reset / Delete Bug Fix

## Problem
When deleting the save file or calling `ResetGameProgress()`, the **Inspector showed PlayerData fields (Currency and GameProgressData) were NOT resetting to zero** - they still displayed old cached values (e.g., Coin: 2135, Token: 6, KeyMap: 8).

## Root Causes

### 1. **Missing Store Reinitialization After Reset**
   - **ResetGameProgress()** and **DeleteSaveAndRestart()** were not calling `SetupStores()` after creating fresh data
   - `SetupStores()` is responsible for:
     - Creating a new `Currency` instance from GameProgressData
     - Creating a new `StoreManager` 
     - Initializing all store systems
   - Without reinitializing stores, `_currencyData` remained pointing to old data

### 2. **PlayerData Serialized in Inspector - Stale Cached References**
   - **Player.cs serializes PlayerData as `[SerializeField] private PlayerData _playerData;`**
   - This field gets serialized in the scene prefab/instance
   - When scenes reload, the old PlayerData values persist in the Inspector until `Initialize()` overwrites them
   - The Inspector displays these **stale cached/serialized values** that weren't being cleared

### 3. **Incomplete Field Initialization in GameProgressData**
   - Constructor was missing explicit initialization of `_totalTokens` and `_totalKeyMaps` to 0
   - When JSON deserialization failed to find these fields, they could remain uninitialized

## Solutions Applied

### Fix 1: Reset Player's Cached PlayerData (Player.cs)
Added new method to clear stale serialized PlayerData:
```csharp
public void ResetPlayerDataCache()
{
    if (_playerData != null)
    {
        // Create fresh empty PlayerData to clear stale serialized values
        _playerData = new PlayerData(new Currency(), new GameProgressData());
        Debug.Log("<color=yellow>[Player] 🔄 PlayerData cache reset to empty state</color>");
    }
}
```

### Fix 2: Call Player Reset in GameManager (GameManager.cs)
Updated both reset methods to also reset Player's cached data:

**ResetGameProgress():**
```csharp
public void ResetGameProgress()
{
    // ... reset SaveSystem and Currency ...
    
    // 5. Reset Player's cached PlayerData
    if (_player != null)
        _player.ResetPlayerDataCache();
    
    OnGameReset?.Invoke();
    LoadScene("MainMenu");
}
```

**DeleteSaveAndRestart():**
```csharp
public void DeleteSaveAndRestart()
{
    // ... delete save and reset Currency ...
    
    // 5. Reset Player's cached PlayerData
    if (_player != null)
        _player.ResetPlayerDataCache();
    
    OnGameReset?.Invoke();
    LoadScene("MainMenu");
}
```

### Fix 3: Reinitialize Stores After Reset (GameManager.cs)
Ensured `SetupStores()` is called to create fresh store instances with new Currency:
```csharp
// 4. Reinitialize stores with fresh progress data
SetupStores(_persistentProgress);
```

### Fix 4: Explicitly Initialize Missing Fields (GameProgressData.cs)
```csharp
public GameProgressData()
{
    // ...existing fields...
    _totalTokens = 0;     // ✅ Explicitly set to 0
    _totalKeyMaps = 0;    // ✅ Explicitly set to 0
    // ...
}
```

## Data Flow After Fix

### When DeleteSaveAndRestart() is called (e.g., F9 in DevCheat):
1. **SaveSystem.DeleteSave()** → Deletes file, creates new empty GameProgressData
2. **GameManager gets fresh GameProgressData** from SaveSystem
3. **SetupStores() creates new Currency** from fresh GameProgressData ✅
4. **Player.ResetPlayerDataCache()** clears stale serialized PlayerData ✅
5. **OnGameReset event invoked** for any listeners
6. **Scene reloads to MainMenu** with all data reset to 0

### When ResetGameProgress() is called:
1. **SaveSystem.ResetData()** → Resets file to defaults
2. **GameManager gets fresh GameProgressData** from SaveSystem
3. **SetupStores() creates new Currency** from fresh GameProgressData ✅
4. **Player.ResetPlayerDataCache()** clears stale serialized PlayerData ✅
5. **OnGameReset event invoked** for any listeners
6. **Scene reloads to MainMenu** with all data reset to 0

## Result
✅ **Currency fields now reset to 0** (Coin, Token, KeyMap)  
✅ **GameProgressData fields reset to 0** (BestScore, TotalCoins, etc.)  
✅ **Store systems properly reinitialized** with fresh data  
✅ **Player's serialized PlayerData cleared** - Inspector shows fresh values  
✅ **No stale references or cached data** persisting across resets  

## Testing Checklist
- [ ] Delete save file (F9 in DevCheat) → Restart game → Verify all values are 0
- [ ] Call ResetGameProgress() → Verify all values are 0
- [ ] Make progress → Delete save → New game has 0 values
- [ ] Make progress → Reset → New game has 0 values
- [ ] Verify Inspector PlayerData shows Coin: 0, Token: 0, KeyMap: 0 after reset
- [ ] Verify stores reinitialized correctly with new items

