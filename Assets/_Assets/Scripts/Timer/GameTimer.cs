namespace MatchFactory.Timer
{
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Đếm ngược thời gian gameplay.
    /// Hỗ trợ Pause/Resume và Freeze (Ice Gun power-up).
    /// Singleton — truy cập qua GameTimer.Instance.
    /// </summary>
    public class GameTimer : Singleton<GameTimer>
    {
        public float RemainingTime { get; private set; }
        public float TotalTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFrozen { get; private set; }

        private float _freezeDuration;

        public float NormalizedTimeRemaining => TotalTime > 0 ? RemainingTime / TotalTime : 0f;

        public void Initialize(float totalSeconds)
        {
            TotalTime = totalSeconds;
            RemainingTime = totalSeconds;
            IsRunning = false;
            IsFrozen = false;
            _freezeDuration = 0f;
            Debug.Log($"[{nameof(GameTimer)}] Initialized: {totalSeconds}s");
        }

        public void Resume()
        {
            IsRunning = true;
            Debug.Log($"[{nameof(GameTimer)}] Resumed. Remaining: {RemainingTime:F1}s");
        }

        public void Pause()
        {
            IsRunning = false;
        }

        /// <summary>Ice Gun power-up: đóng băng timer trong durationSeconds giây</summary>
        public void Freeze(float durationSeconds)
        {
            IsFrozen = true;
            _freezeDuration = durationSeconds;
            GameEvents.OnIceGunUsed?.Invoke();
            Debug.Log($"[{nameof(GameTimer)}] Frozen for {durationSeconds}s");
        }

        private void Update()
        {
            if (!IsRunning) return;

            if (IsFrozen)
            {
                _freezeDuration -= Time.deltaTime;
                if (_freezeDuration <= 0f)
                {
                    IsFrozen = false;
                    Debug.Log($"[{nameof(GameTimer)}] Freeze ended.");
                }
                // Still fire tick so UI updates (shows frozen state)
                GameEvents.OnTimerTick?.Invoke(RemainingTime);
                return;
            }

            RemainingTime -= Time.deltaTime;
            RemainingTime = Mathf.Max(0f, RemainingTime);
            GameEvents.OnTimerTick?.Invoke(RemainingTime);

            if (RemainingTime <= 0f)
            {
                IsRunning = false;
                Debug.Log($"[{nameof(GameTimer)}] Time's up! Triggering lose.");
                GameManager.Instance?.TriggerLose();
            }
        }
    }
}
