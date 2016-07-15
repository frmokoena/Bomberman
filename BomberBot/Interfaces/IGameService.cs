using BomberBot.Enums;

namespace BomberBot.Interfaces
{
    public interface IGameService<T>
    {
        string HomeKey { get; set; }
        T GameState { get; }
        void WriteMove(Move move);
    }
}