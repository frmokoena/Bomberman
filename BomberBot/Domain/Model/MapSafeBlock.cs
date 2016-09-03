namespace BomberBot.Domain.Model
{
    public class MapSafeBlock : MapBombPlacementBlock
    {
        public MapNode MapNode { get; set; }
        public int Probability { get; set; }
    }
}