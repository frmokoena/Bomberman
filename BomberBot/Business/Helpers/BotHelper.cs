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
        public static MapNode FindPathToTarget(GameState state, Location startLoc, Location targetLoc, Player player, IEnumerable<Bomb> bombsToDodge = null, bool stayClear = false, bool super = false, bool hiding = false)
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

                var childrenLoc = super ? ExpandSuperBlocks(state, qMapNode.Location) : ExpandMoveBlocks(state, startLoc, qMapNode.Location, player, bombsToDodge, stayClear, hiding);

                for (var i = 0; i < childrenLoc.Count; i++)
                {
                    gCost = qMapNode.GCost + 1;
                    hCost = 2 * (Math.Abs(childrenLoc[i].X - targetLoc.X) + Math.Abs(childrenLoc[i].Y - targetLoc.Y));
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

        public static List<Location> ExpandMoveBlocks(GameState state, Location startLoc, Location curLoc, Player player, IEnumerable<Bomb> bombsToDodge = null, bool stayClear = false, bool hiding = false, bool bombCrossOver = true)
        {
            Location loc;
            var movesLoc = new List<Location>();

            if (stayClear || curLoc.Equals(startLoc) || hiding)
            {
                IEnumerable<Bomb> bombs;
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
                        else if (newBombs.Count() == 1 && newBombs.Any(b => player.IsBombOwner(b)))
                        {
                            // check the correct timer
                            var explodingBomb = newBombs.First();

                            var chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

                            bool addMoveLoc = true;

                            while (chainingBombs != null && addMoveLoc == true)
                            {
                                if (chainingBombs.Any(bomb => !player.IsBombOwner(bomb))) addMoveLoc = false;

                                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                                if (chainingBombs.Count() > 1) addMoveLoc = false;

                                if (chainingBombs.Count() > 0)
                                {
                                    explodingBomb = chainingBombs.First();
                                    chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                                }
                                else
                                {
                                    chainingBombs = null;
                                }
                            }

                            if (addMoveLoc && explodingBomb.BombTimer > 3)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                        else
                        {
                            var opponentClear = false;

                            foreach (var newBomb in newBombs)
                            {
                                var bombLocation = new Location(newBomb.Location.X - 1, newBomb.Location.Y - 1);

                                var chains = FindVisibleBombs(state, bombLocation, chaining: true);

                                if (chains != null && chains.Except(newBombs).Any())
                                {
                                    opponentClear = true;
                                    break;
                                }

                                if (player.IsBombOwner(newBomb))
                                {
                                    if (newBomb.BombTimer < 4)
                                    {
                                        opponentClear = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    var opponentLocation = state.GetPlayerLocation(newBomb.Owner.Key);
                                    if (opponentLocation != null)
                                    {
                                        var opponentVisibleBombs = FindVisibleBombs(state, opponentLocation);

                                        if (opponentVisibleBombs == null)
                                        {
                                            opponentClear = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!opponentClear)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                    }
                    else if (!(hiding || stayClear))
                    {
                        if (ShouldAddBlockLocation(state, player, loc, bombs))
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
                        else if (newBombs.Count() == 1 && newBombs.Any(b => player.IsBombOwner(b)))
                        {
                            // check the correct timer
                            var explodingBomb = newBombs.First();

                            var chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

                            bool addMoveLoc = true;

                            while (chainingBombs != null && addMoveLoc == true)
                            {
                                if (chainingBombs.Any(bomb => !player.IsBombOwner(bomb))) addMoveLoc = false;

                                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                                if (chainingBombs.Count() > 1) addMoveLoc = false;

                                if (chainingBombs.Count() > 0)
                                {
                                    explodingBomb = chainingBombs.First();
                                    chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                                }
                                else
                                {
                                    chainingBombs = null;
                                }
                            }

                            if (addMoveLoc && explodingBomb.BombTimer > 3)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                        else
                        {
                            var opponentClear = false;

                            foreach (var newBomb in newBombs)
                            {
                                var bombLocation = new Location(newBomb.Location.X - 1, newBomb.Location.Y - 1);

                                var chains = FindVisibleBombs(state, bombLocation, chaining: true);

                                if (chains != null && chains.Except(newBombs).Any())
                                {
                                    opponentClear = true;
                                    break;
                                }

                                if (player.IsBombOwner(newBomb))
                                {
                                    if (newBomb.BombTimer < 4)
                                    {
                                        opponentClear = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    var opponentLocation = state.GetPlayerLocation(newBomb.Owner.Key);
                                    if (opponentLocation != null)
                                    {
                                        var opponentVisibleBombs = FindVisibleBombs(state, opponentLocation);

                                        if (opponentVisibleBombs == null)
                                        {
                                            opponentClear = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!opponentClear)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                    }
                    else if (!(hiding || stayClear))
                    {
                        if (ShouldAddBlockLocation(state, player, loc, bombs))
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
                        else if (newBombs.Count() == 1 && newBombs.Any(b => player.IsBombOwner(b)))
                        {
                            // check the correct timer
                            var explodingBomb = newBombs.First();

                            var chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

                            bool addMoveLoc = true;

                            while (chainingBombs != null && addMoveLoc == true)
                            {
                                if (chainingBombs.Any(bomb => !player.IsBombOwner(bomb))) addMoveLoc = false;

                                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                                if (chainingBombs.Count() > 1) addMoveLoc = false;

                                if (chainingBombs.Count() > 0)
                                {
                                    explodingBomb = chainingBombs.First();
                                    chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                                }
                                else
                                {
                                    chainingBombs = null;
                                }
                            }

                            if (addMoveLoc && explodingBomb.BombTimer > 3)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                        else
                        {
                            var opponentClear = false;

                            foreach (var newBomb in newBombs)
                            {
                                var bombLocation = new Location(newBomb.Location.X - 1, newBomb.Location.Y - 1);

                                var chains = FindVisibleBombs(state, bombLocation, chaining: true);

                                if (chains != null && chains.Except(newBombs).Any())
                                {
                                    opponentClear = true;
                                    break;
                                }

                                if (player.IsBombOwner(newBomb))
                                {
                                    if (newBomb.BombTimer < 4)
                                    {
                                        opponentClear = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    var opponentLocation = state.GetPlayerLocation(newBomb.Owner.Key);
                                    if (opponentLocation != null)
                                    {
                                        var opponentVisibleBombs = FindVisibleBombs(state, opponentLocation);

                                        if (opponentVisibleBombs == null)
                                        {
                                            opponentClear = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!opponentClear)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                    }
                    else if (!(hiding || stayClear))
                    {
                        if (ShouldAddBlockLocation(state, player, loc, bombs))
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
                        else if (newBombs.Count() == 1 && newBombs.Any(b => player.IsBombOwner(b)))
                        {
                            // check the correct timer
                            var explodingBomb = newBombs.First();

                            var chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

                            bool addMoveLoc = true;

                            while (chainingBombs != null && addMoveLoc == true)
                            {
                                if (chainingBombs.Any(bomb => !player.IsBombOwner(bomb))) addMoveLoc = false;

                                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                                if (chainingBombs.Count() > 1) addMoveLoc = false;

                                if (chainingBombs.Count() > 0)
                                {
                                    explodingBomb = chainingBombs.First();
                                    chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                                }
                                else
                                {
                                    chainingBombs = null;
                                }
                            }

                            if (addMoveLoc && explodingBomb.BombTimer > 3)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                        else
                        {
                            var opponentClear = false;

                            foreach (var newBomb in newBombs)
                            {
                                var bombLocation = new Location(newBomb.Location.X - 1, newBomb.Location.Y - 1);

                                var chains = FindVisibleBombs(state, bombLocation, chaining: true);

                                if (chains != null && chains.Except(newBombs).Any())
                                {
                                    opponentClear = true;
                                    break;
                                }

                                if (player.IsBombOwner(newBomb))
                                {
                                    if (newBomb.BombTimer < 4)
                                    {
                                        opponentClear = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    var opponentLocation = state.GetPlayerLocation(newBomb.Owner.Key);
                                    if (opponentLocation != null)
                                    {
                                        var opponentVisibleBombs = FindVisibleBombs(state, opponentLocation);

                                        if (opponentVisibleBombs == null)
                                        {
                                            opponentClear = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!opponentClear)
                            {
                                movesLoc.Add(loc);
                            }
                        }
                    }
                    else if (!(hiding || stayClear))
                    {
                        if (ShouldAddBlockLocation(state, player, loc, bombs))
                        {
                            movesLoc.Add(loc);
                        }
                    }
                }
            }
            else
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }
            }
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


        public static HashSet<Location> FindAllBlastLocations(GameState state, Location startLoc, int bombRadius)
        {
            var openBlocks = new List<Location> { startLoc };

            var blastBlocks = new HashSet<Location>();

            Location qLoc;

            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];
                openBlocks.RemoveAt(0);
                blocksLoc = ExpandPlayerBlocks(state, startLoc, qLoc, bombRadius);

                blastBlocks.UnionWith(blocksLoc);
                openBlocks.AddRange(blocksLoc);
            }
            return blastBlocks.Count == 0 ? null : blastBlocks;
        }

        public static IEnumerable<Bomb> FindVisibleBombs(GameState state, Location curLoc, bool chaining = false)
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
            return visibleBombs.Count == 0 ? null : visibleBombs;
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

        public static IEnumerable<Player> FindVisiblePlayers(GameState state, Location startLoc, string playerKey, int bombRadius)
        {
            var openBlocks = new List<Location> { startLoc };

            var visiblePlayers = new List<Player>();
            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks[0];

                openBlocks.RemoveAt(0);

                blocksLoc = ExpandPlayerBlocks(state, startLoc, qLoc, bombRadius);

                foreach (var bLoc in blocksLoc)
                {
                    var entity = state.GetBlockAtLocation(bLoc).Entity;

                    if (entity is Player)
                    {
                        var opponent = (Player)entity;

                        if (opponent.Key != playerKey)
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

        internal static List<Location> ExpandBlocksForPlayer(GameState state, Location curLoc)
        {
            Location loc;
            var playerBlocksLoc = new List<Location>();

            loc = new Location(curLoc.X, curLoc.Y - 1);

            if (state.IsBlockPlayerClear(loc))
            {
                playerBlocksLoc.Add(loc);
            }

            loc = new Location(curLoc.X + 1, curLoc.Y);

            if (state.IsBlockPlayerClear(loc))
            {
                playerBlocksLoc.Add(loc);
            }

            loc = new Location(curLoc.X, curLoc.Y + 1);

            if (state.IsBlockPlayerClear(loc))
            {
                playerBlocksLoc.Add(loc);
            }

            loc = new Location(curLoc.X - 1, curLoc.Y);

            if (state.IsBlockPlayerClear(loc))
            {
                playerBlocksLoc.Add(loc);
            }

            return playerBlocksLoc;
        }

        private static bool ShouldAddBlockLocation(GameState state, Player player, Location loc, IEnumerable<Bomb> bombs)
        {
            if (bombs.Count() > 1) return false;

            var explodingBomb = bombs.First();

            var owner = explodingBomb.Owner;


            var ownerLocation = new Location(owner.Location.X - 1, owner.Location.Y - 1);

            var opVisibleBombs = FindVisibleBombs(state, ownerLocation);

            if (owner.Key != player.Key)
            {
                if (opVisibleBombs == null) return false;
            }

            var chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

            while (chainingBombs != null)
            {
                if (chainingBombs.Any(bomb => !owner.IsBombOwner(bomb))) return false;

                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                if (chainingBombs.Count() > 1) return false;

                if (chainingBombs.Count() > 0)
                {
                    explodingBomb = chainingBombs.First();
                    chainingBombs = FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                }
                else
                {
                    chainingBombs = null;
                }
            }

            var myLocation = new Location(player.Location.X - 1, player.Location.Y - 1);
            var mySafeBlock = FindSafeBlocks(state, player, loc, bombs, firstSafeBlock: true);

            if (owner.Key == player.Key)
            {
                if (mySafeBlock != null && mySafeBlock.First().Distance < explodingBomb.BombTimer - 1)
                {
                    return true;
                }
            }
            else
            {
                var opSafeBlock = FindSafeBlocks(state, owner, ownerLocation, opVisibleBombs, firstSafeBlock: true);

                if (mySafeBlock != null)
                {
                    if (opSafeBlock == null)
                    {
                        if (mySafeBlock.First().Distance < explodingBomb.BombTimer - 1)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (mySafeBlock.First().Distance < explodingBomb.BombTimer - 1 && mySafeBlock.First().Distance < opSafeBlock.First().Distance + 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static IEnumerable<MapSafeBlock> FindSafeBlocks(GameState state, Player player, Location startLoc, IEnumerable<Bomb> bombsToDodge, bool firstSafeBlock = false)
        {
            var safeBlocks = new List<MapSafeBlock>();
            var bomb = bombsToDodge.OrderByDescending(b => b.BombTimer)
                                   .First();

            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } }; //To be expanded
            var closedSet = new HashSet<MapNode>();          // Expanded and visited

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                MapNode safeNode = FindPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

                //if we can reach this location, and in time

                if (safeNode != null && safeNode.FCost < bomb.BombTimer)
                {
                    var visibleBombs = FindVisibleBombs(state, qNode.Location);

                    if (visibleBombs == null)
                    {
                        MapSafeBlock mapBlock;
                        if (firstSafeBlock)
                        {
                            //add block
                            mapBlock = new MapSafeBlock
                            {
                                Location = qNode.Location,
                                Distance = safeNode.FCost
                            };

                            safeBlocks.Add(mapBlock);
                            return safeBlocks;
                        }


                        var visibleWalls = FindVisibleWalls(state, qNode.Location, player);

                        var nearByPowerUp = FindNearByMapPowerUpBlock(state, qNode.Location, player.Key);

                        var blockProbability = FindBlockProbability(state, qNode.Location, safeNode.FCost, player.Key);

                        //add block
                        mapBlock = new MapSafeBlock
                        {
                            Location = qNode.Location,
                            Distance = safeNode.FCost,
                            LocationToBlock = ReconstructPath(safeNode),
                            VisibleWalls = visibleWalls == null ? 0 : visibleWalls.Count,
                            PowerDistance = nearByPowerUp == null ? int.MaxValue : nearByPowerUp.Distance,
                            SuperDistance = state.SuperLocation == null ? 0 : state.SuperLocation == null ? 0 : FindPathToTarget(state, qNode.Location, state.SuperLocation, player, super: true).FCost,
                            MapNode = safeNode,
                            Probability = blockProbability
                        };
                        safeBlocks.Add(mapBlock);
                    }

                    var possibleBlocksLoc = ExpandMoveBlocks(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

                    for (var i = 0; i < possibleBlocksLoc.Count; i++)
                    {
                        var nodeInOpenList = openSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInOpenList != null) continue;

                        var nodeInClosedList = closedSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInClosedList != null) continue;

                        var newNode = new MapNode
                        {
                            Location = possibleBlocksLoc[i],
                            GCost = qNode.GCost + 1
                        };

                        openSet.Add(newNode);
                    }
                }
            }
            return safeBlocks.Count == 0 ? null : safeBlocks.OrderByDescending(block => block.Probability)
                                                            .ThenBy(block => block.Distance)
                                                            .ThenByDescending(block => block.VisibleWalls)
                                                            .ThenBy(Block => Block.SuperDistance)
                                                            .ThenBy(block => block.PowerDistance);
        }

        private static int FindBlockProbability(GameState state, Location blockLoc, int blockDistance, string playerKey)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = blockLoc } };
            var closedSet = new HashSet<MapNode>();

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(n => n.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                if (qNode.GCost <= blockDistance)
                {
                    var entity = state.GetBlockAtLocation(qNode.Location).Entity;

                    if (entity is Player)
                    {
                        var opponent = (Player)entity;
                        if (opponent.Key != playerKey)
                        {
                            var opLocation = new Location(opponent.Location.X - 1, opponent.Location.Y - 1);
                            var opVisibleBombs = FindVisibleBombs(state, opLocation);
                            if (opVisibleBombs == null) return 0;
                        }
                    }

                    //expand
                    var possibleBlocksLoc = ExpandBlocksForPlayer(state, qNode.Location);

                    for (var i = 0; i < possibleBlocksLoc.Count; i++)
                    {
                        var nodeInOpenList = openSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInOpenList != null) continue;

                        var nodeInClosedList = closedSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInClosedList != null) continue;

                        var newNode = new MapNode
                        {
                            Location = possibleBlocksLoc[i],
                            GCost = qNode.GCost + 1
                        };

                        openSet.Add(newNode);
                    }
                }
            }
            return 1;
        }

        public static MapPowerUpBlock FindNearByMapPowerUpBlock(GameState state, Location startLoc, string playerKey)
        {

            var opponents = state.Players.Where(p => (p.Key != playerKey && !p.Killed));
            var player = state.Players.Find(p => p.Key == playerKey);
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(n => n.GCost).First();

                var mapEntity = state.GetBlockAtLocation(qNode.Location).PowerUp;

                if (mapEntity != null)
                {
                    var mapNode = FindPathToTarget(state, startLoc, qNode.Location, player);

                    if (mapNode != null)
                    {
                        var foundPowerUpBlock = true;

                        foreach (var p in opponents)
                        {
                            var playerLoc = new Location(p.Location.X - 1, p.Location.Y - 1);

                            if (IsBlockInPlayerRange(state, playerLoc, p, qNode.Location, mapNode.FCost))
                            {
                                foundPowerUpBlock = false;
                                break;
                            }
                        }

                        if (foundPowerUpBlock)
                        {
                            return new MapPowerUpBlock
                            {
                                Location = qNode.Location,
                                Distance = mapNode.FCost,
                                LocationToBlock = ReconstructPath(mapNode),
                                PowerUP = mapEntity
                            };
                        }
                    }
                }


                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var possibleBlocksLoc = ExpandMoveBlocks(state, startLoc, qNode.Location, player);

                for (var i = 0; i < possibleBlocksLoc.Count; i++)
                {
                    var nodeInOpenList = openSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                    if (nodeInOpenList != null) continue;

                    var nodeInClosedList = closedSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                    if (nodeInClosedList != null) continue;

                    var newNode = new MapNode
                    {
                        Location = possibleBlocksLoc[i],
                        GCost = qNode.GCost + 1
                    };

                    openSet.Add(newNode);
                }
            }
            return null;
        }

        private static bool IsBlockInPlayerRange(GameState state, Location startLoc, Player player, Location targetLoc, int range)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            while (openSet.Count != 0)
            {

                var qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var blockNode = FindPathToTarget(state, startLoc, qNode.Location, player);

                if (blockNode != null && blockNode.FCost < range)
                {
                    if (qNode.Location.Equals(targetLoc))
                    {
                        return true;
                    }

                    if (state.IsPowerUp(qNode.Location))
                    {
                        return false;
                    }

                    var possibleBlocksLoc = ExpandMoveBlocks(state, startLoc, qNode.Location, player);

                    for (var i = 0; i < possibleBlocksLoc.Count; i++)
                    {
                        var nodeInOpenList = openSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInOpenList != null) continue;

                        var nodeInClosedList = closedSet.FirstOrDefault(node => (node.Location.Equals(possibleBlocksLoc[i])));

                        if (nodeInClosedList != null) continue;

                        var newNode = new MapNode
                        {
                            Location = possibleBlocksLoc[i],
                            GCost = qNode.GCost + 1
                        };
                        openSet.Add(newNode);
                    }
                }
            }
            return false;
        }
    }
}