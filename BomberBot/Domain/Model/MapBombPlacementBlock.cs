namespace BomberBot.Domain.Model
{
    public class MapBombPlacementBlock : MapBlock
    {
        public int VisibleWalls { get; set; }
        public int PowerDistance { get; set; }
        public int SuperDistance { get; set; }
    }
}