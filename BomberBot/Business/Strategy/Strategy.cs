using BomberBot.Business.Helpers;
using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using BomberBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BomberBot.Business.Strategy
{
    public class Strategy : IStrategy
    {
        protected readonly IGameService<GameState> GameService;
        private Move _move { get; set; }
        private MapPowerUpBlock _nearByPowerUp { get; set; }
        private bool _anyBombVisible { get; set; }

        public Strategy(IGameService<GameState> gameServie)
        {
            GameService = gameServie;
        }

        private GameState GameState
        {
            get
            {
                return GameService.GameState;
            }
        }
        private string MyKey
        {
            get
            {
                return GameService.HomeKey;
            }
        }
        private Player MyPlayer
        {
            get
            {
                return GameState.GetPlayer(MyKey);
            }
        }
        private Location MyLocation
        {
            get
            {
                return GameState.GetPlayerLocation(MyKey);
            }
        }

        private bool WallsInMap
        {
            get
            {
                return GameState.WallsLeft != 0;
            }
        }

        private bool BlocksToExploreInMap
        {
            get
            {
                return GameService.BlocksToExplore.Count != 0;
            }
        }

        public void Execute()
        {
            //Player killed
            if (MyLocation == null) return;

            //Stay Clear of Bombs
            if (StayClearOfBombs(GameState, MyPlayer, MyLocation, MyKey))
            {
                GameService.WriteMove(_move);
                return;
            }

            // Trigger bomb
            if (TriggerBomb(GameState, MyPlayer, MyLocation, MyKey))
            {
                GameService.WriteMove(_move);
                return;
            }

            // immediate chase power
            if (PriorityChasePower(GameState, MyPlayer, MyLocation, MyKey))
            {
                GameService.WriteMove(_move);
                return;
            }

            // place bomb
            if (PlaceBomb(GameState, MyPlayer, MyLocation, MyKey))
            {
                GameService.WriteMove(_move);
                return;
            }


            // Chase power up
            if (ChasePower(GameState, MyPlayer, MyLocation))
            {
                GameService.WriteMove(_move);
                return;
            }


            // compute bomb placement blocks
            if (WallsInMap)
            {
                if (FindBombPlacementBlock(GameState, MyPlayer, MyLocation, MyKey))
                {
                    GameService.WriteMove(_move);
                    return;
                }
            }
            else
            {
                if (FindPlacementBlockToExploreOrAttack(GameState, MyPlayer, MyLocation, MyKey))
                {
                    GameService.WriteMove(_move);
                    return;
                }
            }

            // Well, It seem we can't do anything good.
            GameService.WriteMove(Move.DoNothing);
        }

        private Move GetMoveFromLocation(Location playerLoc, Location loc)
        {
            if (playerLoc.Equals(loc))
            {
                return Move.DoNothing;
            }

            if (loc.X == playerLoc.X)
            {
                GameService.UpdateBlocksToExplore(loc);
                return loc.Y > playerLoc.Y ? Move.MoveDown : Move.MoveUp;
            }

            if (loc.Y == playerLoc.Y)
            {
                GameService.UpdateBlocksToExplore(loc);
                return loc.X > playerLoc.X ? Move.MoveRight : Move.MoveLeft;
            }

            return Move.DoNothing;
        }

        private MapSafeBlock FindHidingBlock(GameState state, Player player, Location startLoc, IEnumerable<Bomb> bombsToDodge = null, bool stayClear = false)
        {
            var blastRadius = player.BombRadius;
            var bombTimer = Math.Min(9, (player.BombBag * 3)) + 1;

            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();


            while (openSet.Count != 0)
            {
                var qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var safeNode = stayClear ? BotHelper.FindPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear) : BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

                if (safeNode != null && safeNode.FCost < bombTimer)
                {
                    var visibleBombs = BotHelper.FindVisibleBombs(state, qNode.Location);

                    if (visibleBombs == null)
                    {
                        if (qNode.Location.X != startLoc.X && qNode.Location.Y != startLoc.Y)
                        {
                            return new MapSafeBlock
                            {
                                Location = qNode.Location,
                                Distance = safeNode.FCost
                            };
                        }

                        var blockDistance = qNode.Location.X == startLoc.X ? Math.Abs(qNode.Location.Y - startLoc.Y) : Math.Abs(qNode.Location.X - startLoc.X);

                        if (blockDistance > blastRadius)
                        {
                            return new MapSafeBlock
                            {
                                Location = qNode.Location,
                                Distance = safeNode.FCost
                            };
                        }
                    }

                    var possibleBlocksLoc = stayClear ? BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location, player, bombsToDodge, stayClear) : BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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
            return null;
        }

        private IEnumerable<MapBombPlacementBlock> FindBombPlacementBlocks(GameState state, Player player, Location startLoc, int maxPlacementBlocks = 0, bool oneBlockLookUp = false)
        {
            var openSet = new HashSet<MapNode>() { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();
            List<List<DestructibleWall>> destroyedWalls = new List<List<DestructibleWall>>();
            int searchCount = 5;

            var bombPlacementBlocks = new List<MapBombPlacementBlock>();
            MapNode qNode;

            while (openSet.Count != 0)
            {
                if (oneBlockLookUp)
                {
                    if (searchCount < 1)
                    {
                        return bombPlacementBlocks.Count == 0 ? null : bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                                                                     .ThenBy(b => b.PowerDistance)
                                                                                     .ThenByDescending(b => b.VisibleWalls)
                                                                                     .ThenBy(b => b.Distance);
                    }
                }
                else if (bombPlacementBlocks.Count > maxPlacementBlocks)
                {
                    return bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                              .ThenBy(b => b.PowerDistance)
                                              .ThenByDescending(b => b.VisibleWalls)
                                              .ThenBy(b => b.Distance);
                }

                qNode = openSet.OrderBy(n => n.GCost).First();

                var visibleWalls = BotHelper.FindVisibleWalls(state, qNode.Location, player);

                if (visibleWalls != null)
                {
                    if (!WallsDestroyed(destroyedWalls, visibleWalls))
                    {
                        destroyedWalls.Add(visibleWalls);

                        var mapNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

                        if (mapNode != null)
                        {
                            var nearByPowerUp = FindNearByMapPowerUpBlock(state, qNode.Location, player.Key);

                            var mapBlock = new MapBombPlacementBlock
                            {
                                Location = qNode.Location,
                                Distance = mapNode.FCost,
                                LocationToBlock = BotHelper.ReconstructPath(mapNode),
                                VisibleWalls = visibleWalls.Count,
                                PowerDistance = nearByPowerUp == null ? int.MaxValue : nearByPowerUp.Distance,
                                SuperDistance = state.SuperLocation == null ? 0 : BotHelper.FindPathToTarget(state, qNode.Location, state.SuperLocation, super: true).FCost
                            };

                            bombPlacementBlocks.Add(mapBlock);
                        }
                    }
                }

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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

                if (oneBlockLookUp) searchCount--;
            }
            return bombPlacementBlocks.Count == 0 ? null : bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                                                              .ThenBy(b => b.PowerDistance)
                                                                              .ThenByDescending(b => b.VisibleWalls)
                                                                              .ThenBy(b => b.Distance);
        }

        private IEnumerable<MapSafeBlock> FindSafeBlocks(GameState state, Player player, Location startLoc, IEnumerable<Bomb> bombsToDodge)
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

                MapNode safeNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

                //if we can reach this location, and in time


                if (safeNode != null && safeNode.FCost < bomb.BombTimer)
                {
                    var visibleBombs = BotHelper.FindVisibleBombs(state, qNode.Location);

                    if (visibleBombs == null)
                    {
                        var visibleWalls = BotHelper.FindVisibleWalls(state, qNode.Location, player);

                        var nearByPowerUp = FindNearByMapPowerUpBlock(state, qNode.Location, player.Key);

                        //add block
                        var mapBlock = new MapSafeBlock
                        {
                            Location = qNode.Location,
                            Distance = safeNode.FCost,
                            LocationToBlock = BotHelper.ReconstructPath(safeNode),
                            VisibleWalls = visibleWalls == null ? 0 : visibleWalls.Count,
                            PowerDistance = nearByPowerUp == null ? int.MaxValue : nearByPowerUp.Distance,
                            SuperDistance = state.SuperLocation == null ? 0 : state.SuperLocation == null ? 0 : BotHelper.FindPathToTarget(state, qNode.Location, state.SuperLocation, super: true).FCost,
                            MapNode = safeNode
                        };
                        safeBlocks.Add(mapBlock);
                    }

                    var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

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
            return safeBlocks.Count == 0 ? null : safeBlocks.OrderBy(block => block.Distance)
                                                            .ThenBy(Block => Block.SuperDistance)
                                                            .ThenByDescending(block => block.VisibleWalls)
                                                            .ThenBy(block => block.PowerDistance);
        }


        private List<Location> GetRouteLocations(MapNode mapNode)
        {
            var routeLocations = new List<Location>();

            var currentNode = mapNode;

            while (currentNode.Parent != null)
            {
                routeLocations.Insert(0, currentNode.Location);
                currentNode = currentNode.Parent;
            }
            return routeLocations.Count == 0 ? null : routeLocations;
        }

        private MapBombPlacementBlock FindPlacementBlockToDestroyPlayer(GameState state, Player player, Location startLoc)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            MapNode qNode;


            while (openSet.Count != 0)
            {

                qNode = openSet.OrderBy(node => node.GCost).First();

                if (BotHelper.IsAnyPlayerVisible(state, player, qNode.Location))
                {
                    var mapNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

                    if (mapNode != null)
                    {
                        return new MapBombPlacementBlock
                        {
                            Location = qNode.Location,
                            LocationToBlock = BotHelper.ReconstructPath(mapNode)
                        };
                    }
                }

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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

        private bool WallsDestroyed(List<List<DestructibleWall>> destroyedWalls, List<DestructibleWall> walls)
        {
            var curWalls = new HashSet<DestructibleWall>(walls);

            for (var i = 0; i < destroyedWalls.Count; i++)
            {
                if (curWalls.SetEquals(destroyedWalls[i])) return true;
            }
            return false;
        }

        private MapSafeBlock FindSafeBlockFromPlayer(GameState state, Player player, Location startLoc, IEnumerable<Bomb> bombsToDodge, Bomb opponentBomb)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            while (openSet.Count != 0)
            {
                var qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                MapNode safeNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

                if (safeNode != null && safeNode.FCost < opponentBomb.BombTimer)
                {
                    var visibleBombs = BotHelper.FindVisibleBombs(state, qNode.Location);

                    if (visibleBombs == null)
                    {
                        return new MapSafeBlock
                        {
                            Location = qNode.Location,
                            Distance = safeNode.FCost,
                            LocationToBlock = BotHelper.ReconstructPath(safeNode),
                            MapNode = safeNode
                        };
                    }

                    var bombToDodge = visibleBombs.FirstOrDefault(bomb => bomb == opponentBomb);

                    if (bombToDodge == null)
                    {

                        return new MapSafeBlock
                        {
                            Location = qNode.Location,
                            Distance = safeNode.FCost,
                            LocationToBlock = BotHelper.ReconstructPath(safeNode),
                            MapNode = safeNode
                        };
                    }

                    var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

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
            return null;
        }

        private bool IsBlockInPlayerRange(GameState state, Location startLoc, Location targetLoc, int range)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            while (openSet.Count != 0)
            {

                var qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var blockNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

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

                    var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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

        private MapPowerUpBlock FindNearByMapPowerUpBlock(GameState state, Location startLoc, string playerKey)
        {

            var opponentLocations = state.Players.Where(p => (p.Key != playerKey && !p.Killed))
                                                 .Select(p => new Location(p.Location.X - 1, p.Location.Y - 1));

            //var opponentLocations = new List<Location>();

            //state.Players.FindAll(p => (p.Key != playerKey && !p.Killed))
            //     .ForEach(p => opponentLocations.Add(new Location(p.Location.X - 1, p.Location.Y - 1)));

            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(n => n.GCost).First();

                var mapEntity = state.GetBlockAtLocation(qNode.Location).PowerUp;

                if (mapEntity != null)
                {
                    var mapNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

                    if (mapNode != null)
                    {
                        var foundPowerUpBlock = true;

                        foreach (var playerLoc in opponentLocations)
                        {
                            if (IsBlockInPlayerRange(state, playerLoc, qNode.Location, mapNode.FCost))
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
                                LocationToBlock = BotHelper.ReconstructPath(mapNode),
                                PowerUP = mapEntity
                            };
                        }
                    }
                }

                //
                openSet.Remove(qNode);
                closedSet.Add(qNode);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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

        private bool StayClearOfBombs(GameState state, Player player, Location playerLoc, string playerKey)
        {
            // Stay clear
            var visibleBombs = BotHelper.FindVisibleBombs(state, playerLoc);

            if (visibleBombs == null)
            {
                return false;
            }

            _anyBombVisible = true;

            var visibleOpponentBomb = visibleBombs.OrderBy(b => b.BombTimer)
                                                  .FirstOrDefault(b => !player.IsBombOwner(b));

            Location opponentLocation = null;
            List<Bomb> opponentBombs = null;
            IEnumerable<Bomb> opponentVisibleBombs = null;
            IEnumerable<MapSafeBlock> opponentSafeBlocks = null;

            // if op bomb available
            if (visibleOpponentBomb != null)
            {
                opponentLocation = state.GetPlayerLocation(visibleOpponentBomb.Owner.Key);
                if (opponentLocation != null)
                {
                    opponentBombs = state.GetPlayerBombs(visibleOpponentBomb.Owner.Key);
                    opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                    if (opponentVisibleBombs != null)
                    {
                        opponentSafeBlocks = FindSafeBlocks(state, visibleOpponentBomb.Owner, opponentLocation, opponentVisibleBombs);
                    }
                }
            }

            // if not own bomb
            //if (!player.IsBombOwner(bombToDodge))
            //{
            //    opponentLocation = state.GetPlayerLocationOnMap(bombToDodge.Owner.Key);
            //    if (opponentLocation != null)
            //    {
            //        opponentBombs = state.GetPlayerBombs(bombToDodge.Owner.Key);
            //        opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

            //        if (opponentVisibleBombs != null)
            //        {
            //            opponentSafeBlocks = FindSafeBlocks(state, bombToDodge.Owner, opponentLocation, opponentVisibleBombs);
            //        }
            //    }
            //}

            var safeBlocks = FindSafeBlocks(state, player, playerLoc, visibleBombs);

            if (safeBlocks != null)
            {
                var findNearestHidingBlock = false;

                var ownBomb = visibleBombs.FirstOrDefault(bomb => player.IsBombOwner(bomb));

                bool anyPlayerVisible = ownBomb == null ? false : BotHelper.IsAnyPlayerVisible(state, ownBomb);


                if (anyPlayerVisible)
                {
                    findNearestHidingBlock = true;
                }
                else
                {
                    bool anyChainingBomb = IsAnyBombChaining(state, visibleBombs);
                    if (anyChainingBomb) findNearestHidingBlock = true;
                }

                var prioritySafeBlocks = findNearestHidingBlock ? safeBlocks : safeBlocks.OrderBy(block => block.SuperDistance)
                                                                                         .ThenBy(block => block.PowerDistance)
                                                                                         .ThenByDescending(block => block.VisibleWalls)
                                                                                         .ThenBy(block => block.Distance);

                foreach (var safeBlock in prioritySafeBlocks)
                {
                    if (!visibleBombs.Any(bomb => !player.IsBombOwner(bomb)))
                    {
                        if (!anyPlayerVisible)
                        {
                            if (PlaceBombWhileInDanger(state, player, playerLoc, playerKey, visibleBombs))
                            {
                                _move = Move.PlaceBomb;
                                return true;
                            }
                        }

                        _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                        return true;
                    }
                    else
                    {
                        if (opponentLocation == null)
                        {
                            _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                            return true;
                        }

                        if (opponentVisibleBombs == null)
                        {
                            // else just take the closet safe block
                            _move = GetMoveFromLocation(playerLoc, safeBlocks.First().LocationToBlock);
                            return true;
                        }

                        if (PlaceBombWhileInDangerToDestroyPlayer(state, player, playerLoc, playerKey, visibleBombs, opponentSafeBlocks))
                        {
                            _move = Move.PlaceBomb;
                            return true;
                        }

                        // if we can reach our safe block before op
                        if (opponentSafeBlocks == null || safeBlock.Distance <= opponentSafeBlocks.First().Distance)
                        {
                            _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                            return true;
                        }

                        if (opponentSafeBlocks != null)
                        {
                            var opponentSafeBlock = opponentSafeBlocks.First();

                            // We might clear away from dangerous bomb well in time
                            var maxSearch = opponentSafeBlock.Distance;
                            var searchLocations = GetRouteLocations(safeBlock.MapNode);

                            for (var i = 0; i < maxSearch; i++)
                            {
                                var bombsToDodge = BotHelper.FindVisibleBombs(state, searchLocations[i]);
                                if (bombsToDodge == null || !bombsToDodge.Contains(visibleOpponentBomb))
                                {
                                    _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                                    return true;
                                }
                            }

                            // This might be all we need, but I can't reproduce problem solved by the above routine
                            // so, I'll just leave it. [distance to move to safety + 1 move to trigger]
                            if (safeBlock.Distance <= opponentSafeBlock.Distance + 1)
                            {
                                _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                                return true;
                            }
                        }
                    }

                    //if (player.IsBombOwner(bombToDodge))
                    //{
                    //    if (!anyPlayerVisible)
                    //    {
                    //        if (PlaceBombWhileInDanger(state, player, playerLoc, playerKey, visibleBombs))
                    //        {
                    //            _move = Move.PlaceBomb;
                    //            return true;
                    //        }
                    //    }

                    //    _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //    return true;
                    //}
                    //else
                    //{
                    //    if (opponentLocation == null)
                    //    {
                    //        _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //        return true;
                    //    }

                    //    //if (bombToDodge.BombTimer > opponentBombs[0].BombTimer)
                    //    //{
                    //    //    _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //    //    return true;
                    //    //}

                    //    if (opponentVisibleBombs == null)
                    //    {
                    //        // else just take the closet safe block
                    //        _move = GetMoveFromLocation(playerLoc, safeBlocks.First().LocationToBlock);
                    //        return true;
                    //    }


                    //    if (PlaceBombWhileInDangerToDestroyPlayer(state, player, playerLoc, playerKey, visibleBombs, opponentSafeBlocks))
                    //    {
                    //        _move = Move.PlaceBomb;
                    //        return true;
                    //    }

                    //    //if (opponentVisibleBombs != null)
                    //    //{
                    //    // if we can reach our safe block before op
                    //    if (opponentSafeBlocks == null || safeBlock.Distance <= opponentSafeBlocks.First().Distance)
                    //    {
                    //        _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //        return true;
                    //    }

                    //    if (opponentSafeBlocks != null)
                    //    {
                    //        var opponentSafeBlock = opponentSafeBlocks.First();

                    //        // We might clear away from dangerous bomb well in time
                    //        var maxSearch = opponentSafeBlock.Distance;
                    //        var searchLocations = GetRouteLocations(safeBlock.MapNode);

                    //        for (var i = 0; i < maxSearch; i++)
                    //        {
                    //            var bombsToDodge = BotHelper.FindVisibleBombs(state, searchLocations[i]);
                    //            if (bombsToDodge == null || !bombsToDodge.Contains(bombToDodge))
                    //            {
                    //                _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //                return true;
                    //            }
                    //        }

                    //        // This might be all we need, but I can't reproduce problem solved by the above routine
                    //        // so, I'll just leave it. [distance to move to safety + 1 move to trigger]
                    //        if (safeBlock.Distance <= opponentSafeBlock.Distance + 1)
                    //        {
                    //            _move = GetMoveFromLocation(playerLoc, safeBlock.LocationToBlock);
                    //            return true;
                    //        }
                    //    }
                    //    //}
                    //}
                }
            }

            // op bomb 
            if (visibleOpponentBomb != null)
            {
                // op decsions

                if (opponentVisibleBombs == null || opponentSafeBlocks != null)
                {
                    var mapSafeBlock = FindSafeBlockFromPlayer(state, player, playerLoc, visibleBombs, visibleOpponentBomb);

                    if (mapSafeBlock != null)
                    {
                        if (opponentSafeBlocks != null)
                        {
                            // can clear safe bomb or rather reach safe block before op triggers
                            if (mapSafeBlock.Distance <= opponentSafeBlocks.First().Distance + 1)
                            {
                                // emergency trigger
                                var ownBombs = state.GetPlayerBombs(playerKey);

                                if (ownBombs != null && !visibleBombs.Any(b => b == ownBombs[0]))
                                {
                                    // check if we are clearing the correct bomb
                                    var bombsToClear = BotHelper.FindVisibleBombs(state, mapSafeBlock.Location);

                                    if (bombsToClear != null && bombsToClear.Any(b => b == ownBombs[0]))
                                    {
                                        _move = Move.TriggerBomb;
                                        return true;
                                    }
                                }
                                // we don't have any safe bomb to clear, so just grab the location
                                _move = GetMoveFromLocation(playerLoc, mapSafeBlock.LocationToBlock);
                                return true;
                            }
                        }
                        else if (opponentVisibleBombs == null)
                        {
                            // we are in real danger, so no time to clear any bomb
                            _move = GetMoveFromLocation(playerLoc, mapSafeBlock.LocationToBlock);
                            return true;
                        }
                    }
                }
            }

            //var opponentBomb = visibleBombs.FirstOrDefault(bomb => !player.IsBombOwner(bomb));

            //if (opponentBomb != null)
            //{
            //    // op decsions
            //    // if didn't compute op's
            //    if (player.IsBombOwner(bombToDodge))
            //    {
            //        opponentLocation = state.GetPlayerLocationOnMap(opponentBomb.Owner.Key);
            //        if (opponentLocation != null)
            //        {
            //            opponentBombs = state.GetPlayerBombs(opponentBomb.Owner.Key);
            //            opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

            //            if (opponentVisibleBombs != null)
            //            {
            //                opponentSafeBlocks = FindSafeBlocks(state, opponentBomb.Owner, opponentLocation, opponentVisibleBombs);
            //            }
            //        }
            //    }

            //    if (opponentVisibleBombs == null || opponentSafeBlocks != null)
            //    {
            //        var mapSafeBlock = FindSafeBlockFromPlayer(state, player, playerLoc, visibleBombs, opponentBomb);

            //        if (mapSafeBlock != null)
            //        {
            //            if (opponentSafeBlocks != null)
            //            {
            //                // can clear safe bomb or rather reach safe block before op triggers
            //                if (mapSafeBlock.Distance <= opponentSafeBlocks.First().Distance + 1)
            //                {
            //                    // emergency trigger
            //                    var ownBombs = state.GetPlayerBombs(playerKey);

            //                    if (ownBombs != null && !visibleBombs.Any(b => b == ownBombs[0]))
            //                    {
            //                        // check if we are clearing the correct bomb
            //                        var bombsToClear = BotHelper.FindVisibleBombs(state, mapSafeBlock.Location);

            //                        if (bombsToClear != null && bombsToClear.Any(b => b == ownBombs[0]))
            //                        {
            //                            _move = Move.TriggerBomb;
            //                            return true;
            //                        }
            //                    }
            //                    // we don't have any safe bomb to clear, so just grab the location
            //                    _move = GetMoveFromLocation(playerLoc, mapSafeBlock.LocationToBlock);
            //                    return true;
            //                }
            //            }
            //            else if (opponentVisibleBombs == null)
            //            {
            //                // we are in real danger, so no time to clear any bomb
            //                _move = GetMoveFromLocation(playerLoc, mapSafeBlock.LocationToBlock);
            //                return true;
            //            }
            //        }
            //    }
            //}

            _move = Move.DoNothing;
            return true;
        }

        private bool IsAnyBombChaining(GameState state, IEnumerable<Bomb> bombs)
        {
            foreach (var bomb in bombs)
            {
                var chainBombs = BotHelper.FindVisibleBombs(state, new Location(bomb.Location.X - 1, bomb.Location.Y - 1), chaining: true);
                if (chainBombs != null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool TriggerBomb(GameState state, Player player, Location playerLoc, string playerKey)
        {
            if (_anyBombVisible)
            {
                return false;
            }

            var playerBombs = state.GetPlayerBombs(playerKey);

            if (playerBombs == null || playerBombs[0].BombTimer < 2)
            {
                return false;
            }

            var testPlacementBlock = FindBombPlacementBlocks(state, player, playerLoc);

            if (testPlacementBlock == null)
            {
                _move = Move.TriggerBomb;
                return true;
            }
            else if (testPlacementBlock.First().Distance == 0)
            {
                if (playerBombs.Count >= player.BombBag || FindHidingBlock(state, player, playerLoc) == null)
                {
                    _move = Move.TriggerBomb;
                    return true;
                }
            }

            // attack 1
            var bombLoc = new Location(playerBombs[0].Location.X - 1, playerBombs[0].Location.Y - 1);

            var visibleOpponents = BotHelper.FindVisiblePlayers(state, bombLoc, playerKey, playerBombs[0].BombRadius);

            if (visibleOpponents != null)
            {
                bool trigger = false;

                foreach (var opponent in visibleOpponents)
                {
                    var opponentLocation = state.GetPlayerLocation(opponent.Key);

                    var opVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                    if (opVisibleBombs != null)
                    {
                        var opponentPossibleMovesLoc = BotHelper.ExpandMoveBlocks(state, opponentLocation, opponentLocation, opponent, opVisibleBombs, stayClear: true);

                        foreach (var loc in opponentPossibleMovesLoc)
                        {
                            var bombsInLine = BotHelper.FindVisibleBombs(state, loc);

                            // check if op will dodge
                            if (bombsInLine == null)
                            {
                                trigger = false;
                                break;
                            }
                            trigger = true;
                        }

                        if (trigger)
                        {
                            _move = Move.TriggerBomb;
                            return true;
                        }
                    }
                }
            }

            // attack 2

            var oneBlockOpponents = new List<MapOpponent>();

            var opponents = state.Players.Where(p => (p.Key != player.Key && !p.Killed))
                                         .Select(p => new MapOpponent(p));


            foreach (var opponent in opponents)
            {
                var dx = Math.Abs(bombLoc.X - opponent.Location.X);
                var dy = Math.Abs(bombLoc.Y - opponent.Location.Y);

                if (dx <= playerBombs[0].BombRadius && dy <= playerBombs[0].BombRadius)
                {
                    // one block difference
                    if (dx == 1 || dy == 1)
                    {
                        oneBlockOpponents.Add(opponent);
                    }
                }
            }

            if (oneBlockOpponents.Count > 0)
            {
                var blastBlocks = BotHelper.FindAllBlastLocations(state, bombLoc, playerBombs[0].BombRadius);

                if (blastBlocks != null)
                {
                    foreach (var oneBlockOpponent in oneBlockOpponents)
                    {
                        var opPossibleMoves = BotHelper.ExpandMoveBlocks(state, oneBlockOpponent.Location, oneBlockOpponent.Location);

                        if (opPossibleMoves.Count == 1 && blastBlocks.Contains(opPossibleMoves[0]))
                        {
                            _move = Move.TriggerBomb;
                            return true;
                        }
                    }
                }
            }
            // End attack

            return false;
        }

        private bool PriorityChasePower(GameState state, Player player, Location playerLoc, string playerKey)
        {
            var nearByPowerUp = FindNearByMapPowerUpBlock(state, playerLoc, playerKey);

            if (nearByPowerUp == null)
            {
                return false;
            }

            _nearByPowerUp = nearByPowerUp;

            // if radius power up
            if (nearByPowerUp.PowerUP is BombRadiusPowerUp)
            {
                if (nearByPowerUp.Distance < state.MaxRadiusPowerChase
                && player.BombRadius < state.MaxBombBlast)
                {
                    _move = GetMoveFromLocation(playerLoc, nearByPowerUp.LocationToBlock);
                    return true;
                }
            }
            else if (nearByPowerUp.PowerUP is BombBagPowerUp)
            {
                //if bag power up
                if (nearByPowerUp.Distance < state.MaxBagPowerChase
                    && player.BombBag < 3)
                {
                    _move = GetMoveFromLocation(playerLoc, nearByPowerUp.LocationToBlock);
                    return true;
                }
            }
            else
            {
                //super 
                _move = GetMoveFromLocation(playerLoc, nearByPowerUp.LocationToBlock);
                return true;
            }

            return false;
        }

        private bool PlaceBomb(GameState state, Player player, Location playerLoc, string playerKey)
        {
            if (_anyBombVisible)
            {
                return false;
            }

            var playerBombs = state.GetPlayerBombs(playerKey);

            // return early if possible
            if (playerBombs != null && playerBombs.Count >= player.BombBag)
            {
                return false;
            }

            if (FindHidingBlock(state, player, playerLoc) == null)
            {
                return false;
            }

            // attack 1
            var visibleOpponents = BotHelper.FindVisiblePlayers(state, playerLoc, playerKey, player.BombRadius);

            if (visibleOpponents != null)
            {
                bool strike = false;

                foreach (var opponent in visibleOpponents)
                {
                    var opponentLocation = state.GetPlayerLocation(opponent.Key);

                    var opVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                    if (opVisibleBombs != null)
                    {
                        var opponentPossibleMovesLoc = BotHelper.ExpandMoveBlocks(state, opponentLocation, opponentLocation, opponent, opVisibleBombs, stayClear: true);

                        foreach (var loc in opponentPossibleMovesLoc)
                        {
                            var locVisible = IsBlockBlastVisible(player, playerLoc, loc);

                            var bombsInLine = BotHelper.FindVisibleBombs(state, loc);

                            // check if we are not only introducing the only bomb
                            if (!locVisible && bombsInLine == null)
                            {
                                strike = false;
                                break;
                            }
                            strike = true;
                        }

                        if (strike)
                        {
                            _move = Move.PlaceBomb;
                            return true;
                        }
                    }
                }
            }

            // TODO: Attack 2
            var oneBlockOpponents = new List<MapOpponent>();

            var opponents = state.Players.Where(p => (p.Key != player.Key && !p.Killed))
                                         .Select(p => new MapOpponent(p));

            foreach (var opponent in opponents)
            {
                var dx = Math.Abs(playerLoc.X - opponent.Location.X);
                var dy = Math.Abs(playerLoc.Y - opponent.Location.Y);

                if (dx <= player.BombRadius && dy <= player.BombRadius)
                {
                    // one block difference
                    if (dx == 1 || dy == 1)
                    {
                        oneBlockOpponents.Add(opponent);
                    }
                }
            }

            if (oneBlockOpponents.Count > 0)
            {
                var blastBlocks = BotHelper.FindAllBlastLocations(state, playerLoc, player.BombRadius);

                if (blastBlocks != null)
                {
                    foreach (var oneBlockOpponent in oneBlockOpponents)
                    {
                        var opPossibleMoves = BotHelper.ExpandMoveBlocks(state, oneBlockOpponent.Location, oneBlockOpponent.Location);

                        if (opPossibleMoves.Count == 0)
                        {
                            _move = Move.PlaceBomb;
                            return true;
                        }

                        if (opPossibleMoves.Count == 1 && blastBlocks.Contains(opPossibleMoves[0]))
                        {
                            _move = Move.PlaceBomb;
                            return true;
                        }
                    }
                }
            }

            var visibleWalls = BotHelper.FindVisibleWalls(state, playerLoc, player);
            if (visibleWalls == null)
            {
                return false;
            }

            var bombPlacementBlocks = FindBombPlacementBlocks(state, player, playerLoc, oneBlockLookUp: true);

            //if we can score 4
            if (visibleWalls.Count == 3)
            {
                // if a better location in next block
                if (bombPlacementBlocks != null)
                {
                    var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 3)
                                                                .FirstOrDefault(b => b.Distance < 2);

                    if (bombPlacementBlock != null)
                    {
                        _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                        return true;
                    }
                }
            }
            else if (visibleWalls.Count == 2)
            {
                // if a better location in next block
                if (bombPlacementBlocks != null)
                {
                    var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 2)
                                                                .FirstOrDefault(b => b.Distance < 2);

                    if (bombPlacementBlock != null)
                    {
                        _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                        return true;
                    }
                }
            }
            else if (visibleWalls.Count == 1)
            {
                // if a better location in next block
                if (bombPlacementBlocks != null)
                {
                    var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 1)
                                                                .FirstOrDefault(b => b.Distance < 2);

                    if (bombPlacementBlock != null)
                    {
                        _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                        return true;
                    }
                }
            }

            _move = Move.PlaceBomb;
            return true;
        }

        private bool ChasePower(GameState state, Player player, Location playerLoc)
        {

            if (_nearByPowerUp == null)
            {
                return false;
            }

            var nearByPowerUp = _nearByPowerUp;

            //if bomb radius power up
            if (nearByPowerUp.PowerUP is BombRadiusPowerUp)
            {
                if (player.BombRadius < state.MaxBombBlast)
                {
                    _move = GetMoveFromLocation(playerLoc, nearByPowerUp.LocationToBlock);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // bomb bag
                _move = GetMoveFromLocation(playerLoc, nearByPowerUp.LocationToBlock);
                return true;
            }
        }

        private bool FindBombPlacementBlock(GameState state, Player player, Location playerLoc, string playerKey)
        {
            //var r = new Random();
            //var maxPlacements = r.Next(5, 10);
            var bombPlacementBlocks = state.PercentageWall > 10 ? FindBombPlacementBlocks(state, player, playerLoc, 10) : FindBombPlacementBlocks(state, player, playerLoc, 2);
            var visibleWalls = BotHelper.FindVisibleWalls(state, playerLoc, player);
            var playerBombs = state.GetPlayerBombs(playerKey);

            if (visibleWalls != null)
            {
                // if we can score 4
                if (visibleWalls.Count == 3)
                {
                    // if a better location in next block
                    if (bombPlacementBlocks != null)
                    {
                        var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 3)
                                                                    .FirstOrDefault(b => b.Distance < 2);

                        if (bombPlacementBlock != null)
                        {
                            _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                            return true;
                        }
                    }
                }

                // if we can score 3
                if (visibleWalls.Count == 2)
                {
                    // if a better location in next block
                    if (bombPlacementBlocks != null)
                    {
                        var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 2)
                                                                    .FirstOrDefault(b => b.Distance < 2);

                        if (bombPlacementBlock != null)
                        {
                            _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                            return true;
                        }
                    }
                }

                // if we can score 2
                if (visibleWalls.Count == 1)
                {
                    // if a better location in next block
                    if (bombPlacementBlocks != null)
                    {
                        var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 1)
                                                                    .FirstOrDefault(b => b.Distance < 2);

                        if (bombPlacementBlock != null)
                        {
                            _move = GetMoveFromLocation(playerLoc, bombPlacementBlock.LocationToBlock);
                            return true;
                        }
                    }
                }
            }

            if (visibleWalls == null && bombPlacementBlocks != null)
            {
                _move = GetMoveFromLocation(playerLoc, bombPlacementBlocks.First().LocationToBlock);
                return true;
            }

            return false;
        }

        private bool FindPlacementBlockToExploreOrAttack(GameState state, Player player, Location playerLoc, string playerKey)
        {
            var playerBombs = state.GetPlayerBombs(playerKey);

            if (!_anyBombVisible && (playerBombs == null || playerBombs.Count < player.BombBag))
            {
                // Plant if we can find hide block after planting the bomb
                if (FindHidingBlock(state, player, playerLoc) != null)
                {
                    var visiblePlayers = BotHelper.FindVisiblePlayers(state, playerLoc, playerKey, player.BombRadius);

                    if (visiblePlayers != null)
                    {
                        _move = Move.PlaceBomb;
                        return true;
                    }
                }
            }

            var leadScore = state.Players
                               .Where(p => p.Key != playerKey)
                               .Where(p => !p.Killed)
                               .OrderBy(p => p.Points)
                               .Select(p=>p.Points)
                               .First();

            var playerScore = player.Points;

            // if we are losing try to destroy the other chap
            if (playerScore <= leadScore)
            {
                var visiblePlayerBlock = FindPlacementBlockToDestroyPlayer(state, player, playerLoc);

                if (visiblePlayerBlock != null)
                {
                    _move = GetMoveFromLocation(playerLoc, visiblePlayerBlock.LocationToBlock);
                    return true;
                }
            }

            if (BlocksToExploreInMap)
            {
                BlockToExplore nearestToExplore = FindNearestBlockToExplore(state, playerLoc, GameService.BlocksToExplore);

                if (nearestToExplore != null)
                {
                    _move = GetMoveFromLocation(playerLoc, nearestToExplore.LocationToBlock);
                    return true;
                }
            }
            return false;
        }

        private BlockToExplore FindNearestBlockToExplore(GameState state, Location startLoc, HashSet<Location> blocksToExplore)
        {
            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            while (openSet.Count != 0)
            {
                var qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                if (blocksToExplore.Contains(qNode.Location))
                {
                    MapNode mapNode = BotHelper.FindPathToTarget(state, startLoc, qNode.Location);

                    if (mapNode != null)
                    {
                        return new BlockToExplore
                        {
                            Location = qNode.Location,
                            Distance = mapNode.FCost,
                            LocationToBlock = BotHelper.ReconstructPath(mapNode),
                        };
                    }
                }

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qNode.Location);

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

        private bool PlaceBombWhileInDanger(GameState state, Player player, Location playerLoc, string playerKey, IEnumerable<Bomb> bombsToDodge)
        {
            // return early if possible
            if (bombsToDodge.Count() > 1) return false;

            var playerBombs = state.GetPlayerBombs(playerKey);

            if (playerBombs != null && playerBombs.Count >= player.BombBag)
            {
                return false;
            }

            var visibleWalls = BotHelper.FindVisibleWalls(state, playerLoc, player);

            if (visibleWalls == null)
            {
                //TODO: attack 1
                bool strike = false;
                var visibleOpponents = BotHelper.FindVisiblePlayers(state, playerLoc, playerKey, player.BombRadius);

                if (visibleOpponents != null)
                {
                    foreach (var opponent in visibleOpponents)
                    {
                        var opponentLocation = state.GetPlayerLocation(opponent.Key);

                        var opVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                        if (opVisibleBombs != null)
                        {
                            var opponentPossibleMovesLoc = BotHelper.ExpandMoveBlocks(state, opponentLocation, opponentLocation, opponent, opVisibleBombs, stayClear: true);

                            foreach (var loc in opponentPossibleMovesLoc)
                            {
                                var locVisible = IsBlockBlastVisible(player, playerLoc, loc);

                                var bombsInLine = BotHelper.FindVisibleBombs(state, loc);

                                // check if we are not only introducing the only bomb
                                if (!locVisible && bombsInLine == null)
                                {
                                    strike = false;
                                    break;
                                }
                                strike = true;
                            }
                        }
                        if (strike)
                        {
                            break;
                        }
                    }
                }

                // TODO: Attack 2
                if (!strike)
                {
                    var oneBlockOpponents = new List<MapOpponent>();

                    var opponents = state.Players.Where(p => (p.Key != player.Key && !p.Killed))
                                                 .Select(p => new MapOpponent(p));

                    foreach (var opponent in opponents)
                    {
                        var dx = Math.Abs(playerLoc.X - opponent.Location.X);
                        var dy = Math.Abs(playerLoc.Y - opponent.Location.Y);

                        if (dx <= player.BombRadius && dy <= player.BombRadius)
                        {
                            // one block difference
                            if (dx == 1 || dy == 1)
                            {
                                oneBlockOpponents.Add(opponent);
                            }
                        }
                    }

                    if (oneBlockOpponents.Count > 0)
                    {
                        var blastBlocks = BotHelper.FindAllBlastLocations(state, playerLoc, player.BombRadius);

                        if (blastBlocks != null)
                        {
                            foreach (var oneBlockOpponent in oneBlockOpponents)
                            {
                                var opPossibleMoves = BotHelper.ExpandMoveBlocks(state, oneBlockOpponent.Location, oneBlockOpponent.Location);

                                if (opPossibleMoves.Count == 0)
                                {
                                    strike = true;
                                    break;
                                }

                                if (opPossibleMoves.Count == 1 && blastBlocks.Contains(opPossibleMoves[0]))
                                {
                                    strike = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!strike) return false;
            }

            var explodingBomb = bombsToDodge.First();

            var chainingBombs = BotHelper.FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

            while (chainingBombs != null)
            {
                if (chainingBombs.Any(bomb => !player.IsBombOwner(bomb))) return false;

                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                if (chainingBombs.Count() > 1) return false;

                if (chainingBombs.Count() > 0)
                {
                    explodingBomb = chainingBombs.First();
                    chainingBombs = BotHelper.FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                }
                else
                {
                    chainingBombs = null;
                }
            }

            var hideBlock = FindHidingBlock(GameState, MyPlayer, MyLocation, bombsToDodge, stayClear: true);

            if (hideBlock == null) return false;

            if (hideBlock.Distance < explodingBomb.BombTimer - 1)
            {
                return true;
            }
            return false;
        }

        private bool PlaceBombWhileInDangerToDestroyPlayer(GameState state, Player player, Location playerLoc, string playerKey, IEnumerable<Bomb> bombsToDodge, IEnumerable<MapSafeBlock> opponentSafeBlocks)
        {
            // return early if possible
            if (bombsToDodge.Count() > 1) return false;

            var playerBombs = state.GetPlayerBombs(playerKey);

            if (playerBombs != null && playerBombs.Count >= player.BombBag)
            {
                return false;
            }

            //TODO: destroy 
            var explodingBomb = bombsToDodge.First();
            var bombOwner = explodingBomb.Owner;

            var visibleWalls = BotHelper.FindVisibleWalls(state, playerLoc, player);

            if (visibleWalls == null)
            {
                var visiblePlayers = BotHelper.FindVisiblePlayers(state, playerLoc, playerKey, player.BombRadius);

                if (visiblePlayers == null || !visiblePlayers.Any(p => p.Key == bombOwner.Key)) return false;
            }

            var chainingBombs = BotHelper.FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);

            while (chainingBombs != null)
            {
                if (chainingBombs.Any(bomb => !bombOwner.IsBombOwner(bomb))) return false;

                chainingBombs = chainingBombs.Where(bomb => bomb.BombTimer < explodingBomb.BombTimer);

                if (chainingBombs.Count() > 1) return false;

                if (chainingBombs.Count() > 0)
                {
                    explodingBomb = chainingBombs.First();
                    chainingBombs = BotHelper.FindVisibleBombs(state, new Location(explodingBomb.Location.X - 1, explodingBomb.Location.Y - 1), chaining: true);
                }
                else
                {
                    chainingBombs = null;
                }
            }

            var hideBlock = FindHidingBlock(GameState, MyPlayer, MyLocation, bombsToDodge, stayClear: true);

            if (hideBlock == null || hideBlock.Distance >= explodingBomb.BombTimer - 1)
            {
                return false;
            }

            if (opponentSafeBlocks == null)
            {
                return true;
            }

            var ownerLocation = state.GetPlayerLocation(bombOwner.Key);

            if (ownerLocation != null)
            {
                var ownerPossibleMovesLoc = BotHelper.ExpandMoveBlocks(state, ownerLocation, ownerLocation, bombOwner, bombsToDodge, stayClear: true);

                var strike = true;

                foreach (var loc in ownerPossibleMovesLoc)
                {
                    var locVisible = IsBlockBlastVisible(player, playerLoc, loc);

                    var bombsInLine = BotHelper.FindVisibleBombs(state, loc);

                    // check if we are not only introducing the only bomb
                    if (!locVisible && bombsInLine == null)
                    {
                        strike = false;
                        break;
                    }
                }

                if (strike && hideBlock.Distance < opponentSafeBlocks.First().Distance + 1)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBlockBlastVisible(Player player, Location playerLoc, Location blockLoc)
        {
            if (playerLoc.X == blockLoc.X || playerLoc.Y == blockLoc.Y)
            {
                var locDistance = playerLoc.X == blockLoc.X ? Math.Abs(playerLoc.Y - blockLoc.Y) : Math.Abs(playerLoc.X - blockLoc.X);
                if (locDistance <= player.BombRadius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}