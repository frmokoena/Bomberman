using BomberBot.Common;

namespace BomberBot.Domain.Model
{
    public abstract class MapBlock
    {
        public Location Location { get; set; }
        public int Distance { get; set; }
        public Location NextMove { get; set; }
    }
}