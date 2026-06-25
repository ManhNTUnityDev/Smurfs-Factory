namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Shuffle Power-up: Hất tung toàn bộ Items trên board lên cao, sau đó rơi xuống vị trí mới.
    /// Command Pattern — có thể Undo (khôi phục vị trí cũ).
    /// </summary>
    public class ShuffleCommand : ICommand
    {
        private readonly BoardManager _board;
        private Vector3[] _savedPositions;
        private ItemController[] _items;

        public ShuffleCommand(BoardManager board) => _board = board;

        public bool CanExecute() => _board != null && _board.ActiveItemCount > 0;

        public void Execute()
        {
            _items = _board.GetAllActiveItems();
            _savedPositions = new Vector3[_items.Length];
            for (int i = 0; i < _items.Length; i++)
                _savedPositions[i] = _items[i].transform.position;

            _board.ShuffleItems();
            Debug.Log($"[ShuffleCommand] Executed. {_items.Length} items shuffled.");
        }

        public void Undo()
        {
            if (_items == null) return;
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] != null && !_items[i].IsTapped)
                    _items[i].transform.position = _savedPositions[i];
            }
        }
    }
}
