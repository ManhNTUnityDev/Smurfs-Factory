namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using MatchFactory.Core;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Spring Power-up: Đẩy tất cả Items trong collection bar trở lại board.
    /// Items được đặt trên layer trên cùng (Y cao) của board.
    /// </summary>
    public class SpringCommand : ICommand
    {
        private readonly CollectionBar _bar;
        private readonly BoardManager _board;
        private List<ItemController> _returnedItems = new List<ItemController>();

        public SpringCommand(CollectionBar bar, BoardManager board)
        {
            _bar = bar;
            _board = board;
        }

        public bool CanExecute() => _bar != null && _bar.CurrentItemCount > 0;

        public void Execute()
        {
            _returnedItems = _bar.PopAllItems();
            if (_returnedItems.Count == 0) return;

            foreach (var item in _returnedItems)
            {
                if (item == null) continue;

                // Trả về vị trí ngẫu nhiên trên đỉnh của board
                Vector3 returnPos = new Vector3(
                    Random.Range(-1.5f, 1.5f),
                    5f + Random.Range(0f, 2f),  // Cao hơn để rơi xuống bề mặt
                    Random.Range(-1.5f, 1.5f)
                );

                item.transform.position = returnPos;
                item.Initialize(item.ItemType);

                // Apply slight impulse để items không stack thẳng
                var rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.AddForce(new Vector3(
                        Random.Range(-2f, 2f),
                        Random.Range(1f, 3f),
                        Random.Range(-2f, 2f)), ForceMode.Impulse);
                }

                _board.AddItemToBoard(item, returnPos);
            }

            GameEvents.OnSpringUsed?.Invoke();
            Debug.Log($"[SpringCommand] Returned {_returnedItems.Count} items to board.");
        }

        public void Undo()
        {
            // Undo: tap lại tất cả items trả về
            foreach (var item in _returnedItems)
                item?.TriggerTapFromPowerUp();
        }
    }
}
