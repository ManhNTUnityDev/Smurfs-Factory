namespace MatchFactory.Board
{
    using MatchFactory.Core;
    using System;
    using UnityEngine;

    /// <summary>
    /// MonoBehaviour gắn trên mỗi 3D Item prefab.
    /// Xử lý tap input, trạng thái fly, và physics state.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ItemController : MonoBehaviour
    {
        // --- Public Properties ---
        public ItemType ItemType { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsTapped { get; private set; }
        public Action OnReturnToPool { get; set; }

        // --- Private ---
        private Rigidbody _rb;
        private Collider _col;
        private MeshRenderer _meshRenderer;
        private Color _originalColor;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(ItemType type)
        {
            ItemType = type;
            IsFlying = false;
            IsTapped = false;
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _col.enabled = true;
            gameObject.SetActive(true);
        }

        /// <summary>Gọi từ InputManager (raycast) khi player tap vào item</summary>
        public void HandleTap()
        {
            if (IsTapped || IsFlying) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            IsTapped = true;
            _col.enabled = false;
            _rb.isKinematic = true;

            GameEvents.OnItemTapped?.Invoke(this);
        }

        /// <summary>Gọi từ power-up (Vacuum) để tap programmatically</summary>
        public void TriggerTapFromPowerUp()
        {
            HandleTap();
        }

        public void SetFlying(bool state)
        {
            IsFlying = state;
            _rb.isKinematic = state;
            _col.enabled = !state;
        }

        public void ReturnToPool()
        {
            IsFlying = false;
            IsTapped = false;
            OnReturnToPool?.Invoke();
        }
    }
}
