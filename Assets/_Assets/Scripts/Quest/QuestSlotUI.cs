namespace MatchFactory.Quest
{
    using MatchFactory.Board;
    using MatchFactory.Level;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// UI element đại diện cho 1 Quest trong HUD (góc trên trái).
    /// Hiển thị icon, count, và trạng thái hoàn thành.
    /// </summary>
    public class QuestSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconBackground;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI typeLabel;
        [SerializeField] private GameObject completedOverlay;

        private QuestData _questData;

        public void Setup(QuestData quest)
        {
            _questData = quest;
            gameObject.SetActive(true);

            // Set color based on item type
            if (iconBackground != null)
                iconBackground.color = Collection.CollectionSlot.GetColorForType(quest.itemType);

            if (typeLabel != null)
                typeLabel.text = quest.itemType.ToString().ToUpper();

            if (completedOverlay != null)
                completedOverlay.SetActive(false);

            UpdateDisplay(quest);
        }

        public void UpdateDisplay(QuestData quest)
        {
            _questData = quest;

            if (countText != null)
                countText.text = quest.RemainingCount.ToString();

            if (completedOverlay != null)
                completedOverlay.SetActive(quest.isCompleted);

            // Visual feedback: dim when completed
            if (iconBackground != null)
            {
                var col = Collection.CollectionSlot.GetColorForType(quest.itemType);
                iconBackground.color = quest.isCompleted ? col * 0.4f : col;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
