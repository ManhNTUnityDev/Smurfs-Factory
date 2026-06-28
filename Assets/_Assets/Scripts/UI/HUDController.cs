namespace MatchFactory.UI
{
    using MatchFactory.Core;
    using MatchFactory.Level;
    using MatchFactory.Timer;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// Presenter cho HUD — lắng nghe events từ Game Systems và cập nhật UI.
    /// MVP Pattern: Model = GameTimer/QuestManager, View = TextMeshPro/Images, Presenter = HUDController.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Timer UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerBackground;
        [SerializeField] private TextMeshProUGUI frozenIndicator;

        [Header("Info UI")]
        [SerializeField] private TextMeshProUGUI barSlotsText;
        [SerializeField] private TextMeshProUGUI statusText;

        private void OnEnable()
        {
            GameEvents.OnTimerTick           += UpdateTimerDisplay;
            GameEvents.OnCollectionBarUpdated += UpdateBarSlots;
            GameEvents.OnIceGunUsed          += OnIceGunUsed;
            GameEvents.OnGameWin             += OnGameWin;
            GameEvents.OnGameLose            += OnGameLose;
            GameEvents.OnShuffleUsed         += () => ShowStatus("🔀 Shuffle!");
            GameEvents.OnVacuumUsed          += () => ShowStatus("🌀 Vacuum!");
            GameEvents.OnSpringUsed          += () => ShowStatus("🌿 Spring!");
        }

        private void OnDisable()
        {
            GameEvents.OnTimerTick           -= UpdateTimerDisplay;
            GameEvents.OnCollectionBarUpdated -= UpdateBarSlots;
            GameEvents.OnIceGunUsed          -= OnIceGunUsed;
            GameEvents.OnGameWin             -= OnGameWin;
            GameEvents.OnGameLose            -= OnGameLose;
        }

        private void UpdateTimerDisplay(float remainingSeconds)
        {
            if (timerText == null) return;

            int mins = Mathf.FloorToInt(remainingSeconds / 60f);
            int secs = Mathf.FloorToInt(remainingSeconds % 60f);
            timerText.text = $"{mins:00}:{secs:00}";

            // Urgent warning: red when < 30s
            if (timerText != null)
            {
                if (remainingSeconds < 30f)
                    timerText.color = Color.red;
                else if (remainingSeconds < 60f)
                    timerText.color = new Color(1f, 0.6f, 0f); // Orange
                else
                    timerText.color = Color.white;
            }

            // Frozen indicator
            bool frozen = GameTimer.Instance?.IsFrozen ?? false;
            if (frozenIndicator != null)
            {
                frozenIndicator.gameObject.SetActive(frozen);
                if (frozen) frozenIndicator.text = "❄️ FROZEN";
            }

            if (timerBackground != null)
                timerBackground.color = frozen ? new Color(0.4f, 0.8f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.6f);
        }

        private void UpdateBarSlots(int slotsRemaining)
        {
            if (barSlotsText != null)
                barSlotsText.text = $"Slots: {slotsRemaining}/7";
        }

        private void OnIceGunUsed()
        {
            ShowStatus("🧊 FROZEN 10s!");
        }

        private void OnGameWin()
        {
            if (statusText != null)
            {
                statusText.text = "🎉 YOU WIN!";
                statusText.color = Color.yellow;
                statusText.gameObject.SetActive(true);
            }
        }

        private void OnGameLose()
        {
            if (statusText != null)
            {
                statusText.text = "💀 GAME OVER";
                statusText.color = Color.red;
                statusText.gameObject.SetActive(true);
            }
        }

        private void ShowStatus(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
                statusText.color = Color.cyan;
                statusText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideStatus));
                Invoke(nameof(HideStatus), 1.5f);
            }
        }

        private void HideStatus()
        {
            if (statusText != null)
                statusText.gameObject.SetActive(false);
        }
    }
}
