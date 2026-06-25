# TECHNICAL DESIGN DOCUMENT — MATCH FACTORY (Unity 3D Match-3)

> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 2026-06-23  
> **Tác giả:** Game Architecture Team  
> **Engine:** Unity 2022.3 LTS (URP)  
> **Platform target:** iOS / Android  
> **Ngôn ngữ:** C# (.NET Standard 2.1)

---

## MỤC LỤC

1. [Kiến Trúc Tổng Thể](#1-kiến-trúc-tổng-thể)
2. [Design Patterns Sử Dụng](#2-design-patterns-sử-dụng)
3. [Danh Sách Prefabs](#3-danh-sách-prefabs)
4. [Danh Sách Scripts Chính](#4-danh-sách-scripts-chính)
5. [Hệ Thống Load Level](#5-hệ-thống-load-level)
6. [Hệ Thống Lưu Trạng Thái Game](#6-hệ-thống-lưu-trạng-thái-game)
7. [Game Events System](#7-game-events-system)
8. [Smart Sorting Logic (CollectionBar)](#8-smart-sorting-logic-collectionbar)
9. [Vacuum Power-up Selection Algorithm](#9-vacuum-power-up-selection-algorithm)
10. [Physics & Board Management](#10-physics--board-management)
11. [Scene Structure](#11-scene-structure)
12. [Naming Conventions & Coding Standards](#12-naming-conventions--coding-standards)

---

## 1. Kiến Trúc Tổng Thể

### 1.1 Folder Structure

```
Assets/
├── _Project/                          # Toàn bộ source của dự án
│   ├── Scripts/
│   │   ├── Core/                      # GameManager, Events, States
│   │   ├── Level/                     # LevelData SO, LevelLoader, LevelManager
│   │   ├── Board/                     # BoardManager, ItemController, Factory, Pool
│   │   ├── Collection/                # CollectionBar, CollectionSlot
│   │   ├── Quest/                     # QuestManager, QuestSlotUI
│   │   ├── Timer/                     # GameTimer
│   │   ├── PowerUps/                  # PowerUpManager, Command classes
│   │   ├── Save/                      # SaveManager, SaveData
│   │   ├── UI/                        # UIManager, HUD, ResultPanel, LevelSelect
│   │   ├── Audio/                     # AudioManager, SoundData
│   │   └── Animation/                 # ItemFlyAnimation, QuestFlipAnimation
│   ├── Prefabs/
│   │   ├── Items/                     # 3D item prefabs (Pear, Grape, ...)
│   │   ├── UI/                        # UI prefabs (QuestSlot, CollectionSlot, ...)
│   │   ├── VFX/                       # Particle system prefabs
│   │   └── Other/                     # Board bounds, camera rig
│   ├── ScriptableObjects/
│   │   ├── Levels/                    # Level_001.asset, Level_002.asset, ...
│   │   ├── Quests/                    # QuestData assets
│   │   └── Audio/                     # SoundData.asset
│   ├── Materials/
│   │   ├── Items/                     # PBR materials cho từng item
│   │   └── VFX/                       # Particle materials
│   ├── Models/                        # FBX / GLB models
│   ├── Textures/                      # Albedo, Normal, Metallic maps
│   ├── Animations/                    # Animation clips & controllers
│   ├── Audio/
│   │   ├── BGM/
│   │   └── SFX/
│   └── Scenes/
│       ├── MainMenu.unity
│       ├── LevelSelect.unity
│       ├── Gameplay.unity
│       └── Loading.unity
├── AddressableAssetsData/             # Addressables config
└── StreamingAssets/                   # Raw data files nếu cần
```

### 1.2 Các Layer / Module Chính

| Module | Trách nhiệm | Scripts chính |
|--------|------------|---------------|
| **Core** | GameState machine, event bus, game flow | `GameManager`, `GameEvents`, `GameState` |
| **Level** | Load/setup level, spawn items | `LevelData`, `LevelLoader`, `LevelManager` |
| **Board** | Quản lý 3D heap items, physics bounds | `BoardManager`, `ItemController`, `ItemFactory`, `ItemPool` |
| **Collection** | 7-slot bar, smart sort, match detect | `CollectionBar`, `CollectionSlot` |
| **Quest** | Quest tracking, win condition | `QuestManager`, `QuestSlotUI` |
| **Timer** | Đếm ngược, freeze support | `GameTimer` |
| **PowerUps** | Command pattern, 4 power-ups | `PowerUpManager`, `ICommand`, Command impls |
| **Save** | Persist data, mid-session save | `SaveManager`, `SaveData` |
| **UI** | Panel management, HUD | `UIManager`, `HUDController`, `ResultPanelController` |
| **Audio** | BGM + SFX playback | `AudioManager`, `SoundData` |
| **Animation** | Fly arc, flip, shuffle | `ItemFlyAnimation`, `QuestFlipAnimation` |

### 1.3 Dependency Graph

```
GameManager
    ├──► LevelManager ──► LevelLoader ──► BoardManager
    │                                         ├──► ItemFactory ──► ItemPool
    │                                         └──► ItemController
    ├──► QuestManager ◄── GameEvents.OnMatchCompleted
    │        └──► QuestSlotUI
    ├──► CollectionBar ◄── GameEvents.OnItemTapped
    │        └──► CollectionSlot
    ├──► GameTimer ──► GameEvents.OnTimerTick
    ├──► PowerUpManager ──► ICommand (Shuffle/Vacuum/Spring/IceGun)
    ├──► UIManager ──► HUDController / ResultPanelController
    ├──► AudioManager
    └──► SaveManager
```

> **Quy tắc dependency:** Các module cấp thấp (Board, Collection, Quest) KHÔNG được tham chiếu trực tiếp đến nhau. Giao tiếp qua `GameEvents` (event bus). Chỉ `GameManager` mới được giữ reference đến nhiều manager.

---

## 2. Design Patterns Sử Dụng

### 2.1 Singleton

**Lý do chọn:** Các manager cần truy cập global từ nhiều nơi mà không cần dependency injection phức tạp trong scope của mobile game này.

**Classes áp dụng:** `GameManager`, `AudioManager`, `SaveManager`, `UIManager`

```csharp
// Core/Singleton.cs
namespace MatchFactory.Core
{
    using UnityEngine;

    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}
```

### 2.2 Observer / Event System (C# Action)

**Lý do chọn:** Decoupling hoàn toàn giữa producer và consumer. Không cần reference trực tiếp. Dễ add/remove listeners.

**Events áp dụng:**
- `OnItemTapped` → CollectionBar lắng nghe
- `OnMatchCompleted` → QuestManager lắng nghe
- `OnQuestCompleted` → UIManager lắng nghe
- `OnGameWin` / `OnGameLose` → UIManager, AudioManager lắng nghe

```csharp
// Cách subscribe (trong CollectionBar.cs)
private void OnEnable()
{
    GameEvents.OnItemTapped += HandleItemTapped;
}

private void OnDisable()
{
    GameEvents.OnItemTapped -= HandleItemTapped;  // LUÔN unsubscribe để tránh memory leak
}

// Cách fire event (trong ItemController.cs)
private void OnMouseDown()  // hoặc touch handler
{
    GameEvents.OnItemTapped?.Invoke(this);
}
```

### 2.3 Object Pool

**Lý do chọn:** Tránh GC spike khi spawn/destroy liên tục hàng chục items. Đặc biệt quan trọng trên mobile.

**Classes áp dụng:** `ItemPool` (items 3D), VFX pooling

```csharp
// Board/ItemPool.cs
namespace MatchFactory.Board
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ItemPool : MonoBehaviour
    {
        [SerializeField] private int initialPoolSize = 30;
        
        private Dictionary<ItemType, Queue<ItemController>> _pools = new();
        private Dictionary<ItemType, ItemController> _prefabMap;

        public void Initialize(Dictionary<ItemType, ItemController> prefabMap)
        {
            _prefabMap = prefabMap;
            foreach (var kvp in prefabMap)
            {
                _pools[kvp.Key] = new Queue<ItemController>();
                for (int i = 0; i < initialPoolSize / prefabMap.Count; i++)
                    CreateNew(kvp.Key);
            }
        }

        public ItemController Get(ItemType type)
        {
            if (_pools[type].Count > 0)
            {
                var item = _pools[type].Dequeue();
                item.gameObject.SetActive(true);
                return item;
            }
            return CreateNew(type);
        }

        public void Return(ItemController item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(transform);
            _pools[item.ItemType].Enqueue(item);
        }

        private ItemController CreateNew(ItemType type)
        {
            var go = Instantiate(_prefabMap[type], transform);
            go.gameObject.SetActive(false);
            go.OnReturnToPool = () => Return(go);
            return go;
        }
    }
}
```

### 2.4 State Machine

**Lý do chọn:** Game flow rõ ràng, dễ debug, tránh logic rối khi xử lý các trạng thái khác nhau.

```csharp
// Core/GameManager.cs (phần state machine)
public void ChangeState(GameState newState)
{
    if (_currentState == newState) return;
    
    // Exit current state
    switch (_currentState)
    {
        case GameState.Playing:
            GameTimer.Instance.Pause();
            break;
    }

    _currentState = newState;

    // Enter new state
    switch (_currentState)
    {
        case GameState.Playing:
            GameTimer.Instance.Resume();
            break;
        case GameState.Paused:
            Time.timeScale = 0f;
            UIManager.Instance.ShowPausePanel();
            break;
        case GameState.Win:
            Time.timeScale = 1f;
            GameEvents.OnGameWin?.Invoke();
            SaveManager.Instance.RecordLevelComplete(LevelManager.Instance.CurrentLevelIndex, _earnedStars);
            break;
        case GameState.Lose:
            Time.timeScale = 1f;
            GameEvents.OnGameLose?.Invoke();
            break;
    }
}
```

### 2.5 Factory Pattern

**Lý do chọn:** Tập trung logic tạo items, dễ mở rộng thêm item type mới mà không sửa code chỗ khác.

```csharp
// Board/ItemFactory.cs
namespace MatchFactory.Board
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ItemFactory : MonoBehaviour
    {
        [SerializeField] private ItemPrefabEntry[] prefabEntries;
        
        [System.Serializable]
        public struct ItemPrefabEntry
        {
            public ItemType type;
            public ItemController prefab;
        }

        private Dictionary<ItemType, ItemController> _prefabMap;
        private ItemPool _pool;

        private void Awake()
        {
            _prefabMap = new Dictionary<ItemType, ItemController>();
            foreach (var entry in prefabEntries)
                _prefabMap[entry.type] = entry.prefab;
        }

        public void InjectPool(ItemPool pool)
        {
            _pool = pool;
            _pool.Initialize(_prefabMap);
        }

        public ItemController Create(ItemType type, Vector3 position, Quaternion rotation)
        {
            var item = _pool.Get(type);
            item.transform.position = position;
            item.transform.rotation = rotation;
            item.Initialize(type);
            return item;
        }

        public void Recycle(ItemController item)
        {
            _pool.Return(item);
        }
    }
}
```

### 2.6 Command Pattern

**Lý do chọn:** Power-ups cần Execute() và có thể Undo() (ví dụ Spring trả item về board). Dễ queue và replay.

```csharp
// PowerUps/ICommand.cs
namespace MatchFactory.PowerUps
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}

// PowerUps/ShuffleCommand.cs
namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using UnityEngine;

    public class ShuffleCommand : ICommand
    {
        private readonly BoardManager _board;
        private Vector3[] _savedPositions;
        private ItemController[] _items;

        public ShuffleCommand(BoardManager board) => _board = board;

        public bool CanExecute() => _board.ActiveItemCount > 0;

        public void Execute()
        {
            _items = _board.GetAllActiveItems();
            _savedPositions = new Vector3[_items.Length];
            for (int i = 0; i < _items.Length; i++)
                _savedPositions[i] = _items[i].transform.position;

            _board.ShuffleItems();
            GameEvents.OnShuffleUsed?.Invoke();
        }

        public void Undo()
        {
            // Khôi phục vị trí cũ nếu cần
            if (_items == null) return;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] != null)
                    _items[i].transform.position = _savedPositions[i];
            }
        }
    }
}
```

### 2.7 Data-Driven Design (ScriptableObject)

**Lý do chọn:** Designer có thể tạo/chỉnh level mà không cần sửa code. Dễ version control, dễ balance.

```csharp
// Level/LevelData.cs
namespace MatchFactory.Level
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "Level_001", menuName = "MatchFactory/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int levelIndex;
        public string levelName;
        
        [Header("Quests")]
        public QuestData[] quests;

        [Header("Timer")]
        public float timeLimitSeconds = 120f;

        [Header("Item Spawn")]
        public ItemSpawnConfig[] spawnConfigs;  // loại item và số lượng
        public int totalItemCount;               // tự tính từ spawnConfigs hoặc set manual
        
        [Header("Star Thresholds")]
        [Tooltip("Thời gian còn lại (giây) để đạt 3 sao")]
        public float threeStarTimeThreshold = 60f;
        [Tooltip("Thời gian còn lại (giây) để đạt 2 sao")]
        public float twoStarTimeThreshold = 20f;

        [System.Serializable]
        public struct ItemSpawnConfig
        {
            public ItemType itemType;
            [Tooltip("Phải là bội số của 3")]
            public int count;
        }

        private void OnValidate()
        {
            // Validation: tổng items phải là bội số của 3
            totalItemCount = 0;
            foreach (var cfg in spawnConfigs)
            {
                if (cfg.count % 3 != 0)
                    Debug.LogWarning($"[LevelData] {name}: ItemType {cfg.itemType} count ({cfg.count}) không phải bội số của 3!");
                totalItemCount += cfg.count;
            }
        }
    }
}
```

### 2.8 MVC/MVP Pattern (UI)

**Lý do chọn:** Tách biệt logic game (Model) khỏi hiển thị (View). `HUDController` là Presenter, nhận events từ Model (GameTimer, QuestManager) và cập nhật View (TextMeshPro, Images).

```csharp
// UI/HUDController.cs (Presenter)
private void OnEnable()
{
    GameEvents.OnTimerTick += UpdateTimerDisplay;
    GameEvents.OnQuestProgressUpdated += UpdateQuestDisplay;
}

private void UpdateTimerDisplay(float remainingSeconds)
{
    int mins = Mathf.FloorToInt(remainingSeconds / 60f);
    int secs = Mathf.FloorToInt(remainingSeconds % 60f);
    _timerText.text = $"{mins:00}:{secs:00}";
    _timerText.color = remainingSeconds < 10f ? Color.red : Color.white;
}
```

---

## 3. Danh Sách Prefabs

### 3.1 3D Item Prefabs

Tất cả item prefabs đặt tại: `Assets/_Project/Prefabs/Items/`

| Prefab Name | Mesh | Collider | Material |
|-------------|------|----------|----------|
| `Item_Pear` | Pear.fbx | MeshCollider (convex) | Mat_Pear (PBR) |
| `Item_Grape` | Grape.fbx | SphereCollider | Mat_Grape (PBR) |
| `Item_Cupcake` | Cupcake.fbx | MeshCollider (convex) | Mat_Cupcake (PBR) |
| `Item_Lollipop` | Lollipop.fbx | CapsuleCollider | Mat_Lollipop (PBR) |
| `Item_TennisBall` | Sphere (built-in) | SphereCollider | Mat_TennisBall (PBR) |
| `Item_IceCream` | IceCream.fbx | MeshCollider (convex) | Mat_IceCream (PBR) |
| `Item_Box` | Cube (built-in) | BoxCollider | Mat_Box (PBR) |
| `Item_Maracas` | Maracas.fbx | CapsuleCollider | Mat_Maracas (PBR) |

**Components bắt buộc trên mỗi Item Prefab:**

```
Item_Pear (GameObject)
├── MeshFilter
├── MeshRenderer          [Layer: Items]
├── MeshCollider          [Convex=true, IsTrigger=false]
├── Rigidbody             [Mass=1, Drag=0.5, AngularDrag=0.05, Interpolate=Interpolate]
├── ItemController        [Script]
└── Shadow Caster (URP)
```

**Lưu ý Rigidbody:** `useGravity = true`, `Collision Detection = Continuous Dynamic` để tránh tunneling khi drop nhiều items cùng lúc.

### 3.2 UI Prefabs

Đặt tại: `Assets/_Project/Prefabs/UI/`

**QuestSlot_Prefab:**
```
QuestSlot (RectTransform)
├── Background (Image)         [Sprite: slot_bg, Raycast Target: false]
├── ItemIcon (Image)           [Preserve Aspect: true]
├── CountText (TextMeshProUGUI) [Font: Outfit Bold, Size: 28]
├── CompletedOverlay (Image)   [Sprite: checkmark, Active: false]
├── Animator                   [Controller: QuestSlot_AC]
└── QuestSlotUI (Script)
```

**CollectionSlot_Prefab:**
```
CollectionSlot (RectTransform)   [Width=80, Height=80]
├── Background (Image)           [Sprite: slot_frame]
├── ItemPreview (RawImage)       [hoặc Image]  
├── SlotIndex (int, set via code)
└── CollectionSlot (Script)
```

**PowerUpButton_Prefab:**
```
PowerUpButton (RectTransform)
├── Button (Component)
├── ButtonBG (Image)
├── PowerUpIcon (Image)
├── CooldownOverlay (Image)      [Image Type: Filled, Fill Method: Radial360]
├── CountText (TextMeshProUGUI)  [hiển thị số lần còn lại]
└── PowerUpButtonUI (Script)
```

**ResultPanel_Prefab:**
```
ResultPanel (RectTransform)      [Full screen overlay]
├── DimBackground (Image)        [alpha=0.7]
├── Panel (RectTransform)
│   ├── TitleText (TextMeshProUGUI)  ["YOU WIN!" / "GAME OVER"]
│   ├── StarsContainer (HorizontalLayoutGroup)
│   │   ├── Star1 (Image)
│   │   ├── Star2 (Image)
│   │   └── Star3 (Image)
│   ├── NextButton (Button)
│   ├── RetryButton (Button)
│   └── HomeButton (Button)
└── ResultPanelController (Script)
```

**TimerDisplay_Prefab:**
```
TimerDisplay (RectTransform)
├── TimerBackground (Image)
├── TimerText (TextMeshProUGUI)  [Format "MM:SS"]
├── TimerIcon (Image)
└── TimerDisplayUI (Script)     [đổi màu khi còn <10s]
```

### 3.3 VFX Prefabs

Đặt tại: `Assets/_Project/Prefabs/VFX/`

| Prefab | Particle System Config | Mô tả |
|--------|----------------------|-------|
| `MatchBurst_VFX` | Burst: 20 particles, Lifetime: 0.6s, Shape: Sphere | Hiệu ứng khi match 3 items xoá khỏi bar |
| `QuestComplete_VFX` | Burst: 30 confetti, Lifetime: 1.2s, Gravity: -1 | Confetti nhỏ tại vị trí quest slot |
| `ShuffleExplode_VFX` | Continuous: 50/s, Duration: 0.5s, Shape: Hemisphere | Hiệu ứng vụ nổ nhẹ khi shuffle |
| `VacuumSwirl_VFX` | Continuous: 30/s, Velocity over Lifetime: swirl | Xoáy hút item vào vacuum |
| `IceFrost_VFX` | Burst: 40, Shape: Cone, Color: ice blue | Phun sương đóng băng timer |
| `WinConfetti_VFX` | Burst: 200, Lifetime: 3s, Gravity: 2, Looping: false | Confetti toàn màn hình khi win |
| `FlyToSlot_VFX` | Trail Renderer + Particle: 5/s, Lifetime: 0.3s | Trail theo item khi bay vào slot |

### 3.4 Other Prefabs

**BoardBound_Prefab:**
```
BoardBound (GameObject)
├── Floor (BoxCollider)          [size: 10x0.1x10, physic material: Bouncy]
├── WallLeft (BoxCollider)
├── WallRight (BoxCollider)
├── WallFront (BoxCollider)
└── WallBack (BoxCollider)
```

**GameCamera_Prefab:**
```
GameCamera (GameObject)
├── Camera                       [FOV: 60, Near: 0.1, Far: 100, ClearFlags: Skybox]
├── UniversalAdditionalCameraData
└── CameraController (Script)   [optional: slight idle sway animation]
```

---

## 4. Danh Sách Scripts Chính

### 4.1 Core/

#### `GameManager.cs`

```csharp
namespace MatchFactory.Core
{
    using MatchFactory.Level;
    using MatchFactory.UI;
    using MatchFactory.Save;
    using MatchFactory.Audio;
    using UnityEngine;

    /// <summary>
    /// Singleton trung tâm. Quản lý GameState machine và điều phối tất cả manager.
    /// Là entry point của gameplay scene.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // --- Public Properties ---
        public GameState CurrentState => _currentState;
        public int EarnedStars => _earnedStars;

        // --- Private Fields ---
        private GameState _currentState = GameState.Idle;
        private int _earnedStars = 0;

        // --- Dependencies (set via Inspector hoặc auto-find) ---
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;

        // --- Methods ---

        /// <summary>Khởi động gameplay khi scene load xong</summary>
        public void StartGame(int levelIndex)
        {
            ChangeState(GameState.Loading);
            levelManager.LoadLevel(levelIndex, OnLevelLoaded);
        }

        private void OnLevelLoaded()
        {
            ChangeState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
                ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                ChangeState(GameState.Playing);
            }
        }

        public void TriggerWin(float timeRemaining)
        {
            _earnedStars = CalculateStars(timeRemaining);
            ChangeState(GameState.Win);
        }

        public void TriggerLose()
        {
            ChangeState(GameState.Lose);
        }

        private int CalculateStars(float timeRemaining)
        {
            var data = LevelManager.Instance.CurrentLevelData;
            if (timeRemaining >= data.threeStarTimeThreshold) return 3;
            if (timeRemaining >= data.twoStarTimeThreshold) return 2;
            return 1;
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            // Exit
            switch (_currentState)
            {
                case GameState.Playing:
                    Timer.GameTimer.Instance?.Pause();
                    break;
                case GameState.Paused:
                    Time.timeScale = 1f;
                    break;
            }

            _currentState = newState;

            // Enter
            switch (_currentState)
            {
                case GameState.Loading:
                    uiManager.ShowLoadingScreen(true);
                    break;
                case GameState.Playing:
                    uiManager.ShowLoadingScreen(false);
                    Timer.GameTimer.Instance?.Resume();
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    uiManager.ShowPausePanel(true);
                    break;
                case GameState.Win:
                    audioManager.PlaySFX(SFXType.Win);
                    uiManager.ShowResultPanel(_earnedStars, won: true);
                    SaveManager.Instance.RecordLevelComplete(
                        LevelManager.Instance.CurrentLevelIndex, _earnedStars);
                    break;
                case GameState.Lose:
                    audioManager.PlaySFX(SFXType.Lose);
                    uiManager.ShowResultPanel(0, won: false);
                    break;
            }
        }
    }
}
```

#### `GameState.cs`

```csharp
namespace MatchFactory.Core
{
    public enum GameState
    {
        Idle,       // Chưa bắt đầu (màn hình chờ)
        Loading,    // Đang load level assets
        Playing,    // Đang chơi
        Paused,     // Tạm dừng
        Win,        // Thắng
        Lose        // Thua
    }
}
```

---

### 4.2 Level/

#### `QuestData.cs`

```csharp
namespace MatchFactory.Level
{
    using MatchFactory.Board;
    using UnityEngine;

    [System.Serializable]
    public class QuestData
    {
        public ItemType itemType;
        [Tooltip("Số lượng items cần thu thập")]
        public int requiredCount;
        public Sprite itemIcon;  // Reference đến icon trong UI

        // Runtime state (không serialize vào SO)
        [System.NonSerialized] public int currentCount;
        [System.NonSerialized] public bool isCompleted;

        public int RemainingCount => Mathf.Max(0, requiredCount - currentCount);
        
        public void AddProgress(int amount = 1)
        {
            currentCount = Mathf.Min(currentCount + amount, requiredCount);
            isCompleted = currentCount >= requiredCount;
        }

        public void Reset()
        {
            currentCount = 0;
            isCompleted = false;
        }
    }
}
```

#### `LevelLoader.cs`

```csharp
namespace MatchFactory.Level
{
    using MatchFactory.Board;
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    /// <summary>
    /// Load LevelData từ Addressables, khởi tạo board và spawn items.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private Quest.QuestManager questManager;
        [SerializeField] private Timer.GameTimer gameTimer;

        private const string LEVEL_ADDRESS_PREFIX = "Level_";

        public void LoadLevel(int levelIndex, Action onComplete)
        {
            StartCoroutine(LoadLevelCoroutine(levelIndex, onComplete));
        }

        private IEnumerator LoadLevelCoroutine(int levelIndex, Action onComplete)
        {
            string address = $"{LEVEL_ADDRESS_PREFIX}{levelIndex:000}";
            var handle = Addressables.LoadAssetAsync<LevelData>(address);
            yield return handle;

            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[LevelLoader] Không load được: {address}");
                yield break;
            }

            LevelData data = handle.Result;
            LevelManager.Instance.SetCurrentLevel(data);

            // Setup quest
            questManager.InitializeQuests(data.quests);

            // Setup timer
            gameTimer.Initialize(data.timeLimitSeconds);

            // Spawn items
            boardManager.SpawnItems(data);

            onComplete?.Invoke();
        }
    }
}
```

#### `LevelManager.cs`

```csharp
namespace MatchFactory.Level
{
    using MatchFactory.Core;
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class LevelManager : Singleton<LevelManager>
    {
        public LevelData CurrentLevelData { get; private set; }
        public int CurrentLevelIndex { get; private set; }

        public void SetCurrentLevel(LevelData data)
        {
            CurrentLevelData = data;
            CurrentLevelIndex = data.levelIndex;
        }

        public void LoadLevel(int levelIndex, Action onComplete)
        {
            PlayerPrefs.SetInt("PendingLevel", levelIndex);
            // Delegate to LevelLoader in scene
            FindObjectOfType<LevelLoader>()?.LoadLevel(levelIndex, onComplete);
        }

        public void LoadNextLevel()
        {
            int next = CurrentLevelIndex + 1;
            PlayerPrefs.SetInt("PendingLevel", next);
            SceneManager.LoadScene("Loading");
        }

        public void ReloadCurrentLevel()
        {
            PlayerPrefs.SetInt("PendingLevel", CurrentLevelIndex);
            SceneManager.LoadScene("Loading");
        }

        public void GoToLevelSelect()
        {
            SceneManager.LoadScene("LevelSelect");
        }
    }
}
```

---

### 4.3 Board/

#### `BoardManager.cs`

```csharp
namespace MatchFactory.Board
{
    using MatchFactory.Level;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Quản lý toàn bộ heap items 3D trên board.
    /// Chịu trách nhiệm spawn, track active items, và cung cấp API cho PowerUps.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        [Header("Board Bounds")]
        [SerializeField] private Bounds spawnBounds = new Bounds(Vector3.zero, new Vector3(4f, 3f, 4f));
        [SerializeField] private float spawnHeightStep = 0.4f;
        [SerializeField] private Transform spawnParent;

        [Header("References")]
        [SerializeField] private ItemFactory itemFactory;

        public int ActiveItemCount => _activeItems.Count;

        private List<ItemController> _activeItems = new List<ItemController>();
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        /// <summary>Spawn toàn bộ items từ LevelData</summary>
        public void SpawnItems(LevelData data)
        {
            ClearBoard();
            var spawnList = GenerateShuffledSpawnList(data);
            StartCoroutine(SpawnItemsCoroutine(spawnList));
        }

        private List<ItemType> GenerateShuffledSpawnList(LevelData data)
        {
            var list = new List<ItemType>();
            foreach (var cfg in data.spawnConfigs)
            {
                for (int i = 0; i < cfg.count; i++)
                    list.Add(cfg.itemType);
            }
            // Fisher-Yates shuffle
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private IEnumerator SpawnItemsCoroutine(List<ItemType> list)
        {
            int cols = 5;
            float spacing = spawnBounds.size.x / cols;

            for (int i = 0; i < list.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                Vector3 pos = spawnBounds.min + new Vector3(
                    col * spacing + spacing * 0.5f + Random.Range(-0.1f, 0.1f),
                    row * spawnHeightStep + 1f,
                    Random.Range(-spawnBounds.extents.z, spawnBounds.extents.z) * 0.5f
                );

                var item = itemFactory.Create(list[i], pos, Random.rotation);
                item.transform.SetParent(spawnParent);
                _activeItems.Add(item);

                if (i % 5 == 0) yield return null; // Spread spawn over frames
            }
        }

        public void RemoveItem(ItemController item)
        {
            _activeItems.Remove(item);
            itemFactory.Recycle(item);
        }

        public ItemController[] GetAllActiveItems() => _activeItems.ToArray();

        /// <summary>Shuffle: apply random impulse forces to all items</summary>
        public void ShuffleItems()
        {
            foreach (var item in _activeItems)
            {
                var rb = item.GetComponent<Rigidbody>();
                if (rb == null) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Vector3 impulse = new Vector3(
                    Random.Range(-3f, 3f),
                    Random.Range(2f, 5f),
                    Random.Range(-3f, 3f));
                rb.AddForce(impulse, ForceMode.Impulse);
            }
            GameEvents.OnShuffleUsed?.Invoke();
        }

        /// <summary>Tìm accessible items theo type cho Vacuum power-up</summary>
        public List<ItemController> GetAccessibleItems(ItemType type, int maxCount = 3)
        {
            var candidates = new List<ItemController>();
            foreach (var item in _activeItems)
                if (item.ItemType == type && !item.IsTapped && !item.IsFlying)
                    candidates.Add(item);

            // Sort by Y descending (items on top are less likely blocked)
            candidates.Sort((a, b) =>
                b.transform.position.y.CompareTo(a.transform.position.y));

            var result = new List<ItemController>();
            foreach (var item in candidates)
            {
                if (IsItemAccessible(item))
                    result.Add(item);
                if (result.Count >= maxCount) break;
            }

            // Fallback: nếu không đủ accessible, lấy bất kỳ
            if (result.Count < maxCount)
            {
                foreach (var item in candidates)
                {
                    if (!result.Contains(item))
                        result.Add(item);
                    if (result.Count >= maxCount) break;
                }
            }

            return result;
        }

        private bool IsItemAccessible(ItemController item)
        {
            // Raycast từ camera đến item, nếu hit item đầu tiên thì accessible
            Vector3 dir = item.transform.position - _mainCamera.transform.position;
            if (Physics.Raycast(_mainCamera.transform.position, dir.normalized,
                out RaycastHit hit, dir.magnitude + 0.1f, LayerMask.GetMask("Items")))
            {
                return hit.collider.gameObject == item.gameObject;
            }
            return false;
        }

        private void ClearBoard()
        {
            foreach (var item in _activeItems)
                itemFactory.Recycle(item);
            _activeItems.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);
        }
    }
}
```

#### `ItemController.cs`

```csharp
namespace MatchFactory.Board
{
    using MatchFactory.Core;
    using System;
    using UnityEngine;

    /// <summary>
    /// MonoBehaviour gắn trên mỗi 3D Item. Xử lý tap, fly animation, và physics state.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ItemController : MonoBehaviour
    {
        // --- Public ---
        public ItemType ItemType { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsTapped { get; private set; }
        public Action OnReturnToPool { get; set; }

        // --- Inspector ---
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject highlightOverlay;  // outline hoặc glow

        // --- Private ---
        private Rigidbody _rb;
        private Collider _col;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
        }

        public void Initialize(ItemType type)
        {
            ItemType = type;
            IsFlying = false;
            IsTapped = false;
            _rb.isKinematic = false;
            _col.enabled = true;
            if (highlightOverlay) highlightOverlay.SetActive(false);
        }

        // Tap handler - gọi từ InputManager trung tâm (Raycast)
        public void HandleTap()
        {
            if (IsTapped || IsFlying) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            IsTapped = true;
            _col.enabled = false;  // Vô hiệu tap tiếp
            _rb.isKinematic = true;

            if (highlightOverlay) highlightOverlay.SetActive(true);
            GameEvents.OnItemTapped?.Invoke(this);
        }

        /// <summary>Gọi từ bên ngoài (Vacuum) để trigger tap programmatically</summary>
        public void TriggerTapFromPowerUp()
        {
            HandleTap();
        }

        public void SetFlying(bool state)
        {
            IsFlying = state;
            _rb.isKinematic = state;
            _col.enabled = !state;
        }

        public void ReturnToPool()
        {
            IsFlying = false;
            IsTapped = false;
            OnReturnToPool?.Invoke();
        }
    }
}
```

#### `ItemType.cs`

```csharp
namespace MatchFactory.Board
{
    public enum ItemType
    {
        None = 0,
        Pear = 1,
        Grape = 2,
        Cupcake = 3,
        Lollipop = 4,
        TennisBall = 5,
        IceCream = 6,
        Box = 7,
        Maracas = 8
    }
}
```

---

### 4.4 Collection/

#### `CollectionBar.cs`

```csharp
namespace MatchFactory.Collection
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Quản lý thanh 7 ô chứa items. Thực thi smart sort và match detection.
    /// </summary>
    public class CollectionBar : MonoBehaviour
    {
        public const int MAX_SLOTS = 7;
        public const int MATCH_COUNT = 3;

        [SerializeField] private CollectionSlot[] slots = new CollectionSlot[MAX_SLOTS];
        [SerializeField] private Animation.ItemFlyAnimation flyAnimPrefab;

        // Runtime state
        private List<ItemController> _barItems = new List<ItemController>();

        private void OnEnable() => GameEvents.OnItemTapped += OnItemTapped;
        private void OnDisable() => GameEvents.OnItemTapped -= OnItemTapped;

        private void OnItemTapped(ItemController item)
        {
            StartCoroutine(AddItemToBar(item));
        }

        private IEnumerator AddItemToBar(ItemController item)
        {
            // Tìm target slot index theo smart sort
            int targetSlotIndex = FindSmartInsertIndex(item.ItemType);

            // Animate item bay đến slot
            item.SetFlying(true);
            var fly = Instantiate(flyAnimPrefab);
            yield return fly.FlyTo(item.transform, slots[targetSlotIndex].transform.position);
            Destroy(fly.gameObject);

            // Insert item vào danh sách
            InsertItem(item, targetSlotIndex);
            GameEvents.OnItemAddedToBar?.Invoke(item);

            // Kiểm tra match
            yield return CheckAndProcessMatch(item.ItemType);

            // Kiểm tra bar full
            if (_barItems.Count >= MAX_SLOTS)
            {
                GameEvents.OnCollectionBarFull?.Invoke();
                GameManager.Instance.TriggerLose();
            }

            // Refresh UI
            RefreshSlotUI();
        }

        /// <summary>Smart sort: tìm vị trí insert tối ưu</summary>
        private int FindSmartInsertIndex(ItemType type)
        {
            int lastSameTypeIndex = -1;
            for (int i = 0; i < _barItems.Count; i++)
            {
                if (_barItems[i].ItemType == type)
                    lastSameTypeIndex = i;
            }
            return lastSameTypeIndex >= 0 ? lastSameTypeIndex + 1 : _barItems.Count;
        }

        private void InsertItem(ItemController item, int index)
        {
            index = Mathf.Clamp(index, 0, _barItems.Count);
            _barItems.Insert(index, item);
        }

        private IEnumerator CheckAndProcessMatch(ItemType type)
        {
            int count = 0;
            int firstIndex = -1;
            for (int i = 0; i < _barItems.Count; i++)
            {
                if (_barItems[i].ItemType == type)
                {
                    if (firstIndex < 0) firstIndex = i;
                    count++;
                    if (count == MATCH_COUNT)
                    {
                        yield return ProcessMatch(type, firstIndex);
                        yield break;
                    }
                }
                else
                {
                    count = 0;
                    firstIndex = -1;
                }
            }
        }

        private IEnumerator ProcessMatch(ItemType type, int startIndex)
        {
            yield return new WaitForSeconds(0.1f);

            // Xoá 3 items từ cuối về đầu để không bị index lệch
            for (int i = startIndex + MATCH_COUNT - 1; i >= startIndex; i--)
            {
                var item = _barItems[i];
                _barItems.RemoveAt(i);
                item.ReturnToPool();
            }

            GameEvents.OnMatchCompleted?.Invoke(type);
        }

        private void RefreshSlotUI()
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (i < _barItems.Count)
                    slots[i].SetItem(_barItems[i]);
                else
                    slots[i].SetEmpty();
            }
            GameEvents.OnCollectionBarUpdated?.Invoke(MAX_SLOTS - _barItems.Count);
        }

        /// <summary>Spring power-up: trả item cuối cùng về board</summary>
        public ItemController PopLastItem()
        {
            if (_barItems.Count == 0) return null;
            var item = _barItems[_barItems.Count - 1];
            _barItems.RemoveAt(_barItems.Count - 1);
            RefreshSlotUI();
            return item;
        }

        public void Clear()
        {
            foreach (var item in _barItems)
                item.ReturnToPool();
            _barItems.Clear();
            RefreshSlotUI();
        }

        public List<ItemController> GetCurrentItems() => new List<ItemController>(_barItems);
    }
}
```

---

### 4.5 Quest/

#### `QuestManager.cs`

```csharp
namespace MatchFactory.Quest
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using MatchFactory.Level;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Quản lý danh sách quests, cập nhật tiến độ và kiểm tra điều kiện win.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestSlotUI[] questSlotUIs;

        private List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> Quests => _quests;

        private void OnEnable() => GameEvents.OnMatchCompleted += HandleMatchCompleted;
        private void OnDisable() => GameEvents.OnMatchCompleted -= HandleMatchCompleted;

        public void InitializeQuests(QuestData[] questData)
        {
            _quests.Clear();
            foreach (var q in questData)
            {
                q.Reset();
                _quests.Add(q);
            }
            RefreshUI();
        }

        private void HandleMatchCompleted(ItemType type)
        {
            foreach (var quest in _quests)
            {
                if (quest.itemType != type || quest.isCompleted) continue;
                
                quest.AddProgress(Collection.CollectionBar.MATCH_COUNT);
                GameEvents.OnQuestProgressUpdated?.Invoke(quest);

                if (quest.isCompleted)
                    GameEvents.OnQuestCompleted?.Invoke(quest);
                break;
            }

            CheckWinCondition();
            RefreshUI();
        }

        private void CheckWinCondition()
        {
            foreach (var quest in _quests)
                if (!quest.isCompleted) return;

            float timeRemaining = Timer.GameTimer.Instance.RemainingTime;
            GameManager.Instance.TriggerWin(timeRemaining);
        }

        private void RefreshUI()
        {
            for (int i = 0; i < questSlotUIs.Length && i < _quests.Count; i++)
                questSlotUIs[i].UpdateDisplay(_quests[i]);
        }

        /// <summary>Lấy quest chưa hoàn thành có priority cao nhất (cho Vacuum)</summary>
        public QuestData GetHighestPriorityIncompleteQuest()
        {
            QuestData best = null;
            int bestIdx = int.MaxValue;
            foreach (var q in _quests)
            {
                if (q.isCompleted) continue;
                int idx = _quests.IndexOf(q);
                if (best == null
                    || q.RemainingCount > best.RemainingCount
                    || (q.RemainingCount == best.RemainingCount && idx < bestIdx))
                {
                    best = q;
                    bestIdx = idx;
                }
            }
            return best;
        }
    }
}
```

---

### 4.6 Timer/

#### `GameTimer.cs`

```csharp
namespace MatchFactory.Timer
{
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Đếm ngược thời gian gameplay. Hỗ trợ Pause/Resume và Freeze (IceGun power-up).
    /// </summary>
    public class GameTimer : Singleton<GameTimer>
    {
        public float RemainingTime { get; private set; }
        public float TotalTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFrozen { get; private set; }

        private float _freezeDuration;

        public void Initialize(float totalSeconds)
        {
            TotalTime = totalSeconds;
            RemainingTime = totalSeconds;
            IsRunning = false;
            IsFrozen = false;
        }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public void Freeze(float durationSeconds)
        {
            IsFrozen = true;
            _freezeDuration = durationSeconds;
        }

        private void Update()
        {
            if (!IsRunning) return;

            if (IsFrozen)
            {
                _freezeDuration -= Time.deltaTime;
                if (_freezeDuration <= 0f) IsFrozen = false;
                return;
            }

            RemainingTime -= Time.deltaTime;
            RemainingTime = Mathf.Max(0f, RemainingTime);
            GameEvents.OnTimerTick?.Invoke(RemainingTime);

            if (RemainingTime <= 0f)
            {
                IsRunning = false;
                GameManager.Instance.TriggerLose();
            }
        }
    }
}
```

---

### 4.7 PowerUps/

#### `PowerUpManager.cs`

```csharp
namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using MatchFactory.Quest;
    using UnityEngine;

    public class PowerUpManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private CollectionBar collectionBar;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private Timer.GameTimer gameTimer;

        [Header("Ice Gun Settings")]
        [SerializeField] private float iceGunFreezeDuration = 10f;

        private ICommand _lastCommand;

        public void UseShuffleCommand()   => ExecuteCommand(new ShuffleCommand(boardManager));
        public void UseVacuumCommand()    => ExecuteCommand(new VacuumCommand(boardManager, collectionBar, questManager));
        public void UseSpringCommand()    => ExecuteCommand(new SpringCommand(collectionBar, boardManager));
        public void UseIceGunCommand()    => ExecuteCommand(new IceGunCommand(gameTimer, iceGunFreezeDuration));

        private void ExecuteCommand(ICommand cmd)
        {
            if (!cmd.CanExecute()) return;
            cmd.Execute();
            _lastCommand = cmd;
        }
    }
}
```

#### `VacuumCommand.cs`

```csharp
namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using MatchFactory.Quest;

    public class VacuumCommand : ICommand
    {
        private readonly BoardManager _board;
        private readonly CollectionBar _bar;
        private readonly QuestManager _questManager;

        public VacuumCommand(BoardManager board, CollectionBar bar, QuestManager questManager)
        {
            _board = board;
            _bar = bar;
            _questManager = questManager;
        }

        public bool CanExecute()
            => _questManager.GetHighestPriorityIncompleteQuest() != null
            && _board.ActiveItemCount >= 3;

        public void Execute()
        {
            var quest = _questManager.GetHighestPriorityIncompleteQuest();
            if (quest == null) return;

            var targets = _board.GetAccessibleItems(quest.itemType, 3);
            if (targets.Count == 0) return;

            foreach (var item in targets)
                item.TriggerTapFromPowerUp();

            GameEvents.OnVacuumUsed?.Invoke();
        }

        public void Undo() { /* Vacuum không có undo */ }
    }
}
```

#### `SpringCommand.cs`

```csharp
namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using UnityEngine;

    public class SpringCommand : ICommand
    {
        private readonly CollectionBar _bar;
        private readonly BoardManager _board;
        private ItemController _returnedItem;

        public SpringCommand(CollectionBar bar, BoardManager board) { _bar = bar; _board = board; }

        public bool CanExecute() => _bar.GetCurrentItems().Count > 0;

        public void Execute()
        {
            _returnedItem = _bar.PopLastItem();
            if (_returnedItem == null) return;

            Vector3 returnPos = new Vector3(
                Random.Range(-1.5f, 1.5f),
                4f,
                Random.Range(-1.5f, 1.5f));
            _returnedItem.transform.position = returnPos;
            _returnedItem.SetFlying(false);
            _returnedItem.Initialize(_returnedItem.ItemType);
            GameEvents.OnSpringUsed?.Invoke();
        }

        public void Undo()
        {
            if (_returnedItem != null)
                _returnedItem.TriggerTapFromPowerUp();
        }
    }
}
```

#### `IceGunCommand.cs`

```csharp
namespace MatchFactory.PowerUps
{
    using MatchFactory.Timer;

    public class IceGunCommand : ICommand
    {
        private readonly GameTimer _timer;
        private readonly float _duration;

        public IceGunCommand(GameTimer timer, float duration) { _timer = timer; _duration = duration; }

        public bool CanExecute() => _timer.IsRunning && !_timer.IsFrozen;

        public void Execute()
        {
            _timer.Freeze(_duration);
            GameEvents.OnIceGunUsed?.Invoke();
        }

        public void Undo() { /* IceGun không có undo */ }
    }
}
```

---

### 4.8 Animation/

#### `ItemFlyAnimation.cs`

```csharp
namespace MatchFactory.Animation
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Bezier arc animation cho item bay từ board đến collection slot.
    /// </summary>
    public class ItemFlyAnimation : MonoBehaviour
    {
        [SerializeField] private float flyDuration = 0.4f;
        [SerializeField] private float arcHeight = 2f;
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public IEnumerator FlyTo(Transform item, Vector3 destination)
        {
            Vector3 start = item.position;
            Vector3 mid = (start + destination) * 0.5f + Vector3.up * arcHeight;

            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float easedT = speedCurve.Evaluate(t);

                // Quadratic Bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
                float u = 1f - easedT;
                item.position = u * u * start + 2f * u * easedT * mid + easedT * easedT * destination;

                // Shrink slightly as it arrives
                item.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, easedT);

                yield return null;
            }

            item.position = destination;
        }
    }
}
```

---

## 5. Hệ Thống Load Level

### 5.1 Flow Chi Tiết

```
[LevelSelect UI]
    │ User taps Level button
    ▼
PlayerPrefs.SetInt("PendingLevel", levelIndex)
SceneManager.LoadScene("Loading")
    │
    ▼
[Loading Scene]
    │ AsyncOperation op = SceneManager.LoadSceneAsync("Gameplay")
    │ op.allowSceneActivation = false
    │ while op.progress < 0.9f → update progress bar
    │ op.allowSceneActivation = true
    ▼
[Gameplay Scene Loaded]
    │ GameManager.Start() →
    │   int levelIndex = PlayerPrefs.GetInt("PendingLevel", 1)
    │   StartGame(levelIndex)
    ▼
GameManager.StartGame(levelIndex) → ChangeState(Loading)
    │ LevelManager.LoadLevel(levelIndex, callback)
    ▼
LevelLoader.LoadLevelCoroutine(levelIndex)
    │ Addressables.LoadAssetAsync<LevelData>("Level_001")
    │ yield return handle
    │ LevelManager.SetCurrentLevel(data)
    │ QuestManager.InitializeQuests(data.quests)
    │ GameTimer.Initialize(data.timeLimitSeconds)
    │ BoardManager.SpawnItems(data)
    │ callback() → GameManager.OnLevelLoaded()
    ▼
GameManager.ChangeState(Playing) → Timer.Resume()
    ▼
[Gameplay starts!]
```

### 5.2 Cấu Trúc LevelData Fields

```csharp
[CreateAssetMenu(fileName = "Level_001", menuName = "MatchFactory/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelIndex;                    // 1, 2, 3, ...
    public string levelName;                  // "Level 1", "Pear Garden"
    public QuestData[] quests;               // 1-3 quests
    public float timeLimitSeconds = 120f;
    public ItemSpawnConfig[] spawnConfigs;   // [{Pear, 9}, {Grape, 6}, {Cupcake, 3}]
    public int totalItemCount;              // Auto: 18
    public float threeStarTimeThreshold = 60f;
    public float twoStarTimeThreshold = 20f;
    public bool useCustomLayout = false;
    public Vector3[] customSpawnPositions;  // Optional
}
```

### 5.3 Addressables Naming

- **Naming:** `Level_001`, `Level_002`, ..., `Level_999`
- **Group:** `Levels` (có thể remote bundle để hot-update)
- **Fallback:** `Resources.Load<LevelData>($"Levels/Level_{idx:000}")`

### 5.4 ItemSpawnStrategy

```csharp
// Đảm bảo items luôn chia hết cho 3 tại LevelData.OnValidate()
// Spawn: Grid 5 cột, thả từ cao, physics settle tự nhiên
// Fisher-Yates shuffle toàn bộ list trước khi spawn
// Batch 5 items/frame để tránh frame spike
```

---

## 6. Hệ Thống Lưu Trạng Thái Game

### 6.1 Cấu Trúc JSON SaveData

```json
{
  "highestUnlockedLevel": 5,
  "levelProgress": [
    { "levelIndex": 1, "stars": 3, "bestTime": 95.2 },
    { "levelIndex": 2, "stars": 2, "bestTime": 67.4 }
  ],
  "totalStars": 9,
  "lastPlayedLevel": 5
}
```

### 6.2 Mid-Session Save

```json
{
  "levelIndex": 5,
  "timeRemaining": 74.3,
  "questProgress": [
    { "itemType": 1, "currentCount": 3, "requiredCount": 6 }
  ],
  "collectionBarState": [2, 2, 1, 3, 2],
  "timestamp": 1718000000
}
```

### 6.3 SaveManager Implementation

```csharp
namespace MatchFactory.Save
{
    using MatchFactory.Core;
    using System;
    using System.IO;
    using UnityEngine;

    [Serializable]
    public class LevelProgressEntry
    {
        public int levelIndex;
        public int stars;
        public float bestTime;
    }

    [Serializable]
    public class SaveData
    {
        public int highestUnlockedLevel = 1;
        public LevelProgressEntry[] levelProgress = new LevelProgressEntry[0];
        public int totalStars = 0;
        public int lastPlayedLevel = 1;
    }

    [Serializable]
    public class QuestProgressEntry
    {
        public int itemType;
        public int currentCount;
        public int requiredCount;
    }

    [Serializable]
    public class GameProgressData
    {
        public int levelIndex;
        public float timeRemaining;
        public QuestProgressEntry[] questProgress;
        public int[] collectionBarState;
        public long timestamp;
    }

    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_FILE = "save_data.json";
        private const string PROGRESS_FILE = "session_progress.json";

        private SaveData _saveData;
        private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);
        private string ProgressPath => Path.Combine(Application.persistentDataPath, PROGRESS_FILE);

        protected override void Awake()
        {
            base.Awake();
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_saveData, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveManager] Saved to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _saveData = JsonUtility.FromJson<SaveData>(json);
                }
                else
                {
                    _saveData = new SaveData();
                    Debug.Log("[SaveManager] No save found, using default.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                _saveData = new SaveData();
            }
        }

        public void DeleteSave()
        {
            _saveData = new SaveData();
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(ProgressPath)) File.Delete(ProgressPath);
            Debug.Log("[SaveManager] Save deleted.");
        }

        public void RecordLevelComplete(int levelIndex, int stars)
        {
            _saveData.lastPlayedLevel = levelIndex;

            bool found = false;
            for (int i = 0; i < _saveData.levelProgress.Length; i++)
            {
                if (_saveData.levelProgress[i].levelIndex == levelIndex)
                {
                    if (stars > _saveData.levelProgress[i].stars)
                        _saveData.levelProgress[i].stars = stars;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var list = new System.Collections.Generic.List<LevelProgressEntry>(_saveData.levelProgress);
                list.Add(new LevelProgressEntry { levelIndex = levelIndex, stars = stars });
                _saveData.levelProgress = list.ToArray();
            }

            if (stars > 0 && levelIndex + 1 > _saveData.highestUnlockedLevel)
                _saveData.highestUnlockedLevel = levelIndex + 1;

            RecalculateTotalStars();
            Save();
        }

        public int GetStarsForLevel(int levelIndex)
        {
            foreach (var entry in _saveData.levelProgress)
                if (entry.levelIndex == levelIndex) return entry.stars;
            return 0;
        }

        public bool IsLevelUnlocked(int levelIndex)
            => levelIndex <= _saveData.highestUnlockedLevel;

        private void RecalculateTotalStars()
        {
            int total = 0;
            foreach (var e in _saveData.levelProgress) total += e.stars;
            _saveData.totalStars = total;
        }

        public void SaveSessionProgress(GameProgressData data)
        {
            data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                File.WriteAllText(ProgressPath, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Session save failed: {e.Message}");
            }
        }

        public GameProgressData LoadSessionProgress()
        {
            if (!File.Exists(ProgressPath)) return null;
            try { return JsonUtility.FromJson<GameProgressData>(File.ReadAllText(ProgressPath)); }
            catch { return null; }
        }

        public void ClearSessionProgress()
        {
            if (File.Exists(ProgressPath)) File.Delete(ProgressPath);
        }
    }
}
```

### 6.4 OnApplicationPause

```csharp
// GameManager.cs
private void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus && _currentState == GameState.Playing)
    {
        var data = CollectCurrentSessionData();
        SaveManager.Instance.SaveSessionProgress(data);
    }
    else if (!pauseStatus && _currentState == GameState.Playing)
    {
        PauseGame();  // Hiển thị pause panel khi foreground lại
    }
}
```

---

## 7. Game Events System

```csharp
namespace MatchFactory.Core
{
    using MatchFactory.Board;
    using MatchFactory.Level;
    using System;

    /// <summary>
    /// Static event bus. Tất cả cross-module communication đi qua đây.
    /// LUÔN null-check khi invoke: GameEvents.OnXxx?.Invoke(...)
    /// LUÔN unsubscribe trong OnDisable() để tránh memory leak.
    /// </summary>
    public static class GameEvents
    {
        // ========================
        // ITEM EVENTS
        // ========================
        
        /// <summary>Fired khi player tap vào 1 item 3D trên board</summary>
        public static Action<ItemController> OnItemTapped;
        
        /// <summary>Fired sau khi item đã bay vào và settle trong collection bar</summary>
        public static Action<ItemController> OnItemAddedToBar;
        
        /// <summary>Fired khi 3 items cùng loại được xoá khỏi bar</summary>
        public static Action<ItemType> OnMatchCompleted;

        // ========================
        // QUEST EVENTS
        // ========================
        
        /// <summary>Fired mỗi khi quest progress thay đổi (để update UI)</summary>
        public static Action<QuestData> OnQuestProgressUpdated;
        
        /// <summary>Fired khi 1 quest hoàn thành (trigger animation + SFX)</summary>
        public static Action<QuestData> OnQuestCompleted;

        // ========================
        // GAME STATE EVENTS
        // ========================
        
        /// <summary>Fired khi tất cả quests hoàn thành</summary>
        public static Action OnGameWin;
        
        /// <summary>Fired khi timer hết hoặc bar đầy</summary>
        public static Action OnGameLose;
        
        /// <summary>Fired mỗi frame khi timer chạy. float = thời gian còn lại</summary>
        public static Action<float> OnTimerTick;
        
        /// <summary>Fired khi game start sau khi load xong</summary>
        public static Action OnGameStarted;
        
        /// <summary>Fired khi game pause/resume. bool = isPaused</summary>
        public static Action<bool> OnGamePauseChanged;

        // ========================
        // POWER-UP EVENTS
        // ========================
        
        public static Action OnShuffleUsed;
        public static Action OnVacuumUsed;
        public static Action OnSpringUsed;
        public static Action OnIceGunUsed;

        // ========================
        // COLLECTION BAR EVENTS
        // ========================
        
        /// <summary>Fired khi collection bar đầy 7 ô</summary>
        public static Action OnCollectionBarFull;
        
        /// <summary>int = số slot còn trống sau khi update</summary>
        public static Action<int> OnCollectionBarUpdated;

        // ========================
        // UTILITY
        // ========================
        
        /// <summary>Reset tất cả events về null - gọi khi restart level</summary>
        public static void ClearAllListeners()
        {
            OnItemTapped = null;
            OnItemAddedToBar = null;
            OnMatchCompleted = null;
            OnQuestProgressUpdated = null;
            OnQuestCompleted = null;
            OnGameWin = null;
            OnGameLose = null;
            OnTimerTick = null;
            OnGameStarted = null;
            OnGamePauseChanged = null;
            OnShuffleUsed = null;
            OnVacuumUsed = null;
            OnSpringUsed = null;
            OnIceGunUsed = null;
            OnCollectionBarFull = null;
            OnCollectionBarUpdated = null;
        }
    }
}
```

> **⚠️ Quan trọng:** Gọi `GameEvents.ClearAllListeners()` trước khi reload scene để tránh stale delegates trỏ vào objects đã bị destroyed.

---

## 8. Smart Sorting Logic (CollectionBar)

### 8.1 Thuật Toán

```
Khi item X (type = T) tapped:
┌─────────────────────────────────────────────────────────────────┐
│ 1. Animate item X bay từ board → target slot (Bezier arc)       │
│                                                                  │
│ 2. FindSmartInsertIndex(T):                                      │
│    - Duyệt _barItems từ 0 đến Count-1                           │
│    - Ghi nhớ lastSameTypeIndex (index cuối cùng có type T)       │
│    - Nếu tìm thấy: return lastSameTypeIndex + 1                  │
│    - Nếu không: return _barItems.Count (append cuối)             │
│                                                                  │
│ 3. _barItems.Insert(insertIndex, itemX)                          │
│    → Items từ insertIndex trở đi shift sang phải 1 ô            │
│                                                                  │
│ 4. CheckAndProcessMatch(T):                                      │
│    - Đếm consecutive items cùng type T                          │
│    - Nếu đủ 3 liên tiếp:                                        │
│      a. Play MatchBurst_VFX tại slot giữa                       │
│      b. Xoá 3 items (từ cuối về đầu để giữ index đúng)         │
│      c. Recycle về pool                                          │
│      d. Fire OnMatchCompleted(T)                                 │
│                                                                  │
│ 5. if _barItems.Count >= 7: TriggerLose()                       │
│                                                                  │
│ 6. RefreshSlotUI()                                              │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Ví Dụ Trực Quan

```
Bar:      [🍐][🍐][🍇][🧁][🍇][ ][ ]
Tap 🍐:

FindSmartInsertIndex(Pear):
  i=0: Pear → lastSame=0
  i=1: Pear → lastSame=1
  i=2: Grape → skip
  i=3: Cupcake → skip
  i=4: Grape → skip
  Return: 1+1 = 2

Insert tại 2:
  [🍐][🍐][🍐][🍇][🧁][🍇][ ]

CheckMatch: consecutive Pear tại 0,1,2 → MATCH!
Remove [0..2] → Bar: [🍇][🧁][🍇][ ][ ][ ][ ]
Fire OnMatchCompleted(Pear)
```

---

## 9. Vacuum Power-up Selection Algorithm

### 9.1 Thuật Toán

```
VacuumCommand.Execute():

1. GetHighestPriorityIncompleteQuest():
   - Lọc: quests chưa hoàn thành
   - Sort: RemainingCount DESC, nếu bằng → questIndex ASC
   - Return quest[0]

2. targetType = quest.itemType

3. boardManager.GetAccessibleItems(targetType, maxCount=3):
   - Lọc items: type == targetType && !IsTapped && !IsFlying
   - Sort: Y position DESC (items trên cao ít bị block)
   - Raycast từ Camera → mỗi item:
     * Hit collider FIRST = item itself → accessible = true
     * Hit another collider first → accessible = false
   - Collect up to 3 accessible
   - Fallback: nếu < 3 accessible, lấy bất kỳ candidates còn lại

4. foreach item in targets: item.TriggerTapFromPowerUp()
   → OnItemTapped event → CollectionBar xử lý bình thường

5. Fire OnVacuumUsed (play VFX, SFX)
```

### 9.2 Edge Cases

| Tình huống | Xử lý |
|-----------|-------|
| Không có items của targetType trên board | Tìm type khác của quest thứ 2 |
| Tất cả items bị block | Fallback: lấy items không cần raycast check |
| Board trống | CanExecute() = false, button disabled |
| Bar sắp đầy (còn < 3 slot) | Vẫn execute, smart sort + match xử lý tự động |

---

## 10. Physics & Board Management

### 10.1 Spawn Strategy (3D Heap)

```csharp
// Grid 5 cột × n/5 hàng
// Cell size: spawnBounds.size.x / 5
// Mỗi row spawn thêm 0.5f độ cao
// Random offset ±0.1f để tránh stack thẳng đứng hoàn hảo
// Batch 5 items/frame: yield return null sau mỗi batch
// Physics settle trong ~0.5-1s sau khi spawn xong
```

### 10.2 Input / Tap Detection

```csharp
// InputManager.cs (thêm vào GameManager hoặc riêng)
void Update()
{
    if (GameManager.Instance.CurrentState != GameState.Playing) return;

#if UNITY_EDITOR
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        ProcessRaycast(ray);
    }
#else
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
        ProcessRaycast(ray);
    }
#endif
}

void ProcessRaycast(Ray ray)
{
    // Chỉ hit layer Items, không xuyên qua UI
    if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Items")))
    {
        hit.collider.GetComponent<ItemController>()?.HandleTap();
    }
}
```

> **Lý do không dùng `OnMouseDown` trên Item:** Unity gửi `OnMouseDown` đến tất cả colliders trong path của ray, không chỉ cái đầu tiên. `Physics.Raycast` từ InputManager trả về ĐÚNG collider đầu tiên bị hit.

### 10.3 Board Bounds Setup

```
BoardBound_Prefab (Layer: Board)
├── Floor     BoxCollider [center=(0,-0.1,0), size=(8,0.2,8)]
│             PhysicMaterial: HighFriction (staticFriction=0.8, dynamicFriction=0.6, bounciness=0.1)
├── WallLeft  BoxCollider [center=(-4,2,0),   size=(0.2,4,8)]
│             PhysicMaterial: Bouncy (bounciness=0.3)
├── WallRight BoxCollider [center=(4,2,0),    size=(0.2,4,8)]
├── WallFront BoxCollider [center=(0,2,4),    size=(8,4,0.2)]
└── WallBack  BoxCollider [center=(0,2,-4),   size=(8,4,0.2)]
```

### 10.4 Layer Setup

| Layer | Index | Dùng cho | Physics với |
|-------|-------|----------|------------|
| `Items` | 8 | Tất cả 3D item prefabs | Items✅, Board✅, UI❌ |
| `Board` | 9 | Board bounds colliders | Items✅, Board❌ |
| `VFX` | 10 | Particle systems | Tất cả❌ |

---

## 11. Scene Structure

### 11.1 Scenes List

| Scene | Mô tả |
|-------|-------|
| `MainMenu` | Logo, Play button, Settings |
| `LevelSelect` | Grid levels, stars display, lock state |
| `Loading` | Progress bar, async load Gameplay |
| `Gameplay` | Scene chơi game chính |

### 11.2 Gameplay Scene Hierarchy

```
Gameplay (Scene)
│
├── [Camera] GameCamera
│   ├── Camera                    [FOV=60, ClearFlags=Skybox]
│   ├── UniversalAdditionalCameraData
│   └── PostProcessVolume (URP)   [Bloom, Vignette, ColorGrading]
│
├── [Lighting]
│   ├── DirectionalLight          [Intensity=1.2, Shadows=Soft]
│   └── ReflectionProbe           [Baked]
│
├── [Board] BoardManager
│   ├── BoundColliders
│   │   ├── Floor
│   │   ├── WallLeft
│   │   ├── WallRight
│   │   ├── WallFront
│   │   └── WallBack
│   ├── ItemSpawnPoint
│   ├── ItemPool                  [Script]
│   └── ItemFactory               [Script]
│
├── [UI] Canvas (Screen Space - Overlay)
│   ├── HUD
│   │   ├── QuestPanel            [HorizontalLayoutGroup]
│   │   │   ├── QuestSlot_0
│   │   │   ├── QuestSlot_1
│   │   │   └── QuestSlot_2
│   │   ├── TimerDisplay
│   │   └── PowerUpBar
│   │       ├── ShuffleButton
│   │       ├── VacuumButton
│   │       ├── SpringButton
│   │       └── IceGunButton
│   ├── CollectionBar
│   │   ├── Slot_0 ... Slot_6     [CollectionSlot × 7]
│   ├── PausePanel                [Active=false]
│   └── ResultPanel               [Active=false]
│
├── [VFX] VFXContainer
│
└── [Managers]
    ├── GameManager               [Singleton]
    ├── LevelManager              [Singleton, DontDestroyOnLoad]
    ├── LevelLoader               [MonoBehaviour]
    ├── QuestManager              [MonoBehaviour]
    ├── GameTimer                 [Singleton]
    ├── PowerUpManager            [MonoBehaviour]
    ├── UIManager                 [Singleton]
    ├── AudioManager              [Singleton, DontDestroyOnLoad]
    └── SaveManager               [Singleton, DontDestroyOnLoad]
```

---

## 12. Naming Conventions & Coding Standards

### 12.1 Script Naming

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| MonoBehaviour Manager | `PascalCase + Manager` | `BoardManager.cs` |
| MonoBehaviour Controller | `PascalCase + Controller` | `ItemController.cs` |
| ScriptableObject | `PascalCase + Data` | `LevelData.cs`, `SoundData.cs` |
| Interface | `I + PascalCase` | `ICommand.cs` |
| Enum | `PascalCase` | `ItemType.cs`, `GameState.cs` |
| Static event class | `PascalCase + Events` | `GameEvents.cs` |
| Abstract base | `PascalCase` | `Singleton.cs` |

### 12.2 Prefab Naming

| Category | Format | Ví dụ |
|----------|--------|-------|
| 3D Items | `Item_<Type>` | `Item_Pear`, `Item_TennisBall` |
| UI | `<Role>_Prefab` | `QuestSlot_Prefab`, `ResultPanel_Prefab` |
| VFX | `<Effect>_VFX` | `MatchBurst_VFX`, `WinConfetti_VFX` |
| ScriptableObject Level | `Level_<000>` | `Level_001.asset` |

### 12.3 Variable / Method Naming

```csharp
// Private fields: _camelCase
private int _currentCount;
private BoardManager _boardManager;

// Public properties: PascalCase (get only preferred)
public int CurrentCount { get; private set; }
public bool IsCompleted => _currentCount >= _requiredCount;

// Methods: PascalCase
public void Initialize(QuestData data) { }
private void HandleMatchCompleted(ItemType type) { }

// Constants: UPPER_SNAKE_CASE
private const int MAX_SLOTS = 7;
private const string SAVE_FILE = "save_data.json";

// Events: On + PascalCase
public static Action<ItemController> OnItemTapped;

// Coroutines: PascalCase + Coroutine suffix
private IEnumerator LoadLevelCoroutine(int index) { }

// Animator hashes: UPPER_SNAKE_CASE + _TRIGGER/_BOOL
private static readonly int FLIP_TRIGGER = Animator.StringToHash("Flip");
```

### 12.4 Coding Standards (10 Rules)

1. **Namespace bắt buộc:** Mọi file phải có `namespace MatchFactory.<Module>`
2. **Null-safe events:** Luôn dùng `?.Invoke()` thay vì `.Invoke()`
3. **Unsubscribe events:** Luôn trong `OnDisable()`, không bao giờ bỏ sót
4. **SerializeField:** Dùng `[SerializeField] private` thay vì `public`
5. **Header groups:** Nhóm Inspector fields với `[Header("...")]`
6. **RequireComponent:** Thêm khi MonoBehaviour phụ thuộc component khác
7. **OnValidate:** Validate SO data trong Editor, warn nếu sai
8. **Log prefix:** `Debug.Log($"[{nameof(BoardManager)}] message")`
9. **No magic numbers:** Dùng `const` hoặc `[SerializeField]`
10. **Object Pool:** KHÔNG bao giờ `Instantiate`/`Destroy` items trong gameplay loop

### 12.5 Git Commit Convention

```
feat(board): add smart sort algorithm in CollectionBar
fix(timer): freeze duration not resetting after use
refactor(quest): extract QuestProgressEntry to SaveData.cs
docs: update TDD with PowerUp vacuum algorithm
art(prefab): add IceCream 3D item prefab + material
perf(pool): increase initial pool size to 50
```

---

## PHỤ LỤC A: Animation Clips

| Clip | Duration | Loop | Trigger |
|------|----------|------|---------|
| `QuestSlot_Flip` | 0.3s | No | Count thay đổi |
| `QuestSlot_Complete` | 0.5s | No | Quest done |
| `Item_Highlight` | 0.2s | Yes | Được tap |
| `Star_PopIn` | 0.25s | No | Result screen |
| `CollectionBar_Shake` | 0.3s | No | Bar đầy |

## PHỤ LỤC B: Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| FPS | 60 FPS stable | iPhone 11+ / SD865+ |
| Draw Calls | < 50 | GPU Instancing cho items |
| Memory | < 200 MB | Addressables + Object Pool |
| Load Time | < 3s | Gameplay scene |
| GC Alloc/frame | < 1 KB | Tránh string concat trong Update |

## PHỤ LỤC C: Pre-Build Checklist

- [ ] Tất cả `LevelData.OnValidate()` không có warning
- [ ] Tất cả events được unsubscribe trong `OnDisable()`
- [ ] `ItemPool` initialized trước `SpawnItems()`
- [ ] `GameEvents.ClearAllListeners()` gọi trước scene reload
- [ ] `SaveManager.Save()` gọi sau mỗi level complete
- [ ] Addressables remote catalog URL đúng
- [ ] Physics Matrix: Items↔Board=On, Items↔UI=Off
- [ ] Tất cả item counts trong LevelData bội số của 3
- [ ] Android `minSdkVersion = 24`, iOS `minimum deployment = 14.0`
- [ ] Audio Master Volume ≤ -3dB để tránh clipping

---

*Tài liệu cần cập nhật khi có thay đổi architecture. Lịch sử version theo Git commit log.*
