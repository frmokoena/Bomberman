using BomberBot.Common;

namespace BomberBot.Domain.Model
{
    public class MapBlock
    {
        public Location Location { get; set; }
        public int Distance { get; set; }
        public Location NextMove { get; set; }
        public int VisibleWalls { get; set; }
        public MapNode MapNode { get; set; }
    }
}