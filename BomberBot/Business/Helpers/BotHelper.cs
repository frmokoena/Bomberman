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
            return (loc.X > 0 && loc.X < state.MapWidth - 1)
                && (loc.Y > 0 && loc.Y < state.MapHeight - 1);
        }


        /// <summary>
        /// Shortest route to target
        /// </summary>
        /// <param name="startLoc"></param>
        /// <param name="targetLoc"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static MapNode BuildPathToTarget(GameState state, Location startLoc, Location targetLoc, bool stayClear = false)
        {
            var openList = new List<MapNode> { new MapNode { Location = startLoc } };
            var closedList = new List<MapNode>();

            int gCost, hCost, fCost;
            MapNode qMapNode;

            while (openList.Count != 0)
            {
                openList = openList.OrderBy(node => node.FCost).ToList();

                qMapNode = openList[0];

                if (qMapNode.Location.Equals(targetLoc))
                {
                    return qMapNode;
                }

                openList.RemoveAt(0);
                closedList.Add(qMapNode);


                var childrenLoc = ExpandMoveBlocks(state, startLoc, qMapNode.Location, stayClear);


                foreach (var loc in childrenLoc)
                {
                    gCost = qMapNode.GCost + 1;
                    hCost = Math.Abs(loc.X - targetLoc.X) + Math.Abs(loc.Y - targetLoc.Y);
                    fCost = gCost + hCost;

                    var newChild = new MapNode
                    {
                        Parent = qMapNode,
                        Location = loc,
                        GCost = gCost,
                        HCost = hCost,
                        FCost = fCost
                    };

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
        /// <param name="startLoc"></param>
        /// <param name="goalMapNode"></param>
        /// <returns>Next move Location towards target</returns>
        public static Location RecontractPath(MapNode goalMapNode)
        {
            if (goalMapNode == null) return null;

            if (goalMapNode.Parent == null) return goalMapNode.Location;

            //if (goalMapNode.Location.Equals(startLoc)) return startLoc;

            var currentMapNode = goalMapNode;
            //while (!currentMapNode.Parent.Location.Equals(startLoc))
            while (currentMapNode.Parent.Parent != null)
            {
                currentMapNode = currentMapNode.Parent;
            }
            return currentMapNode.Location;
        }

        /// <summary>
        /// Possible move Locations
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startLoc"></param>
        /// <param name="curLoc"></param>
        /// <returns>next posssible move Locations</returns>
        public static List<Location> ExpandMoveBlocks(GameState state, Location startLoc, Location curLoc, bool stayClear = false)
        {
            Location loc;
            var movesLoc = new List<Location>();

            if (curLoc.Equals(startLoc))
            {
                List<Bomb> bombs;
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        movesLoc.Add(loc);
                    }
                    else
                    {
                        if (stayClear && bombs[0].BombTimer > 1)
                        {
                            movesLoc.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        movesLoc.Add(loc);
                    }
                    else
                    {
                        if (stayClear && bombs[0].BombTimer > 1)
                        {
                            movesLoc.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        movesLoc.Add(loc);
                    }
                    else
                    {
                        if (stayClear && bombs[0].BombTimer > 1)
                        {
                            movesLoc.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        movesLoc.Add(loc);
                    }
                    else
                    {
                        if (stayClear && bombs[0].BombTimer > 1)
                        {
                            movesLoc.Add(loc);
                        }
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
        /// Find if is any player to kill
        /// </summary>
        /// <param name="state"></param>
        /// <param name="bomb"></param>
        /// <returns></returns>
        public static bool IsAnyPlayerVisible(GameState state, Bomb bomb)
        {
            var curLoc = new Location(bomb.Location.X - 1, bomb.Location.Y - 1);
            var openBlocks = new List<Location> { curLoc };

            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                blocksLoc = ExpandPlayerBlocks(state, curLoc, qLoc, bomb.BombRadius);

                foreach (var bLoc in blocksLoc)
                {
                    var entity = state.GetBlock(bLoc).Entity;

                    if (entity is Player)
                    {
                        var player = (Player)entity;

                        if (player.Key != bomb.Owner.Key) return true;
                        openBlocks.Add(bLoc);
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Expand blocks to discover players
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <param name="bombRadius"></param>
        /// <returns></returns>
        private static List<Location> ExpandPlayerBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.Y - loc.Y);
                        if (locDistance <= bombRadius)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
                else
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockPlayerClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X);
                        if (locDistance <= bombRadius)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
            }
            return blocksLoc;
        }



        /// <summary>
        /// Find bombs endangering a bot
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns>List of bombs in LOS</returns>
        public static List<Bomb> FindVisibleBombs(GameState state, Location curLoc, bool chaining = false)
        {
            var visibleBombs = new List<Bomb>();

            //Sitting on Bomb
            if (state.IsBomb(curLoc) && !chaining)
            {
                var bomb = state.GetBlock(curLoc).Bomb;
                visibleBombs.Add(bomb);
            }

            //Continue to add others
            var openBlocks = new List<Location> { curLoc };
            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                blocksLoc = ExpandBombBlocks(state, curLoc, qLoc);

                foreach (var bLoc in blocksLoc)
                {
                    if (state.IsBomb(bLoc))
                    {
                        var bomb = state.GetBlock(bLoc).Bomb;
                        var bombDistance = Math.Abs(curLoc.X - bLoc.X) + Math.Abs(curLoc.Y - bLoc.Y);
                        if (bomb.BombRadius > bombDistance - 1)
                        {
                            visibleBombs.Add(bomb);
                        }
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return visibleBombs.Count == 0 ? null : visibleBombs.OrderBy(b => b.BombTimer).ToList();
        }

        /// <summary>
        /// Expand blocks in the direction of bombs
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>

        private static List<Location> ExpandBombBlocks(GameState state, Location curLoc, Location blockLoc)
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
                else
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
        /// Expand safe bloks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startLoc"></param>
        /// <param name="curLoc"></param>
        /// <returns></returns>
        public static List<Location> ExpandSafeBlocks(GameState state, Location startLoc, Location curLoc)
        {
            Location loc;
            var safeBlocks = new List<Location>();

            if (curLoc.Equals(startLoc))
            {
                List<Bomb> bombs;
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        safeBlocks.Add(loc);
                    }
                    else
                    {
                        if (bombs[0].BombTimer > 1)
                        {
                            safeBlocks.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        safeBlocks.Add(loc);
                    }
                    else
                    {
                        if (bombs[0].BombTimer > 1)
                        {
                            safeBlocks.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        safeBlocks.Add(loc);
                    }
                    else
                    {
                        if (bombs[0].BombTimer > 1)
                        {
                            safeBlocks.Add(loc);
                        }
                    }
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindVisibleBombs(state, loc);

                    if (bombs == null)
                    {
                        safeBlocks.Add(loc);
                    }
                    else
                    {
                        if (bombs[0].BombTimer > 1)
                        {
                            safeBlocks.Add(loc);
                        }
                    }
                }
            }
            else
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    safeBlocks.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    safeBlocks.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    safeBlocks.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    safeBlocks.Add(loc);
                }
            }
            return safeBlocks;
        }



        /// <summary>
        /// Destructible walls in LOS
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns></returns>
        public static List<DestructibleWall> FindVisibleWalls(GameState state, Location curLoc, Player player)
        {
            var visibleWalls = new List<DestructibleWall>();

            var openBlocks = new List<Location> { curLoc };
            Location qLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                var blocksLoc = ExpandWallBlocks(state, curLoc, qLoc, player.BombRadius);

                foreach (var wLoc in blocksLoc)
                {
                    if (state.IsDestructibleWall(wLoc))
                    {
                        var wall = (DestructibleWall)state.GetBlock(wLoc).Entity;
                        visibleWalls.Add(wall);
                    }
                    else
                    {
                        openBlocks.Add(wLoc);
                    }
                }
            }
            return visibleWalls.Count == 0 ? null : visibleWalls;
        }

        /// <summary>
        /// Expand plant blocks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>
        private static List<Location> ExpandWallBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
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
                        var locDistance = Math.Abs(curLoc.Y - loc.Y);
                        if (locDistance <= bombRadius)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
                else
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X);
                        if (locDistance <= bombRadius)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
            }
            return blocksLoc;
        }

        public static bool IsAnyPlayerVisible(GameState state, Player player, Location startLoc)
        {            
            var openBlocks = new List<Location> { startLoc };

            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                blocksLoc = ExpandPlayerBlocks(state, startLoc, qLoc, player.BombRadius);

                foreach (var bLoc in blocksLoc)
                {
                    var entity = state.GetBlock(bLoc).Entity;

                    if (entity is Player)
                    {
                        var opponent = (Player)entity;

                        if (opponent.Key != player.Key) return true;
                        openBlocks.Add(bLoc);
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return false;
        }
    }
}