namespace MatchFactory.Collection
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using MatchFactory.Animation;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Quản lý thanh 7 ô chứa items. Thực thi smart sort và match detection.
    /// Smart Sort: xếp items cùng loại liền kề nhau.
    /// Match-3: 3 items cùng loại liên tiếp → tự động xóa.
    /// </summary>
    public class CollectionBar : MonoBehaviour
    {
        public const int MAX_SLOTS = 7;
        public const int MATCH_COUNT = 3;

        [SerializeField] private CollectionSlot[] slots = new CollectionSlot[MAX_SLOTS];
        [SerializeField] private ItemFlyAnimation flyAnimPrefab;

        // Runtime state — danh sách items đang có trong bar (theo thứ tự)
        private List<ItemController> _barItems = new List<ItemController>();
        private bool _isProcessing = false;

        private void OnEnable()  => GameEvents.OnItemTapped += OnItemTapped;
        private void OnDisable() => GameEvents.OnItemTapped -= OnItemTapped;

        private void OnItemTapped(ItemController item)
        {
            StartCoroutine(AddItemToBar(item));
        }

        private IEnumerator AddItemToBar(ItemController item)
        {
            // Tính target slot index theo smart sort
            int targetSlotIndex = FindSmartInsertIndex(item.ItemType);

            // Clamp target slot to bar size
            int flyTargetVisualIndex = Mathf.Clamp(targetSlotIndex, 0, MAX_SLOTS - 1);

            // Animate item bay đến slot
            item.SetFlying(true);

            Vector3 targetPos = slots[flyTargetVisualIndex].transform.position;
            if (flyAnimPrefab != null)
            {
                var fly = Instantiate(flyAnimPrefab);
                bool done = false;
                yield return fly.FlyTo(item.transform, targetPos, () => done = true);
                yield return new WaitUntil(() => done || fly == null);
                if (fly != null) Destroy(fly.gameObject);
            }
            else
            {
                // Fallback: direct move
                float elapsed = 0f;
                Vector3 start = item.transform.position;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    item.transform.position = Vector3.Lerp(start, targetPos, elapsed / 0.3f);
                    yield return null;
                }
                item.transform.position = targetPos;
            }

            // Insert item vào danh sách theo smart sort
            InsertItem(item, targetSlotIndex);
            GameEvents.OnItemAddedToBar?.Invoke(item);

            // Check match TRƯỚC khi check bar full
            bool matched = false;
            yield return CheckAndProcessMatch(item.ItemType, () => matched = true);

            // Kiểm tra bar full (chỉ khi không vừa match)
            if (!matched && _barItems.Count >= MAX_SLOTS)
            {
                GameEvents.OnCollectionBarFull?.Invoke();
                GameManager.Instance?.TriggerLose();
            }

            RefreshSlotUI();
        }

        /// <summary>
        /// Smart sort: tìm vị trí insert tối ưu.
        /// - Nếu đã có item cùng loại: chèn sau item cùng loại cuối cùng
        /// - Nếu chưa có: append vào cuối
        /// </summary>
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

        private IEnumerator CheckAndProcessMatch(ItemType type, System.Action onMatch = null)
        {
            // Tìm 3 items cùng loại liên tiếp
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
                        yield return ProcessMatch(type, firstIndex, onMatch);
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

        private IEnumerator ProcessMatch(ItemType type, int startIndex, System.Action onMatch = null)
        {
            yield return new WaitForSeconds(0.15f);

            // Brief flash animation on matched items
            for (int i = startIndex; i < startIndex + MATCH_COUNT && i < _barItems.Count; i++)
            {
                var item = _barItems[i];
                StartCoroutine(FlashAndShrink(item.transform));
            }

            yield return new WaitForSeconds(0.2f);

            // Xoá 3 items từ cuối về đầu để giữ index đúng
            for (int i = startIndex + MATCH_COUNT - 1; i >= startIndex; i--)
            {
                if (i < _barItems.Count)
                {
                    var item = _barItems[i];
                    _barItems.RemoveAt(i);
                    item.ReturnToPool();
                }
            }

            GameEvents.OnMatchCompleted?.Invoke(type);
            onMatch?.Invoke();
            RefreshSlotUI();
        }

        private IEnumerator FlashAndShrink(Transform t)
        {
            float elapsed = 0f;
            Vector3 startScale = t.localScale;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(elapsed * 30f) * 0.1f;
                t.localScale = startScale * pulse;
                yield return null;
            }
        }

        private void RefreshSlotUI()
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (slots[i] == null) continue;
                if (i < _barItems.Count)
                    slots[i].SetItem(_barItems[i]);
                else
                    slots[i].SetEmpty();
            }
            GameEvents.OnCollectionBarUpdated?.Invoke(MAX_SLOTS - _barItems.Count);
        }

        /// <summary>Spring power-up: bỏ tất cả items ra khỏi bar và trả về board</summary>
        public List<ItemController> PopAllItems()
        {
            var items = new List<ItemController>(_barItems);
            _barItems.Clear();
            RefreshSlotUI();
            return items;
        }

        /// <summary>Spring power-up: lấy item cuối cùng ra khỏi bar</summary>
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
                item?.ReturnToPool();
            _barItems.Clear();
            RefreshSlotUI();
        }

        public List<ItemController> GetCurrentItems() => new List<ItemController>(_barItems);
        public int CurrentItemCount => _barItems.Count;
    }
}
