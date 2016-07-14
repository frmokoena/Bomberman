using BomberBot.Common;
using BomberBot.Enums;

namespace BomberBot.Domain.Objects
{
    public class Block
    {
        public Entity Entity { get; set; }
        public Bomb Bomb { get; set; }
        public PowerUp PowerUp { get; set; }
        public bool Exploding { get; set; }     
        public Location Location { get; set; }

        public ObjectInBlock EntityInBlock
        {
            get
            {
                if (Entity != null)
                {
                    if (Entity is IndestructibleWall) return ObjectInBlock.IndestructibleWall;
                    if (Entity is DestructibleWall) return ObjectInBlock.DestructibleWall;
                    if (Entity is Player)
                    {
                        if (Bomb != null) return ObjectInBlock.PlayerSittingOnBomb;
                        return ObjectInBlock.Player;
                    }
                }

                if (Exploding) return ObjectInBlock.BombExploding;

                if (Bomb != null) return ObjectInBlock.Bomb;

                if (PowerUp != null)
                {
                    if (PowerUp is BombBagPowerUp) return ObjectInBlock.BombBagPowerUp;
                    if (PowerUp is BombRadiusPowerUp) return ObjectInBlock.BombRadiusPowerUp;
                    if (PowerUp is SuperPowerUp) return ObjectInBlock.SuperPowerUp;
                }
                return ObjectInBlock.EmptyBlock;
            }
        }
    }
}