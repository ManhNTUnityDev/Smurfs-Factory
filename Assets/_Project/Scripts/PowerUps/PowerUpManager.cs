namespace MatchFactory.PowerUps
{
    using MatchFactory.Board;
    using MatchFactory.Collection;
    using MatchFactory.Quest;
    using MatchFactory.Timer;
    using MatchFactory.Core;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// Quản lý tất cả 4 power-ups sử dụng Command Pattern.
    /// Mỗi power-up có button riêng, count riêng, và command riêng.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private CollectionBar collectionBar;
        [SerializeField] private QuestManager questManager;

        [Header("Ice Gun Settings")]
        [SerializeField] private float iceGunFreezeDuration = 10f;

        [Header("Initial Counts")]
        [SerializeField] private int shuffleCount = 3;
        [SerializeField] private int vacuumCount = 3;
        [SerializeField] private int springCount = 3;
        [SerializeField] private int iceGunCount = 3;

        [Header("UI Buttons")]
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button vacuumButton;
        [SerializeField] private Button springButton;
        [SerializeField] private Button iceGunButton;

        [Header("Count Labels")]
        [SerializeField] private TextMeshProUGUI shuffleCountText;
        [SerializeField] private TextMeshProUGUI vacuumCountText;
        [SerializeField] private TextMeshProUGUI springCountText;
        [SerializeField] private TextMeshProUGUI iceGunCountText;

        private ICommand _lastCommand;

        private void Start()
        {
            UpdateButtonUI();
        }

        public void UseShuffleCommand()
        {
            if (shuffleCount <= 0) return;
            var cmd = new ShuffleCommand(boardManager);
            if (ExecuteCommand(cmd))
            {
                shuffleCount--;
                UpdateButtonUI();
            }
        }

        public void UseVacuumCommand()
        {
            if (vacuumCount <= 0) return;
            var cmd = new VacuumCommand(boardManager, collectionBar, questManager);
            if (ExecuteCommand(cmd))
            {
                vacuumCount--;
                UpdateButtonUI();
            }
        }

        public void UseSpringCommand()
        {
            if (springCount <= 0) return;
            var cmd = new SpringCommand(collectionBar, boardManager);
            if (ExecuteCommand(cmd))
            {
                springCount--;
                UpdateButtonUI();
            }
        }

        public void UseIceGunCommand()
        {
            if (iceGunCount <= 0) return;
            var timer = GameTimer.Instance;
            var cmd = new IceGunCommand(timer, iceGunFreezeDuration);
            if (ExecuteCommand(cmd))
            {
                iceGunCount--;
                UpdateButtonUI();
            }
        }

        private bool ExecuteCommand(ICommand cmd)
        {
            if (!cmd.CanExecute())
            {
                Debug.Log($"[{nameof(PowerUpManager)}] Command cannot execute.");
                return false;
            }
            cmd.Execute();
            _lastCommand = cmd;
            return true;
        }

        private void UpdateButtonUI()
        {
            SetButtonState(shuffleButton, shuffleCountText, shuffleCount, "🔀");
            SetButtonState(vacuumButton, vacuumCountText, vacuumCount, "🌀");
            SetButtonState(springButton, springCountText, springCount, "🌿");
            SetButtonState(iceGunButton, iceGunCountText, iceGunCount, "🧊");
        }

        private void SetButtonState(Button btn, TextMeshProUGUI label, int count, string emoji)
        {
            if (btn != null)
                btn.interactable = count > 0;
            if (label != null)
                label.text = $"{emoji} x{count}";
        }
    }
}
