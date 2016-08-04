namespace BomberBot.Domain.Model
{
    class MapBombPlacementBlock : MapBlock
    {
        public int VisibleWalls { get; set; }
        public int PowerDistance { get; set; }
        public int SuperDistance { get; set; }
    }
}