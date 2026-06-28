namespace MatchFactory.PowerUps
{
    using MatchFactory.Timer;
    using MatchFactory.Core;
    using UnityEngine;

    /// <summary>
    /// Ice Gun Power-up: Đóng băng timer trong 10 giây.
    /// </summary>
    public class IceGunCommand : ICommand
    {
        private readonly GameTimer _timer;
        private readonly float _duration;

        public IceGunCommand(GameTimer timer, float duration = 10f)
        {
            _timer = timer;
            _duration = duration;
        }

        public bool CanExecute() => _timer != null && _timer.IsRunning && !_timer.IsFrozen;

        public void Execute()
        {
            _timer.Freeze(_duration);
            Debug.Log($"[IceGunCommand] Timer frozen for {_duration}s");
        }

        public void Undo() { /* Ice Gun không có undo */ }
    }
}
