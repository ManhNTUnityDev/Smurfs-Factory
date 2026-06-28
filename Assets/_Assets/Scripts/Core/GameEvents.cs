namespace MatchFactory.Core
{
    using MatchFactory.Board;
    using MatchFactory.Level;
    using System;

    /// <summary>
    /// Static event bus — tất cả cross-module communication đi qua đây.
    /// LUÔN null-check khi invoke: GameEvents.OnXxx?.Invoke(...)
    /// LUÔN unsubscribe trong OnDisable() để tránh memory leak.
    /// </summary>
    public static class GameEvents
    {
        // ========================
        // ITEM EVENTS
        // ========================

        /// <summary>Fired khi player tap vào 1 item 3D trên board</summary>
        public static Action<ItemController> OnItemTapped;

        /// <summary>Fired sau khi item đã bay vào và settle trong collection bar</summary>
        public static Action<ItemController> OnItemAddedToBar;

        /// <summary>Fired khi 3 items cùng loại được xoá khỏi bar</summary>
        public static Action<ItemType> OnMatchCompleted;

        // ========================
        // QUEST EVENTS
        // ========================

        /// <summary>Fired mỗi khi quest progress thay đổi (để update UI)</summary>
        public static Action<QuestData> OnQuestProgressUpdated;

        /// <summary>Fired khi 1 quest hoàn thành (trigger animation + SFX)</summary>
        public static Action<QuestData> OnQuestCompleted;

        // ========================
        // GAME STATE EVENTS
        // ========================

        /// <summary>Fired khi tất cả quests hoàn thành</summary>
        public static Action OnGameWin;

        /// <summary>Fired khi timer hết hoặc bar đầy</summary>
        public static Action OnGameLose;

        /// <summary>Fired mỗi frame khi timer chạy. float = thời gian còn lại</summary>
        public static Action<float> OnTimerTick;

        /// <summary>Fired khi game start sau khi load xong</summary>
        public static Action OnGameStarted;

        /// <summary>Fired khi game pause/resume. bool = isPaused</summary>
        public static Action<bool> OnGamePauseChanged;

        // ========================
        // POWER-UP EVENTS
        // ========================

        public static Action OnShuffleUsed;
        public static Action OnVacuumUsed;
        public static Action OnSpringUsed;
        public static Action OnIceGunUsed;

        // ========================
        // COLLECTION BAR EVENTS
        // ========================

        /// <summary>Fired khi collection bar đầy 7 ô</summary>
        public static Action OnCollectionBarFull;

        /// <summary>int = số slot còn trống sau khi update</summary>
        public static Action<int> OnCollectionBarUpdated;

        // ========================
        // UTILITY
        // ========================

        /// <summary>Reset tất cả events về null — gọi khi restart level</summary>
        public static void ClearAllListeners()
        {
            OnItemTapped = null;
            OnItemAddedToBar = null;
            OnMatchCompleted = null;
            OnQuestProgressUpdated = null;
            OnQuestCompleted = null;
            OnGameWin = null;
            OnGameLose = null;
            OnTimerTick = null;
            OnGameStarted = null;
            OnGamePauseChanged = null;
            OnShuffleUsed = null;
            OnVacuumUsed = null;
            OnSpringUsed = null;
            OnIceGunUsed = null;
            OnCollectionBarFull = null;
            OnCollectionBarUpdated = null;
        }
    }
}
