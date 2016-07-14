using BomberBot.Common;

namespace BomberBot.Domain.Objects
{
    public class Bomb
    {
        public Player Owner { get; set; }
        public int BombRadius { get; set; }
        public int BombTimer { get; set; }
        public bool IsExploding { get; set; }
        public Location Location { get; set; }
    }
}