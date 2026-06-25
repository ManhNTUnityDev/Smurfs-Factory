namespace MatchFactory.Input
{
    using MatchFactory.Board;
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Trung tâm xử lý input — raycast từ camera để detect tap vào items.
    /// Tại sao không dùng OnMouseDown trên Item: 
    /// Physics.Raycast trả về ĐÚNG collider đầu tiên bị hit (top-most).
    /// OnMouseDown gửi event đến tất cả colliders trong path.
    /// Hỗ trợ cả New Input System lẫn Legacy Input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private LayerMask itemLayerMask;

        private void Awake()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;
            if (gameCamera == null)
                gameCamera = FindObjectOfType<Camera>();
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            bool didTap = false;
            Vector3 screenPos = Vector3.zero;

            int pointerId = -1;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // New Input System
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                didTap = true;
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null &&
                     UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
                didTap = true;
                pointerId = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.touchId.ReadValue();
            }
#else
            // Legacy Input
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
                didTap = true;
            }
            else if (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                screenPos = touch.position;
                didTap = true;
                pointerId = touch.fingerId;
            }
#endif

            if (didTap)
            {
                // Check if clicking on UI
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    bool isOverUI = pointerId >= 0 ? 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(pointerId) : 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                    
                    if (isOverUI) return;
                }

                if (gameCamera != null)
                {
                    Ray ray = gameCamera.ScreenPointToRay(screenPos);
                    ProcessRaycast(ray);
                }
            }
        }

        private void ProcessRaycast(Ray ray)
        {
            int mask = itemLayerMask != 0 ? (int)itemLayerMask : ~0;
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, mask))
            {
                var item = hit.collider.GetComponent<ItemController>();
                if (item != null)
                {
                    Debug.Log($"[InputManager] Tapped: {item.ItemType}");
                    item.HandleTap();
                }
            }
        }
    }
}
