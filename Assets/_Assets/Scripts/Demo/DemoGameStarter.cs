namespace MatchFactory.Demo
{
    using MatchFactory.Core;
    using MatchFactory.Level;
    using MatchFactory.Board;
    using UnityEngine;

    /// <summary>
    /// Demo scene starter — tạo LevelData trực tiếp trong code thay vì dùng Addressables.
    /// Dùng cho demo/test không cần bộ full level loading system.
    /// </summary>
    public class DemoGameStarter : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Demo Level Config")]
        [SerializeField] private int boxCount    = 9;  // ItemType.Box (Cube - Red)
        [SerializeField] private int sphereCount = 9;  // ItemType.Sphere (Green)
        [SerializeField] private int capsuleCount = 9; // ItemType.Capsule (Blue)
        [SerializeField] private float timeLimitSeconds = 225f;

        private void Start()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;

            // Build LevelData programmatically
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.levelIndex = 1;
            levelData.levelName  = "Demo Level 1";
            levelData.timeLimitSeconds = timeLimitSeconds;
            levelData.threeStarTimeThreshold = timeLimitSeconds * 0.4f;
            levelData.twoStarTimeThreshold   = timeLimitSeconds * 0.2f;

            // Quests
            var quests = new QuestData[3];

            quests[0] = new QuestData { itemType = ItemType.Box,     requiredCount = boxCount };
            quests[1] = new QuestData { itemType = ItemType.Sphere,   requiredCount = sphereCount };
            quests[2] = new QuestData { itemType = ItemType.Capsule,  requiredCount = capsuleCount };

            levelData.quests = quests;

            // Spawn configs
            levelData.spawnConfigs = new LevelData.ItemSpawnConfig[]
            {
                new LevelData.ItemSpawnConfig { itemType = ItemType.Box,     count = boxCount    },
                new LevelData.ItemSpawnConfig { itemType = ItemType.Sphere,  count = sphereCount },
                new LevelData.ItemSpawnConfig { itemType = ItemType.Capsule, count = capsuleCount },
            };

            Debug.Log($"[DemoGameStarter] Starting demo: {boxCount + sphereCount + capsuleCount} items, {timeLimitSeconds}s");
            gameManager.StartGame(levelData);
        }
    }
}
