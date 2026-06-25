namespace MatchFactory.Level
{
    using MatchFactory.Board;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject chứa toàn bộ config cho 1 level.
    /// Designer tạo file asset này để define level mà không cần sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_001", menuName = "MatchFactory/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int levelIndex = 1;
        public string levelName = "Level 1";

        [Header("Quests")]
        public QuestData[] quests;

        [Header("Timer")]
        [Tooltip("Tổng thời gian level tính bằng giây")]
        public float timeLimitSeconds = 225f;

        [Header("Item Spawn")]
        public ItemSpawnConfig[] spawnConfigs;

        [HideInInspector]
        public int totalItemCount;

        [Header("Star Thresholds (seconds remaining)")]
        [Tooltip("Thời gian còn lại >= này → 3 sao (> 40% = 90s)")]
        public float threeStarTimeThreshold = 90f;
        [Tooltip("Thời gian còn lại >= này → 2 sao (20-40% = 45s)")]
        public float twoStarTimeThreshold = 45f;

        [System.Serializable]
        public struct ItemSpawnConfig
        {
            public ItemType itemType;
            [Tooltip("Phải là bội số của 3")]
            public int count;
        }

        private void OnValidate()
        {
            totalItemCount = 0;
            foreach (var cfg in spawnConfigs)
            {
                if (cfg.count % 3 != 0)
                    Debug.LogWarning($"[LevelData] {name}: {cfg.itemType} count ({cfg.count}) không phải bội số của 3!");
                totalItemCount += cfg.count;
            }

            // Validate quests match spawn configs
            if (quests != null)
            {
                foreach (var quest in quests)
                {
                    if (quest.requiredCount % 3 != 0)
                        Debug.LogWarning($"[LevelData] {name}: Quest {quest.itemType} requiredCount ({quest.requiredCount}) không phải bội số của 3!");
                }
            }
        }
    }
}
