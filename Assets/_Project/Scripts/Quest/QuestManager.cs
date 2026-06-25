namespace MatchFactory.Quest
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using MatchFactory.Level;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Quản lý danh sách quests, cập nhật tiến độ và kiểm tra điều kiện win.
    /// Observer: lắng nghe OnMatchCompleted event từ CollectionBar.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestSlotUI[] questSlotUIs;

        private List<QuestData> _quests = new List<QuestData>();
        public IReadOnlyList<QuestData> Quests => _quests;

        private void OnEnable()  => GameEvents.OnMatchCompleted += HandleMatchCompleted;
        private void OnDisable() => GameEvents.OnMatchCompleted -= HandleMatchCompleted;

        public void InitializeQuests(QuestData[] questData)
        {
            _quests.Clear();
            foreach (var q in questData)
            {
                q.Reset();
                _quests.Add(q);
            }

            // Show quest slots
            for (int i = 0; i < questSlotUIs.Length; i++)
            {
                if (i < _quests.Count)
                    questSlotUIs[i].Setup(_quests[i]);
                else
                    questSlotUIs[i].Hide();
            }

            Debug.Log($"[{nameof(QuestManager)}] Initialized {_quests.Count} quests.");
        }

        private void HandleMatchCompleted(ItemType type)
        {
            foreach (var quest in _quests)
            {
                if (quest.itemType != type || quest.isCompleted) continue;

                // Each match-3 = 3 items collected
                quest.AddProgress(Collection.CollectionBar.MATCH_COUNT);
                GameEvents.OnQuestProgressUpdated?.Invoke(quest);
                Debug.Log($"[{nameof(QuestManager)}] Quest {type}: {quest.currentCount}/{quest.requiredCount}");

                if (quest.isCompleted)
                {
                    GameEvents.OnQuestCompleted?.Invoke(quest);
                    Debug.Log($"[{nameof(QuestManager)}] Quest {type} COMPLETED!");
                }
                break;
            }

            RefreshUI();
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            foreach (var quest in _quests)
                if (!quest.isCompleted) return;

            float timeRemaining = Timer.GameTimer.Instance?.RemainingTime ?? 0f;
            Debug.Log($"[{nameof(QuestManager)}] All quests complete! Win! Time remaining: {timeRemaining:F1}s");
            GameManager.Instance?.TriggerWin(timeRemaining);
        }

        private void RefreshUI()
        {
            for (int i = 0; i < questSlotUIs.Length && i < _quests.Count; i++)
                questSlotUIs[i].UpdateDisplay(_quests[i]);
        }

        /// <summary>
        /// Lấy quest chưa hoàn thành có priority cao nhất (cho Vacuum).
        /// Priority = RemainingCount cao nhất; tie-break = index nhỏ hơn.
        /// </summary>
        public QuestData GetHighestPriorityIncompleteQuest()
        {
            QuestData best = null;
            int bestIdx = int.MaxValue;

            for (int i = 0; i < _quests.Count; i++)
            {
                var q = _quests[i];
                if (q.isCompleted) continue;

                if (best == null
                    || q.RemainingCount > best.RemainingCount
                    || (q.RemainingCount == best.RemainingCount && i < bestIdx))
                {
                    best = q;
                    bestIdx = i;
                }
            }
            return best;
        }
    }
}
