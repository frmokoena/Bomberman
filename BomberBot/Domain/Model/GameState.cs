using BomberBot.Common;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BomberBot.Domain.Model
{
    public class GameState
    {
        public int CurrentRound { get; set; }
        public int PlayerBounty { get; set; }
        public int MapHeight { get; set; }
        public int MapWidth { get; set; }

        [JsonProperty("RegisteredPlayerEntities")]
        public List<Player> Players { get; set; }

        [JsonProperty("GameBlocks")]
        public Block[][] Map { get; set; }

        public Block GetBlock(Location loc)
        {

            return Map[loc.X][loc.Y];
        }

        public Block GetBlock(int x, int y)
        {

            return Map[x][y];
        }

        public Location FindPlayerLocationOnMap(string playerKey)
        {
            var player = Players.Find(p => p.Key == playerKey);
            if (player.Killed) return null;
            return new Location(player.Location.X - 1, player.Location.Y - 1);
        }

        public bool IsEmptyBlock(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.EmptyBlock;
        }

        public bool IsBombExploding(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.BombExploding;
        }

        public bool IsPlayer(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.Player;
        }

        public bool IsPlayerSittingOnBomb(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.PlayerSittingOnBomb;
        }

        public List<Bomb> GetPlayerBombs(string playerKey)
        {
            var bombs = new List<Bomb>();

            for (var x = 1; x < MapWidth; x++)
            {
                for (var y = 1; y < MapHeight; y++)
                {
                    var bomb = GetBlock(x, y).Bomb;

                    if (bomb == null)
                    {
                        continue;
                    }

                    if (bomb.Owner.Key == playerKey && !bomb.IsExploding)
                    {
                        bombs.Add(bomb);
                    }
                }
            }

            return bombs.Count == 0 ? null : bombs.OrderBy(b => b.BombTimer).ToList();
        }

        public bool IsDestructibleWall(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.DestructibleWall;
        }

        public bool IsBomb(Location loc)
        {
            var entity = GetBlock(loc).EntityInBlock;
            return entity == ObjectInBlock.Bomb || entity == ObjectInBlock.PlayerSittingOnBomb;
        }

        public bool IsSuperPowerUp(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.SuperPowerUp;
        }

        public bool IsBombRadiusPowerUp(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.BombRadiusPowerUp;
        }

        public bool IsBombBagPowerUp(Location loc)
        {
            return GetBlock(loc).EntityInBlock == ObjectInBlock.BombBagPowerUp;
        }

        public bool IsPowerUp(Location loc)
        {
            var entity = GetBlock(loc).PowerUp;
            return entity != null;
        }

        //private bool IsValidBlock(Location loc)
        //{
        //    if((loc.X > 0 && loc.X < MapHeight - 1)
        //        && (loc.Y > 0 && loc.Y < MapWidth - 1))
        //    {
        //        var entity = GetBlock(loc).EntityInBlock;
        //        if(entity != ObjectInBlock.IndestructibleWall)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public bool IsBlockClear(Location loc)
        {
            var block = GetBlock(loc);
            var entity = block.EntityInBlock;

            return (entity == ObjectInBlock.EmptyBlock
                || entity == ObjectInBlock.BombExploding
                || entity == ObjectInBlock.Player
                || block.PowerUp != null);
        }

        public bool IsBlockBombClear(Location loc)
        {
            var block = GetBlock(loc);
            var entity = block.EntityInBlock;

            return (entity == ObjectInBlock.EmptyBlock
                || entity == ObjectInBlock.BombExploding
                || entity == ObjectInBlock.Player
                || entity == ObjectInBlock.PlayerSittingOnBomb
                || entity == ObjectInBlock.Bomb
                || block.PowerUp != null);
        }

        // Open to move to
        public bool IsBlockSafe(Location loc)
        {
            var block = GetBlock(loc);
            var entity = block.EntityInBlock;

            return (entity == ObjectInBlock.EmptyBlock
                || entity == ObjectInBlock.BombExploding
                || block.PowerUp != null);
        }

        //plant clear
        public bool IsBlockPlantClear(Location loc)
        {
            var block = GetBlock(loc);
            var entity = block.EntityInBlock;

            return (entity == ObjectInBlock.EmptyBlock
                || entity == ObjectInBlock.DestructibleWall
                || entity == ObjectInBlock.BombExploding
                || entity == ObjectInBlock.Player
                || entity == ObjectInBlock.PlayerSittingOnBomb
                || block.PowerUp != null);
        }
    }
}
