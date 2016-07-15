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

        public bool IsEmpty()
        {
            return Entity == null 
                && Bomb == null 
                && PowerUp == null 
                && !Exploding;
        }

        public bool IsBombExploding()
        {
            return Exploding;
        }

        public bool IsPlayer()
        {
            if (Entity is Player)
            {
                if (Bomb != null) return false;
                return true;                
            }
            return false;
        }

        public bool IsPlayerSittingOnBomb()
        {
            if (Entity is Player)
            {
                if (Bomb != null) return true;
                return false;
            }
            return false;
        }

        public bool IsIndestructibleWall()
        {
            return Entity is IndestructibleWall;
        }

        public bool IsDestructibleWall()
        {
            return Entity is DestructibleWall;
        }

        public bool IsBomb()
        {
            return Bomb != null;
        }

        public bool IsPowerUp()
        {
            return PowerUp != null;
        }

        public bool IsSuperPowerUp()
        {
            return PowerUp is SuperPowerUp;
        }

        public bool IsBombRadiusPowerUp()
        {
            return PowerUp is BombRadiusPowerUp;
        }

        public bool IsBombBagPowerUp()
        {
            return PowerUp is BombBagPowerUp;
        }
    }
}