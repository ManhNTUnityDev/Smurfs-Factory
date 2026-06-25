namespace MatchFactory.Board
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Factory tạo Items từ pool theo ItemType.
    /// Tập trung logic tạo items, dễ mở rộng thêm loại mới.
    /// </summary>
    public class ItemFactory : MonoBehaviour
    {
        [System.Serializable]
        public struct ItemPrefabEntry
        {
            public ItemType type;
            public ItemController prefab;
        }

        [SerializeField] private ItemPrefabEntry[] prefabEntries;

        private Dictionary<ItemType, ItemController> _prefabMap;
        private ItemPool _pool;

        private void Awake()
        {
            _prefabMap = new Dictionary<ItemType, ItemController>();
            foreach (var entry in prefabEntries)
                _prefabMap[entry.type] = entry.prefab;
        }

        public void InjectPool(ItemPool pool)
        {
            _pool = pool;
            _pool.Initialize(_prefabMap);
        }

        public ItemController Create(ItemType type, Vector3 position, Quaternion rotation)
        {
            var item = _pool.Get(type);
            if (item == null) return null;
            item.transform.position = position;
            item.transform.rotation = rotation;
            item.Initialize(type);
            return item;
        }

        public void Recycle(ItemController item)
        {
            _pool.Return(item);
        }
    }
}
