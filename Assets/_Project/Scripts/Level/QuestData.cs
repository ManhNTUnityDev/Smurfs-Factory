namespace MatchFactory.Level
{
    using MatchFactory.Board;
    using UnityEngine;

    /// <summary>
    /// Runtime data cho mỗi Quest. Serializable để dùng trong ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class QuestData
    {
        public ItemType itemType;

        [Tooltip("Số lượng items cần thu thập — PHẢI là bội số của 3")]
        public int requiredCount = 9;

        public Sprite itemIcon; // Optional icon reference

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
