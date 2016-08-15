using BomberBot.Common;
using BomberBot.Domain.Objects;

namespace BomberBot.Domain.Model
{
    class MapOpponent
    {
        public Player Player { get; set; }
        public readonly Location Location;

        public MapOpponent(Player player)
        {
            Player = player;
            Location = new Location(player.Location.X - 1, player.Location.Y - 1);
        }
    }
}
