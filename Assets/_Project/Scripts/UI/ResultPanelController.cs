namespace MatchFactory.UI
{
    using MatchFactory.Core;
    using MatchFactory.Timer;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// Quản lý Result Panel (Win/Lose screen) với star rating.
    /// </summary>
    public class ResultPanelController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject resultPanel;

        [Header("Win/Lose")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Stars")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Color starActiveColor = Color.yellow;
        [SerializeField] private Color starInactiveColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextButton;

        private void OnEnable()
        {
            GameEvents.OnGameWin  += OnWin;
            GameEvents.OnGameLose += OnLose;
        }

        private void OnDisable()
        {
            GameEvents.OnGameWin  -= OnWin;
            GameEvents.OnGameLose -= OnLose;
        }

        private void Start()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        private void OnWin()
        {
            float timeRemaining = GameTimer.Instance?.RemainingTime ?? 0f;
            float totalTime = GameTimer.Instance?.TotalTime ?? 225f;
            int stars = CalculateStars(timeRemaining, totalTime);
            ShowResult(true, stars, timeRemaining);
        }

        private void OnLose()
        {
            ShowResult(false, 0, 0f);
        }

        private void ShowResult(bool won, int stars, float timeRemaining)
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = won ? "🎉 YOU WIN!" : "💀 GAME OVER";
                titleText.color = won ? Color.yellow : Color.red;
            }

            if (subtitleText != null)
            {
                if (won)
                {
                    int mins = Mathf.FloorToInt(timeRemaining / 60f);
                    int secs = Mathf.FloorToInt(timeRemaining % 60f);
                    subtitleText.text = $"Time remaining: {mins:00}:{secs:00}";
                }
                else
                {
                    subtitleText.text = "Better luck next time!";
                }
            }

            // Set stars
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                        starImages[i].color = i < stars ? starActiveColor : starInactiveColor;
                }
            }

            // Show retry, hide next if lose
            if (retryButton != null) retryButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(won);

            Debug.Log($"[ResultPanel] Showing: {(won ? "WIN" : "LOSE")}, Stars: {stars}");
        }

        private int CalculateStars(float timeRemaining, float totalTime)
        {
            // > 40% thời gian còn lại → 3 sao
            if (timeRemaining >= totalTime * 0.4f) return 3;
            // 20-40% → 2 sao
            if (timeRemaining >= totalTime * 0.2f) return 2;
            // < 20% → 1 sao
            return 1;
        }

        public void OnRetryClicked()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            GameManager.Instance?.RestartGame();
        }
    }
}
