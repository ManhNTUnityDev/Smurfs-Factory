namespace MatchFactory.Core
{
    public enum GameState
    {
        Idle,       // Chưa bắt đầu
        Loading,    // Đang load level
        Playing,    // Đang chơi
        Paused,     // Tạm dừng
        Win,        // Thắng
        Lose        // Thua
    }
}
