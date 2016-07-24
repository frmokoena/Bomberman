using BomberBot.Common;
using BomberBot.Domain.Objects;
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

        public Location SuperLocation
        {
            get
            {
                var superLocation = new Location(MapWidth / 2, MapHeight / 2);
                return GetBlock(superLocation).PowerUp != null ? superLocation : null;
            }
        }

        public bool WallsExhausted
        {
            get
            {
                for (var x = 1; x < MapWidth; x++)
                {
                    for (var y = 1; y < MapHeight; y++)
                    {
                        if (GetBlock(x, y).IsDestructibleWall()) return false;
                    }
                }
                return true;
            }
        }

        public Block GetBlock(Location loc)
        {
            return Map[loc.X][loc.Y];
        }

        public Block GetBlock(int x, int y)
        {
            return Map[x][y];
        }

        public Player GetPlayer(string key)
        {
            return Players.Find(p => p.Key == key);
        }

        public Location FindPlayerLocationOnMap(string playerKey)
        {
            var player = Players.Find(p => p.Key == playerKey);
            if (player.Killed) return null;
            return new Location(player.Location.X - 1, player.Location.Y - 1);
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

        public bool IsBlockClear(Location loc)
        {
            var block = GetBlock(loc);

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPowerUp();
        }

        public bool IsBlockBombClear(Location loc)
        {
            var block = GetBlock(loc);

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPlayer()
                || block.IsPlayerSittingOnBomb()
                || block.IsBomb()
                || block.IsPowerUp();
        }

        //plant clear
        public bool IsBlockPlantClear(Location loc)
        {
            var block = GetBlock(loc);

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPlayer()
                || block.IsPlayerSittingOnBomb()
                || block.IsDestructibleWall()
                || block.IsPowerUp();
        }


        //actual super
        public bool IsBlockSuperClear(Location loc)
        {
            var block = GetBlock(loc);
            return block.IsEmpty()
                || !block.IsIndestructibleWall();
        }

        public bool IsBomb(Location loc)
        {
            return GetBlock(loc).IsBomb();
        }

        public bool IsDestructibleWall(Location loc)
        {
            return GetBlock(loc).IsDestructibleWall();
        }

        public bool IsPowerUp(Location loc)
        {
            return GetBlock(loc).IsPowerUp();
        }

        public bool IsPlayer(Location loc)
        {
            return GetBlock(loc).IsPlayer();
        }

        public bool IsPlayerSittingOnBomb(Location loc)
        {
            return GetBlock(loc).IsPlayerSittingOnBomb();
        }

        public bool IsBlockPlayerClear(Location loc)
        {
            var block = GetBlock(loc);

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPlayer()
                || block.IsPlayerSittingOnBomb()
                || block.IsPowerUp();
        }
    }
}