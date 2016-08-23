using BomberBot.Common;
using BomberBot.Domain.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;

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
                return GetBlockAtLocation(superLocation).PowerUp != null ? superLocation : null;
            }
        }
        public int WallsInitial
        {
            get
            {
                return ((PlayerBounty * Players.Count) - 100) / 10;
            }
        }
        public int WallsLeft
        {
            get
            {
                var wallsLeft = 0;
                for (var x = 1; x < MapWidth; x++)
                {
                    for (var y = 1; y < MapHeight; y++)
                    {
                        if (GetBlockAtLocation(x, y).IsDestructibleWall()) wallsLeft++;
                    }
                }
                return wallsLeft;
            }
        }
        public double PercentageWall
        {
            get
            {
                return 100 * ((double)WallsLeft / (double)WallsInitial);
            }
        }

        internal bool IsIndestructibleWall(int x, int y)
        {
            return GetBlockAtLocation(x,y).IsIndestructibleWall();
        }

        public int MaxBombBlast
        {
            get
            {
                return MapWidth > MapHeight ? MapWidth - 3 : MapHeight - 3;
            }
        }

        public double MaxRadiusPowerChase
        {
            get
            {
                return 0.6 * MaxBombBlast;
            }
        }

        public double MaxBagPowerChase
        {
            get
            {
                return 0.4 * MaxBombBlast;
            }
        }

        public Block GetBlockAtLocation(Location loc)
        {
            return Map[loc.X][loc.Y];
        }

        public Block GetBlockAtLocation(int x, int y)
        {
            return Map[x][y];
        }

        public Player GetPlayer(string key)
        {
            return Players.Find(p => p.Key == key);
        }

        public Location GetPlayerLocation(string playerKey)
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
                    var bomb = GetBlockAtLocation(x, y).Bomb;

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
            if (loc.X < 1 || loc.X > MapWidth - 1 || loc.Y < 1 || loc.Y > MapHeight - 1)
            {
                return false;
            }

            var block = GetBlockAtLocation(loc);

            if (block.IsIndestructibleWall()) return false;

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPowerUp();
        }

        public bool IsBlockBombClear(Location loc)
        {
            if (loc.X < 1 || loc.X > MapWidth - 1 || loc.Y < 1 || loc.Y > MapHeight - 1)
            {
                return false;
            }

            var block = GetBlockAtLocation(loc);

            if (block.IsIndestructibleWall()) return false;

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
            if (loc.X < 1 || loc.X > MapWidth - 1 || loc.Y < 1 || loc.Y > MapHeight - 1)
            {
                return false;
            }

            var block = GetBlockAtLocation(loc);

            if (block.IsIndestructibleWall()) return false;

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
            if (loc.X < 1 || loc.X > MapWidth - 1 || loc.Y < 1 || loc.Y > MapHeight - 1)
            {
                return false;
            }

            var block = GetBlockAtLocation(loc);

            return block.IsEmpty()
                || !block.IsIndestructibleWall();
        }

        public bool IsBomb(Location loc)
        {
            return GetBlockAtLocation(loc).IsBomb();
        }

        public bool IsDestructibleWall(Location loc)
        {
            return GetBlockAtLocation(loc).IsDestructibleWall();
        }

        public bool IsPowerUp(Location loc)
        {
            return GetBlockAtLocation(loc).IsPowerUp();
        }

        public bool IsPlayer(Location loc)
        {
            return GetBlockAtLocation(loc).IsPlayer();
        }

        public bool IsPlayerSittingOnBomb(Location loc)
        {
            return GetBlockAtLocation(loc).IsPlayerSittingOnBomb();
        }

        public bool IsBlockPlayerClear(Location loc)
        {
            if (loc.X < 1 || loc.X > MapWidth - 1 || loc.Y < 1 || loc.Y > MapHeight - 1)
            {
                return false;
            }

            var block = GetBlockAtLocation(loc);

            if (block.IsIndestructibleWall()) return false;

            return block.IsEmpty()
                || block.IsBombExploding()
                || block.IsPlayer()
                || block.IsPlayerSittingOnBomb()
                || block.IsPowerUp();
        }
    }
}