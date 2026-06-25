namespace MatchFactory.Board
{
    using MatchFactory.Core;
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
        [SerializeField] private Vector3 spawnCenter = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Vector3 spawnSize = new Vector3(4f, 1f, 4f);
        [SerializeField] private float spawnHeightStep = 0.5f;
        [SerializeField] private Transform spawnParent;

        [Header("References")]
        [SerializeField] private ItemFactory itemFactory;
        [SerializeField] private ItemPool itemPool;

        public int ActiveItemCount => _activeItems.Count;

        private List<ItemController> _activeItems = new List<ItemController>();
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                _mainCamera = FindObjectOfType<Camera>();
        }

        private void Start()
        {
            // InjectPool here — called after ALL Awake() have run
            if (itemPool != null && itemFactory != null)
                itemFactory.InjectPool(itemPool);
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
                for (int i = 0; i < cfg.count; i++)
                    list.Add(cfg.itemType);

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
            float spacingX = spawnSize.x / cols;
            float spacingZ = spawnSize.z / cols;

            for (int i = 0; i < list.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                Vector3 pos = spawnCenter + new Vector3(
                    (col - cols / 2f) * spacingX + Random.Range(-0.1f, 0.1f),
                    row * spawnHeightStep + 2f,
                    Random.Range(-spawnSize.z * 0.4f, spawnSize.z * 0.4f)
                );

                var item = itemFactory.Create(list[i], pos, Random.rotation);
                if (item != null)
                {
                    item.transform.SetParent(spawnParent != null ? spawnParent : transform);
                    _activeItems.Add(item);
                }

                if (i % 5 == 4) yield return null; // Spread over frames
            }

            Debug.Log($"[{nameof(BoardManager)}] Spawned {_activeItems.Count} items.");
        }

        public void RemoveItem(ItemController item)
        {
            _activeItems.Remove(item);
            itemFactory.Recycle(item);
        }

        public ItemController[] GetAllActiveItems() => _activeItems.ToArray();

        /// <summary>Shuffle: apply random impulse forces to all items (Shuffle power-up)</summary>
        public void ShuffleItems()
        {
            foreach (var item in _activeItems)
            {
                if (item == null) continue;
                var rb = item.GetComponent<Rigidbody>();
                if (rb == null || rb.isKinematic) continue;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Vector3 impulse = new Vector3(
                    Random.Range(-4f, 4f),
                    Random.Range(3f, 7f),
                    Random.Range(-4f, 4f));
                rb.AddForce(impulse, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            GameEvents.OnShuffleUsed?.Invoke();
            Debug.Log($"[{nameof(BoardManager)}] Shuffle executed.");
        }

        /// <summary>Tìm accessible items theo type cho Vacuum power-up</summary>
        public List<ItemController> GetAccessibleItems(ItemType type, int maxCount = 3)
        {
            var candidates = new List<ItemController>();
            foreach (var item in _activeItems)
                if (item != null && item.ItemType == type && !item.IsTapped && !item.IsFlying)
                    candidates.Add(item);

            // Sort by Y descending (items trên cao dễ lấy hơn)
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
            if (_mainCamera == null) return true;
            Vector3 dir = item.transform.position - _mainCamera.transform.position;
            int itemLayer = LayerMask.GetMask("Items");
            if (itemLayer == 0) return true; // Layer chưa được tạo, fallback

            if (Physics.Raycast(_mainCamera.transform.position, dir.normalized,
                out RaycastHit hit, dir.magnitude + 0.1f, itemLayer))
            {
                return hit.collider.gameObject == item.gameObject;
            }
            return false;
        }

        public void AddItemToBoard(ItemController item, Vector3 position)
        {
            _activeItems.Add(item);
            item.transform.position = position;
            item.SetFlying(false);
            item.Initialize(item.ItemType);
        }

        private void ClearBoard()
        {
            foreach (var item in _activeItems)
                if (item != null) itemFactory.Recycle(item);
            _activeItems.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnCenter, spawnSize);
        }
    }
}
