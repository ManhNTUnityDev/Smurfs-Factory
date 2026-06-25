namespace MatchFactory.Collection
{
    using MatchFactory.Board;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// Đại diện cho 1 ô trong Collection Bar (7 ô).
    /// Hiển thị item type bằng màu sắc và text label.
    /// </summary>
    public class CollectionSlot : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI itemLabel;
        [SerializeField] private Image itemColorDisplay;

        private ItemController _currentItem;
        private bool _isEmpty = true;

        public bool IsEmpty => _isEmpty;
        public ItemController CurrentItem => _currentItem;

        public void SetItem(ItemController item)
        {
            _currentItem = item;
            _isEmpty = false;

            // Show item type as colored square + label
            if (itemColorDisplay != null)
            {
                itemColorDisplay.color = GetColorForType(item.ItemType);
                itemColorDisplay.gameObject.SetActive(true);
            }

            if (itemLabel != null)
            {
                itemLabel.text = GetLabelForType(item.ItemType);
                itemLabel.gameObject.SetActive(true);
            }

            if (backgroundImage != null)
                backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }

        public void SetEmpty()
        {
            _currentItem = null;
            _isEmpty = true;

            if (itemColorDisplay != null)
                itemColorDisplay.gameObject.SetActive(false);

            if (itemLabel != null)
                itemLabel.text = "";

            if (backgroundImage != null)
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        }

        public static Color GetColorForType(ItemType type)
        {
            return type switch
            {
                ItemType.Box      => new Color(0.9f, 0.2f, 0.2f), // Red
                ItemType.Sphere   => new Color(0.2f, 0.8f, 0.2f), // Green
                ItemType.Capsule  => new Color(0.2f, 0.4f, 0.9f), // Blue
                ItemType.Cylinder => new Color(0.9f, 0.8f, 0.1f), // Yellow
                _                 => Color.white
            };
        }

        public static string GetLabelForType(ItemType type)
        {
            return type switch
            {
                ItemType.Box     => "BOX",
                ItemType.Sphere  => "SPH",
                ItemType.Capsule => "CAP",
                ItemType.Cylinder => "CYL",
                _                => type.ToString().Substring(0, Mathf.Min(3, type.ToString().Length))
            };
        }
    }
}
