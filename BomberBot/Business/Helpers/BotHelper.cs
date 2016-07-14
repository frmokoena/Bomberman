using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BomberBot.Business.Helpers
{
    public class BotHelper
    {
        public static bool IsValidBlock(GameState state, Location loc)
        {
            return (loc.X > 0 && loc.X < state.MapHeight - 1)
                && (loc.Y > 0 && loc.Y < state.MapWidth - 1);
        }

        /// <summary>
        /// Possible move Locations
        /// </summary>
        /// <param name="state"></param>
        /// <param name="start"></param>
        /// <param name="curLoc"></param>
        /// <returns>next posssible move Locations</returns>
        public static List<Location> FindMoveLocations(GameState state, Location start, Location curLoc, bool stayClear = false)
        {
            Location loc;
            var movesLoc = new List<Location>();

            if (curLoc.Equals(start))
            {
                List<Bomb> bombs;
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }


                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

            }
            else
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }
            }
            return movesLoc;
        }

        /// <summary>
        /// Shortest route to target
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static MapNode BuildPathToTarget(GameState state, Location start, Location target, bool stayClear = false)
        {
            var openList = new List<MapNode> { new MapNode { Parent = null, Location = start, GCost = 0, HCost = 0, FCost = 0 } };
            var closedList = new List<MapNode>();
            int gCost, hCost, fCost;

            while (openList.Count != 0)
            {
                var q = openList.OrderBy(node => node.FCost).First();

                openList.Remove(q);
                closedList.Add(q);


                var childrenLoc = FindMoveLocations(state, start, q.Location, stayClear);


                foreach (var loc in childrenLoc)
                {
                    gCost = q.GCost + 1;
                    hCost = Math.Abs(loc.X - target.X) + Math.Abs(loc.Y - target.Y);
                    fCost = gCost + hCost;

                    var newChild = new MapNode
                    {
                        Parent = q,
                        Location = loc,
                        GCost = gCost,
                        HCost = hCost,
                        FCost = fCost
                    };

                    if (loc.Equals(target))
                    {
                        return newChild;
                    }

                    var nodeInOpenList = openList.FirstOrDefault(node => (node.Location.Equals(loc)));

                    if (nodeInOpenList != null && nodeInOpenList.FCost < newChild.FCost)
                        continue;

                    var nodeInClosedList = closedList.FirstOrDefault(node => (node.Location.Equals(loc)));
                    if (nodeInClosedList != null && nodeInClosedList.FCost < newChild.FCost)
                        continue;

                    openList.Add(newChild);
                }
            }
            return null;
        }

        /// <summary>
        /// Reconstruct the path to target
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns>Next move Location towards target</returns>
        public static Location RecontractPath(Location start, MapNode goal)
        {
            if (goal == null) return null;

            var current = goal;
            while (!current.Parent.Location.Equals(start))
            {
                current = current.Parent;
            }
            return new Location(current.Location.X, current.Location.Y);
        }

        /// <summary>
        /// Find bombs endangering a bot
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns>List of bombs in LOS</returns>
        public static List<Bomb> FindBombsInLOS(GameState state, Location curLoc)
        {
            var bombs = new List<Bomb>();

            //Sitting on Bomb
            if (state.IsBomb(curLoc))
            {
                var bomb = state.GetBlock(curLoc).Bomb;
                bombs.Add(bomb);
            }

            //Continue to add others
            var openBlocks = new List<Location> { curLoc };

            while (openBlocks.Count != 0)
            {
                var qLoc = openBlocks.First();

                openBlocks.Remove(qLoc);

                var blocksLoc = ExpandBlocks(state, curLoc, qLoc);

                foreach (var bLoc in blocksLoc)
                {
                    if (state.IsBomb(bLoc))
                    {
                        var bomb = state.GetBlock(bLoc).Bomb;
                        var bombDistance = Math.Abs(curLoc.X - bLoc.X) + Math.Abs(curLoc.Y - bLoc.Y);
                        if (bomb.BombRadius > bombDistance - 1)
                        {
                            bombs.Add(bomb);
                        }
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return bombs.Count == 0 ? null : bombs.OrderBy(b => b.BombTimer).ToList();
        }

        /// <summary>
        /// Expand blocks in the direction of bombs
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>

        private static List<Location> ExpandBlocks(GameState state, Location curLoc, Location blockLoc)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }


                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
                else if (blockLoc.Y == curLoc.Y)
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
            }
            return blocksLoc;
        }

        /// <summary>
        /// Expand safe blocks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Location> FindSafeLocations(GameState state, Location curLoc)
        {
            Location loc;
            var safeBlocks = new List<Location>();

            loc = new Location(curLoc.X, curLoc.Y - 1);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }


            loc = new Location(curLoc.X + 1, curLoc.Y);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            loc = new Location(curLoc.X, curLoc.Y + 1);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            loc = new Location(curLoc.X - 1, curLoc.Y);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            return safeBlocks;
        }

        /// <summary>
        /// Destructible walls in LOS
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns></returns>
        public static List<DestructibleWall> FindWallsInLOS(GameState state, Location curLoc, Player player)
        {
            var walls = new List<DestructibleWall>();

            var openBlocks = new List<Location> { curLoc };

            while (openBlocks.Count != 0)
            {
                var qLoc = openBlocks.First();

                openBlocks.Remove(qLoc);

                var blocksLoc = ExpandPlantBlocks(state, curLoc, qLoc, player.BombRadius);

                foreach (var wLoc in blocksLoc)
                {
                    if (state.IsDestructibleWall(wLoc))
                    {
                        var wall = (DestructibleWall)state.GetBlock(wLoc).Entity;
                        walls.Add(wall);
                    }
                    else
                    {
                        openBlocks.Add(wLoc);
                    }
                }
            }
            return walls.Count == 0 ? null : walls;
        }

        /// <summary>
        /// Expand plant blocks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>
        private static List<Location> ExpandPlantBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }


                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X) + Math.Abs(curLoc.Y - loc.Y);
                        if (bombRadius > locDistance - 1)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
                else if (blockLoc.Y == curLoc.Y)
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X) + Math.Abs(curLoc.Y - loc.Y);
                        if (bombRadius > locDistance - 1)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
            }
            return blocksLoc;
        }
    }
}
