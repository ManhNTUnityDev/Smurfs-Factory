namespace MatchFactory.Board
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Object Pool cho Items 3D — tránh GC spike khi spawn/destroy liên tục.
    /// </summary>
    public class ItemPool : MonoBehaviour
    {
        [SerializeField] private int initialPoolSizePerType = 15;

        private Dictionary<ItemType, Queue<ItemController>> _pools = new();
        private Dictionary<ItemType, ItemController> _prefabMap;

        public void Initialize(Dictionary<ItemType, ItemController> prefabMap)
        {
            _prefabMap = prefabMap;
            foreach (var kvp in prefabMap)
            {
                _pools[kvp.Key] = new Queue<ItemController>();
                for (int i = 0; i < initialPoolSizePerType; i++)
                    CreateNew(kvp.Key);
            }
            Debug.Log($"[{nameof(ItemPool)}] Initialized pool with {prefabMap.Count} types x {initialPoolSizePerType} items each.");
        }

        public ItemController Get(ItemType type)
        {
            if (_pools.ContainsKey(type) && _pools[type].Count > 0)
            {
                var item = _pools[type].Dequeue();
                item.gameObject.SetActive(true);
                return item;
            }
            return CreateNew(type);
        }

        public void Return(ItemController item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(transform);
            item.transform.localScale = Vector3.one;
            if (_pools.ContainsKey(item.ItemType))
                _pools[item.ItemType].Enqueue(item);
        }

        private ItemController CreateNew(ItemType type)
        {
            if (!_prefabMap.ContainsKey(type))
            {
                Debug.LogError($"[{nameof(ItemPool)}] No prefab registered for {type}");
                return null;
            }
            var go = Instantiate(_prefabMap[type], transform);
            go.gameObject.SetActive(false);
            go.OnReturnToPool = () => Return(go);
            return go;
        }
    }
}
