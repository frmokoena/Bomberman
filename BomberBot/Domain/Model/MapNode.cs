using BomberBot.Common;

namespace BomberBot.Domain.Model
{
    public class MapNode
    {
        public MapNode Parent { get; set; }
        public Location Location { get; set; }
        public int GCost { get; set; }
        public int HCost { get; set; }
        public int FCost { get; set; }
    }
}
