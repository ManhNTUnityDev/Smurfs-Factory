namespace MatchFactory.Core
{
    using MatchFactory.Level;
    using MatchFactory.Timer;
    using MatchFactory.Quest;
    using MatchFactory.Collection;
    using MatchFactory.Board;
    using UnityEngine;

    /// <summary>
    /// Singleton trung tâm — quản lý GameState machine và điều phối tất cả managers.
    /// Entry point của gameplay scene.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // --- Public Properties ---
        public GameState CurrentState => _currentState;
        public int EarnedStars => _earnedStars;

        // --- Private Fields ---
        private GameState _currentState = GameState.Idle;
        private int _earnedStars = 0;

        // --- References (set via Inspector) ---
        [SerializeField] private QuestManager questManager;
        [SerializeField] private CollectionBar collectionBar;
        [SerializeField] private BoardManager boardManager;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            // Auto-start nếu DemoGameStarter chưa gọi
            if (_currentState == GameState.Idle)
            {
                Debug.Log($"[{nameof(GameManager)}] Auto-start prevented. Call StartGame() via DemoGameStarter.");
            }
        }

        /// <summary>Khởi động gameplay với LevelData đã được load sẵn</summary>
        public void StartGame(LevelData levelData)
        {
            StartCoroutine(StartGameCoroutine(levelData));
        }

        private System.Collections.IEnumerator StartGameCoroutine(LevelData levelData)
        {
            ChangeState(GameState.Loading);

            // Wait one frame for all Start() callbacks to complete
            yield return null;

            // Init timer
            GameTimer.Instance.Initialize(levelData.timeLimitSeconds);

            // Init quests
            questManager.InitializeQuests(levelData.quests);

            // Spawn items
            boardManager.SpawnItems(levelData);

            // Short delay to let physics settle
            yield return new WaitForSeconds(0.8f);
            BeginPlaying();
        }

        private void BeginPlaying()
        {
            ChangeState(GameState.Playing);
            GameEvents.OnGameStarted?.Invoke();
        }

        public void TriggerWin(float timeRemaining)
        {
            if (_currentState == GameState.Win || _currentState == GameState.Lose) return;

            _earnedStars = CalculateStars(timeRemaining);
            ChangeState(GameState.Win);
        }

        public void TriggerLose()
        {
            if (_currentState == GameState.Win || _currentState == GameState.Lose) return;
            ChangeState(GameState.Lose);
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
                ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

        public void RestartGame()
        {
            Debug.Log($"[{nameof(GameManager)}] Restarting... (reload scene)");
            GameEvents.ClearAllListeners();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private int CalculateStars(float timeRemaining)
        {
            // > 40% còn lại = 3 sao, 20-40% = 2 sao, <20% = 1 sao
            float total = GameTimer.Instance?.TotalTime ?? 225f;
            if (timeRemaining >= total * 0.4f) return 3;
            if (timeRemaining >= total * 0.2f) return 2;
            return 1;
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            // Exit current state
            switch (_currentState)
            {
                case GameState.Playing:
                    GameTimer.Instance?.Pause();
                    break;
                case GameState.Paused:
                    Time.timeScale = 1f;
                    break;
            }

            _currentState = newState;
            Debug.Log($"[{nameof(GameManager)}] State: {newState}");

            // Enter new state
            switch (_currentState)
            {
                case GameState.Loading:
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    GameTimer.Instance?.Resume();
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    GameEvents.OnGamePauseChanged?.Invoke(true);
                    break;
                case GameState.Win:
                    Time.timeScale = 1f;
                    Debug.Log($"[{nameof(GameManager)}] WIN! Stars: {_earnedStars}");
                    GameEvents.OnGameWin?.Invoke();
                    break;
                case GameState.Lose:
                    Time.timeScale = 1f;
                    Debug.Log($"[{nameof(GameManager)}] LOSE!");
                    GameEvents.OnGameLose?.Invoke();
                    break;
            }
        }
    }
}
