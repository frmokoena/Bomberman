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
        public static MapNode BuildPathToTarget(GameState state, Location startLoc, Location targetLoc, Player player = null, List<Bomb> bombsToDodge = null, bool stayClear = false, bool super = false)
        {
            var openList = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedList = new HashSet<MapNode>();

            int gCost, hCost, fCost;
            MapNode qMapNode;

            while (openList.Count != 0)
            {
                qMapNode = openList.OrderBy(node => node.FCost).First();

                if (qMapNode.Location.Equals(targetLoc))
                {
                    return qMapNode;
                }

                openList.Remove(qMapNode);
                closedList.Add(qMapNode);
                
                var childrenLoc = super ? ExpandSuperBlocks(state, qMapNode.Location) : ExpandMoveBlocks(state, startLoc, qMapNode.Location, player, bombsToDodge, stayClear);
                
                for (var i = 0; i < childrenLoc.Count; i++)
                {
                    gCost = qMapNode.GCost + 1;
                    hCost = 2*(Math.Abs(childrenLoc[i].X - targetLoc.X) + Math.Abs(childrenLoc[i].Y - targetLoc.Y));
                    fCost = gCost + hCost;

                    var newNode = new MapNode
                    {
                        Parent = qMapNode,
                        Location = childrenLoc[i],
                        GCost = gCost,
                        HCost = hCost,
                        FCost = fCost
                    };

                    var nodeInOpenList = openList.FirstOrDefault(node => (node.Location.Equals(childrenLoc[i])));

                    if (nodeInOpenList != null && nodeInOpenList.FCost < newNode.FCost)
                        continue;

                    var nodeInClosedList = closedList.FirstOrDefault(node => (node.Location.Equals(childrenLoc[i])));
                    if (nodeInClosedList != null && nodeInClosedList.FCost < newNode.FCost)
                        continue;

                    openList.Add(newNode);
                }
            }
            return null;
        }

        public static Location ReconstructPath(MapNode goalMapNode)
        {
            if (goalMapNode == null) return null;

            if (goalMapNode.Parent == null) return goalMapNode.Location;

            var currentMapNode = goalMapNode;

            while (currentMapNode.Parent.Parent != null)
            {
                currentMapNode = currentMapNode.Parent;
            }
            return currentMapNode.Location;
        }

        public static List<Location> ExpandMoveBlocks(GameState state, Location startLoc, Location curLoc, Player player = null, List<Bomb> bombsToDodge = null, bool stayClear = false)
        {
            Location loc;
            var movesLoc = new List<Location>();

            //if (curLoc.Equals(startLoc))
            //{
            List<Bomb> bombs;
            loc = new Location(curLoc.X, curLoc.Y - 1);

            if (state.IsBlockClear(loc))
            {
                bombs = FindVisibleBombs(state, loc);

                if (bombs == null)
                {
                    movesLoc.Add(loc);
                }
                else if (stayClear)
                {
                    var newBombs = bombs.Except(bombsToDodge);

                    if (!newBombs.Any())
                    {
                        movesLoc.Add(loc);
                    }
                    else if (newBombs.Count() == 1
                            && newBombs.Any(b => player.IsBombOwner(b))
                            && newBombs.First().BombTimer > 2)
                    {
                        movesLoc.Add(loc);
                    }
                }
            }

            loc = new Location(curLoc.X + 1, curLoc.Y);

            if (state.IsBlockClear(loc))
            {
                bombs = FindVisibleBombs(state, loc);

                if (bombs == null)
                {
                    movesLoc.Add(loc);
                }
                else if (stayClear)
                {
                    var newBombs = bombs.Except(bombsToDodge);

                    if (!newBombs.Any())
                    {
                        movesLoc.Add(loc);
                    }
                    else if (newBombs.Count() == 1
                            && newBombs.Any(b => player.IsBombOwner(b))
                            && newBombs.First().BombTimer > 2)
                    {
                        movesLoc.Add(loc);
                    }
                }
            }

            loc = new Location(curLoc.X, curLoc.Y + 1);

            if (state.IsBlockClear(loc))
            {
                bombs = FindVisibleBombs(state, loc);

                if (bombs == null)
                {
                    movesLoc.Add(loc);
                }
                else if (stayClear)
                {
                    var newBombs = bombs.Except(bombsToDodge);

                    if (!newBombs.Any())
                    {
                        movesLoc.Add(loc);
                    }
                    else if (newBombs.Count() == 1
                            && newBombs.Any(b => player.IsBombOwner(b))
                            && newBombs.First().BombTimer > 2)
                    {
                        movesLoc.Add(loc);
                    }
                }
            }

            loc = new Location(curLoc.X - 1, curLoc.Y);

            if (state.IsBlockClear(loc))
            {
                bombs = FindVisibleBombs(state, loc);

                if (bombs == null)
                {
                    movesLoc.Add(loc);
                }
                else if (stayClear)
                {
                    var newBombs = bombs.Except(bombsToDodge);

                    if (!newBombs.Any())
                    {
                        movesLoc.Add(loc);
                    }
                    else if (newBombs.Count() == 1
                            && newBombs.Any(b => player.IsBombOwner(b))
                            && newBombs.First().BombTimer > 2)
                    {
                        movesLoc.Add(loc);
                    }
                }
            }
            //}
            //else
            //{
            //    loc = new Location(curLoc.X, curLoc.Y - 1);

            //    if (state.IsBlockClear(loc))
            //    {
            //        movesLoc.Add(loc);
            //    }

            //    loc = new Location(curLoc.X + 1, curLoc.Y);

            //    if (state.IsBlockClear(loc))
            //    {
            //        movesLoc.Add(loc);
            //    }

            //    loc = new Location(curLoc.X, curLoc.Y + 1);

            //    if (state.IsBlockClear(loc))
            //    {
            //        movesLoc.Add(loc);
            //    }

            //    loc = new Location(curLoc.X - 1, curLoc.Y);

            //    if (state.IsBlockClear(loc))
            //    {
            //        movesLoc.Add(loc);
            //    }
            //}
            return movesLoc;
        }


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
                    var entity = state.GetBlockAtLocation(bLoc).Entity;

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

        private static List<Location> ExpandPlayerBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (state.IsBlockPlayerClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (state.IsBlockPlayerClear(loc))
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

                    if (state.IsBlockPlayerClear(loc))
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

        public static List<Bomb> FindVisibleBombs(GameState state, Location curLoc, bool chaining = false)
        {
            var visibleBombs = new List<Bomb>();

            //Sitting on Bomb
            if (!chaining && state.IsBomb(curLoc))
            {
                var bomb = state.GetBlockAtLocation(curLoc).Bomb;
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
                        var bomb = state.GetBlockAtLocation(bLoc).Bomb;
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

        private static List<Location> ExpandBombBlocks(GameState state, Location curLoc, Location blockLoc)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
                else
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
            }
            return blocksLoc;
        }

        //public static List<Location> ExpandSafeBlocks(GameState state, Location startLoc, Location curLoc, List<Bomb> bombsToDodge)
        //{
        //    Location loc;
        //    var safeBlocks = new List<Location>();

        //    if (curLoc.Equals(startLoc))
        //    {
        //        List<Bomb> bombs;
        //        loc = new Location(curLoc.X, curLoc.Y - 1);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            bombs = FindVisibleBombs(state, loc);

        //            if (bombs == null)
        //            {
        //                safeBlocks.Add(loc);
        //            }
        //            else
        //            {
        //                var anyNewBomb = bombs.Except(bombsToDodge).Any();

        //                if (!anyNewBomb)
        //                {
        //                    safeBlocks.Add(loc);
        //                }
        //            }
        //        }

        //        loc = new Location(curLoc.X + 1, curLoc.Y);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            bombs = FindVisibleBombs(state, loc);

        //            if (bombs == null)
        //            {
        //                safeBlocks.Add(loc);
        //            }
        //            else
        //            {
        //                var anyNewBomb = bombs.Except(bombsToDodge).Any();

        //                if (!anyNewBomb)
        //                {
        //                    safeBlocks.Add(loc);
        //                }
        //            }
        //        }

        //        loc = new Location(curLoc.X, curLoc.Y + 1);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            bombs = FindVisibleBombs(state, loc);

        //            if (bombs == null)
        //            {
        //                safeBlocks.Add(loc);
        //            }
        //            else
        //            {
        //                var anyNewBomb = bombs.Except(bombsToDodge).Any();

        //                if (!anyNewBomb)
        //                {
        //                    safeBlocks.Add(loc);
        //                }
        //            }
        //        }

        //        loc = new Location(curLoc.X - 1, curLoc.Y);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            bombs = FindVisibleBombs(state, loc);

        //            if (bombs == null)
        //            {
        //                safeBlocks.Add(loc);
        //            }
        //            else
        //            {
        //                var anyNewBomb = bombs.Except(bombsToDodge).Any();

        //                if (!anyNewBomb)
        //                {
        //                    safeBlocks.Add(loc);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        loc = new Location(curLoc.X, curLoc.Y - 1);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            safeBlocks.Add(loc);
        //        }

        //        loc = new Location(curLoc.X + 1, curLoc.Y);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            safeBlocks.Add(loc);
        //        }

        //        loc = new Location(curLoc.X, curLoc.Y + 1);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            safeBlocks.Add(loc);
        //        }

        //        loc = new Location(curLoc.X - 1, curLoc.Y);

        //        if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
        //        {
        //            safeBlocks.Add(loc);
        //        }
        //    }
        //    return safeBlocks;
        //}

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
                        var visibleBombs = FindVisibleBombs(state, wLoc);

                        if (visibleBombs == null)
                        {
                            var wall = (DestructibleWall)state.GetBlockAtLocation(wLoc).Entity;
                            visibleWalls.Add(wall);
                        }
                    }
                    else
                    {
                        openBlocks.Add(wLoc);
                    }
                }
            }
            return visibleWalls.Count == 0 ? null : visibleWalls;
        }

        private static List<Location> ExpandWallBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (state.IsBlockPlantClear(loc))
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

                    if (state.IsBlockPlantClear(loc))
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
                    var entity = state.GetBlockAtLocation(bLoc).Entity;

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

        public static List<Player> FindVisiblePlayers(GameState state, Player player, Location startLoc)
        {
            var openBlocks = new List<Location> { startLoc };

            var visiblePlayers = new List<Player>();
            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                blocksLoc = ExpandPlayerBlocks(state, startLoc, qLoc, player.BombRadius);

                foreach (var bLoc in blocksLoc)
                {
                    var entity = state.GetBlockAtLocation(bLoc).Entity;

                    if (entity is Player)
                    {
                        var opponent = (Player)entity;

                        if (opponent.Key != player.Key)
                        {
                            visiblePlayers.Add(opponent);
                        }

                        // player doesn't block a bomb
                        openBlocks.Add(bLoc);
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return visiblePlayers.Count == 0 ? null : visiblePlayers;
        }

        public static List<Location> ExpandSuperBlocks(GameState state, Location curLoc)
        {
            Location loc;
            var superMovesLoc = new List<Location>();

            loc = new Location(curLoc.X, curLoc.Y - 1);

            if (state.IsBlockSuperClear(loc))
            {
                superMovesLoc.Add(loc);
            }

            loc = new Location(curLoc.X + 1, curLoc.Y);

            if (state.IsBlockSuperClear(loc))
            {
                superMovesLoc.Add(loc);
            }

            loc = new Location(curLoc.X, curLoc.Y + 1);

            if (state.IsBlockSuperClear(loc))
            {
                superMovesLoc.Add(loc);
            }

            loc = new Location(curLoc.X - 1, curLoc.Y);

            if (state.IsBlockSuperClear(loc))
            {
                superMovesLoc.Add(loc);
            }

            return superMovesLoc;
        }

        public static List<Location> FindPlayerBombTargetBlocks(GameState state, Player player, Location startLoc)
        {
            return null;
        }
    }
}