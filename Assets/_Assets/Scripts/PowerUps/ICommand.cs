namespace MatchFactory.PowerUps
{
    /// <summary>
    /// Command Pattern interface — mỗi power-up implement interface này.
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
