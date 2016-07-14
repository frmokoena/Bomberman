using BomberBot.Common;

namespace BomberBot.Domain.Objects
{
    public class Player : Entity
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public int Points { get; set; }
        public bool Killed { get; set; }
        public int BombBag { get; set; }
        public int BombRadius { get; set; }
    }
}