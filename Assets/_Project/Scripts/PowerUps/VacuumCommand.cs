namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using MatchFactory.Quest;
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Vacuum Power-up: Tự động hút 3 items cùng loại từ board về collection bar.
    /// Ưu tiên loại item có Quest RemainingCount cao nhất.
    /// Tie-break: Quest index nhỏ hơn (bên trái hơn).
    /// </summary>
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
        {
            var quest = _questManager?.GetHighestPriorityIncompleteQuest();
            return quest != null && _board != null && _board.ActiveItemCount >= 3
                   && (_bar.CurrentItemCount + 3) <= CollectionBar.MAX_SLOTS;
        }

        public void Execute()
        {
            var quest = _questManager.GetHighestPriorityIncompleteQuest();
            if (quest == null)
            {
                Debug.Log("[VacuumCommand] No incomplete quest found.");
                return;
            }

            var targets = _board.GetAccessibleItems(quest.itemType, 3);
            if (targets.Count == 0)
            {
                Debug.Log($"[VacuumCommand] No accessible items of type {quest.itemType}.");
                return;
            }

            Debug.Log($"[VacuumCommand] Vacuuming {targets.Count} items of type {quest.itemType}");
            foreach (var item in targets)
                item.TriggerTapFromPowerUp();

            GameEvents.OnVacuumUsed?.Invoke();
        }

        public void Undo() { /* Vacuum không có undo */ }
    }
}
