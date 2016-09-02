namespace BomberBot.Domain.Model
{
    class MapSafeBlock : MapBombPlacementBlock
    {
        public MapNode MapNode { get; set; }
        public int Probability { get; set; }
    }
}