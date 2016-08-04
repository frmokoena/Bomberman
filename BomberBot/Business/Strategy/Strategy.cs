using BomberBot.Business.Helpers;
using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using BomberBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BomberBot.Business.Strategy
{
    public class Strategy : IStrategy
    {
        protected readonly IGameService<GameState> GameService;

        public Strategy(IGameService<GameState> gameServie)
        {
            GameService = gameServie;
        }

        public void Execute()
        {
            GameState state = GameService.GameState;
            string homePlayerKey = GameService.HomeKey;
            Player homePlayer = state.GetPlayer(homePlayerKey);
            Location homePlayerLocation = state.GetPlayerLocationOnMap(homePlayerKey);

            //Player killed
            if (homePlayerLocation == null) return;

            // Stay clear
            var visibleBombs = BotHelper.FindVisibleBombs(state, homePlayerLocation);

            if (visibleBombs != null)
            {
                var bombToDodge = visibleBombs.First();

                Location opponentLocation = null;
                List<Bomb> opponentBombs = null;
                IEnumerable<Bomb> opponentVisibleBombs = null;
                IEnumerable<MapSafeBlock> opponentSafeBlocks = null;


                // if not own bomb
                if (!homePlayer.IsBombOwner(bombToDodge))
                {
                    opponentLocation = state.GetPlayerLocationOnMap(bombToDodge.Owner.Key);
                    if (opponentLocation != null)
                    {
                        opponentBombs = state.GetPlayerBombs(bombToDodge.Owner.Key);
                        opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                        if (opponentVisibleBombs != null)
                        {
                            opponentSafeBlocks = FindSafeBlocks(state, bombToDodge.Owner, opponentLocation, opponentVisibleBombs);
                        }
                    }
                }

                var safeBlocks = FindSafeBlocks(state, homePlayer, homePlayerLocation, visibleBombs);

                if (safeBlocks != null)
                {
                    var ownBomb = visibleBombs.FirstOrDefault(bomb => homePlayer.IsBombOwner(bomb));

                    bool playerVisible = ownBomb == null ? false : BotHelper.IsAnyPlayerVisible(state, ownBomb);


                    var chainBombs = BotHelper.FindVisibleBombs(state, new Location(bombToDodge.Location.X - 1, bombToDodge.Location.Y - 1), chaining: true);

                    var findNearestHiding = chainBombs != null || playerVisible;

                    var prioritySafeBlocks = findNearestHiding ? safeBlocks : safeBlocks.OrderBy(block => block.SuperDistance)
                                                                                        .ThenBy(block => block.PowerDistance)
                                                                                        .ThenByDescending(block => block.VisibleWalls)
                                                                                        .ThenBy(block => block.Distance);

                    foreach (var safeBlock in prioritySafeBlocks)
                    {
                        if (homePlayer.IsBombOwner(bombToDodge))
                        {
                            var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                            GameService.WriteMove(move);
                            return;
                        }
                        else
                        {
                            if (opponentLocation == null)
                            {
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }

                            if (bombToDodge.BombTimer > opponentBombs[0].BombTimer)
                            {
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }


                            if (opponentVisibleBombs != null)
                            {
                                //if we can reach our safe block before op
                                if (opponentSafeBlocks == null || safeBlock.Distance <= opponentSafeBlocks.First().Distance)
                                {
                                    var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                    GameService.WriteMove(move);
                                    return;
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
                                        if (bombsToDodge == null || !bombsToDodge.Contains(bombToDodge))
                                        {
                                            var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                            GameService.WriteMove(move);
                                            return;
                                        }
                                    }

                                    // This might be all we need, but I can't reproduce problem solved by the above routine
                                    // so, I'll just leave it. [distance to move to safety + 1 move to trigger]
                                    if (safeBlock.Distance <= opponentSafeBlock.Distance + 1)
                                    {
                                        var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                        GameService.WriteMove(move);
                                        return;
                                    }
                                }


                            }
                            else
                            {
                                // else just take the closet safe block
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlocks.First().LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }
                        }
                    }
                }

                // op bomb 

                var opponentBomb = visibleBombs.FirstOrDefault(bomb => !homePlayer.IsBombOwner(bomb));

                if (opponentBomb != null)
                {
                    // op decsions
                    // if didn't compute op's
                    if (homePlayer.IsBombOwner(bombToDodge))
                    {
                        opponentLocation = state.GetPlayerLocationOnMap(opponentBomb.Owner.Key);
                        if (opponentLocation != null)
                        {
                            opponentBombs = state.GetPlayerBombs(opponentBomb.Owner.Key);
                            opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                            if (opponentVisibleBombs != null)
                            {
                                opponentSafeBlocks = FindSafeBlocks(state, opponentBomb.Owner, opponentLocation, opponentVisibleBombs);
                            }
                        }
                    }

                    if (opponentVisibleBombs == null || opponentSafeBlocks != null)
                    {
                        var mapSafeBlock = FindSafeBlockFromPlayer(state, homePlayer, homePlayerLocation, visibleBombs, opponentBomb);

                        if (mapSafeBlock != null)
                        {
                            if (opponentSafeBlocks != null)
                            {
                                // can clear safe bomb or rather reach safe block before op triggers
                                if (mapSafeBlock.Distance <= opponentSafeBlocks.First().Distance + 1)
                                {
                                    // emergency trigger
                                    Move move;
                                    var ownBombs = state.GetPlayerBombs(homePlayerKey);

                                    if (ownBombs != null && !visibleBombs.Any(b => b == ownBombs[0]))
                                    {
                                        // check if we are clearing the correct bomb
                                        var bombsToClear = BotHelper.FindVisibleBombs(state, mapSafeBlock.Location);

                                        if (bombsToClear != null && bombsToClear.Any(b => b == ownBombs[0]))
                                        {
                                            move = Move.TriggerBomb;
                                            GameService.WriteMove(move);
                                            return;
                                        }
                                    }
                                    // we don't have any safe bomb to clear, so just grab the location
                                    move = GetMoveFromLocation(homePlayerLocation, mapSafeBlock.LocationToBlock);
                                    GameService.WriteMove(move);
                                    return;
                                }
                            }
                            else if (opponentVisibleBombs == null)
                            {
                                // we are in real danger, so no time to clear any bomb
                                var move = GetMoveFromLocation(homePlayerLocation, mapSafeBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }
                        }
                    }
                }
            }

            // Trigger bomb
            var homePlayerBombs = state.GetPlayerBombs(homePlayerKey);
            if (visibleBombs == null
                && homePlayerBombs != null
                && homePlayerBombs[0].BombTimer > 2)
            {
                var move = Move.TriggerBomb;
                GameService.WriteMove(move);
                return;
            }

            var nearByPowerUp = FindNearByMapPowerUpBlock(state, homePlayerLocation, homePlayerKey);

            if (nearByPowerUp != null)
            {
                // chase power up if 3 blokcs or nearer
                if (nearByPowerUp.Distance < 4)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                    GameService.WriteMove(move);
                    return;
                }

                //if bomb radius power up
                if (nearByPowerUp.PowerUP is BombRadiusPowerUp && nearByPowerUp.Distance < 16)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                    GameService.WriteMove(move);
                    return;
                }

                //if bomb radius power up
                if (nearByPowerUp.PowerUP is BombBagPowerUp
                    && nearByPowerUp.Distance < 11
                    && homePlayer.BombBag < 2)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                    GameService.WriteMove(move);
                    return;
                }

                //if super power up
                if (nearByPowerUp.PowerUP is SuperPowerUp)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                    GameService.WriteMove(move);
                    return;

                }
            }

            // Place bomb       
            IEnumerable<MapBombPlacementBlock> bombPlacementBlocks = null;

            var visibleWalls = BotHelper.FindVisibleWalls(state, homePlayerLocation, homePlayer);

            if (homePlayerBombs == null || homePlayerBombs.Count < homePlayer.BombBag)
            {
                Move move;

                if (visibleWalls != null)
                {
                    bombPlacementBlocks = FindBombPlacementBlocks(state, homePlayer, homePlayerLocation, oneBlockLookUp: true);

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
                                move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
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
                                move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
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
                                move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }
                        }
                    }

                    // Plant if we can find hide block after planting the bomb
                    if (visibleBombs == null && CanFindHidingBlock(state, homePlayer, homePlayerLocation))
                    {
                        move = Move.PlaceBomb;
                        GameService.WriteMove(move);
                        return;
                    }
                }

                //TODO: Attack


            }

            // Chase power up
            if (nearByPowerUp != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                GameService.WriteMove(move);
                return;
            }


            // compute bomb placement blocks
            var r = new Random();
            var maxPlacements = r.Next(5, 10);
            bombPlacementBlocks = state.PercentageWall > 10 ? FindBombPlacementBlocks(state, homePlayer, homePlayerLocation, maxPlacements) : FindBombPlacementBlocks(state, homePlayer, homePlayerLocation, 2);

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
                            var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                            GameService.WriteMove(move);
                            return;
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
                            var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                            GameService.WriteMove(move);
                            return;
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
                            var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.LocationToBlock);
                            GameService.WriteMove(move);
                            return;
                        }
                    }
                }
            }

            if (visibleWalls == null && bombPlacementBlocks != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlocks.First().LocationToBlock);
                GameService.WriteMove(move);
                return;
            }

            if (state.WallsLeft == 0)
            {
                var visiblePlayers = BotHelper.FindVisiblePlayers(state, homePlayer, homePlayerLocation);

                if (visiblePlayers != null)
                {
                    if (homePlayerBombs == null || homePlayerBombs.Count < homePlayer.BombBag)
                    {
                        // Plant if we can find hide block after planting the bomb
                        if (visibleBombs == null && CanFindHidingBlock(state, homePlayer, homePlayerLocation))
                        {
                            var move = Move.PlaceBomb;
                            GameService.WriteMove(move);
                            return;
                        }
                    }
                }

                var visiblePlayerBlock = FindPlacementBlockToDestroyPlayer(state, homePlayer, homePlayerLocation);

                if (visiblePlayerBlock != null)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, visiblePlayerBlock.LocationToBlock);
                    GameService.WriteMove(move);
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
                return loc.Y > playerLoc.Y ? Move.MoveDown : Move.MoveUp;
            }

            if (loc.Y == playerLoc.Y)
            {
                return loc.X > playerLoc.X ? Move.MoveRight : Move.MoveLeft;
            }

            return Move.DoNothing;
        }

        private bool CanFindHidingBlock(GameState state, Player player, Location startLoc)
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

                var safeNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location);

                if (safeNode != null && safeNode.FCost < bombTimer)
                {
                    var visibleBombs = BotHelper.FindVisibleBombs(state, qNode.Location);

                    if (visibleBombs == null)
                    {
                        if (qNode.Location.X != startLoc.X && qNode.Location.Y != startLoc.Y) return true;

                        var blockDistance = qNode.Location.X == startLoc.X ? Math.Abs(qNode.Location.Y - startLoc.Y) : Math.Abs(qNode.Location.X - startLoc.X);

                        if (blockDistance > blastRadius) return true;
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
                if (oneBlockLookUp && searchCount < 1)
                {
                    return bombPlacementBlocks.Count == 0 ? null : bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                                                                      .ThenBy(b => b.PowerDistance)
                                                                                      .ThenByDescending(b => b.VisibleWalls)
                                                                                      .ThenBy(b => b.Distance);
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

                        var mapNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location);

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
                                SuperDistance = state.SuperLocation == null ? 0 : BotHelper.BuildPathToTarget(state, qNode.Location, state.SuperLocation, super: true).FCost
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
            var bomb = bombsToDodge.First();

            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } }; //To be expanded
            var closedSet = new HashSet<MapNode>();          // Expanded and visited

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(node => node.GCost).First();

                openSet.Remove(qNode);
                closedSet.Add(qNode);

                MapNode safeNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

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
                            SuperDistance = state.SuperLocation == null ? 0 : state.SuperLocation == null ? 0 : BotHelper.BuildPathToTarget(state, qNode.Location, state.SuperLocation, super: true).FCost,
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
                    var mapNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location);

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

                MapNode safeNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location, player, bombsToDodge, stayClear: true);

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

                var blockNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location);

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

            var opponentLocations = new List<Location>();

            state.Players.FindAll(p => (p.Key != playerKey && !p.Killed))
                 .ForEach(p => opponentLocations.Add(new Location(p.Location.X - 1, p.Location.Y - 1)));

            var openSet = new HashSet<MapNode> { new MapNode { Location = startLoc } };
            var closedSet = new HashSet<MapNode>();

            MapNode qNode;

            while (openSet.Count != 0)
            {
                qNode = openSet.OrderBy(n => n.GCost).First();

                var mapEntity = state.GetBlockAtLocation(qNode.Location).PowerUp;

                if (mapEntity != null)
                {
                    var mapNode = BotHelper.BuildPathToTarget(state, startLoc, qNode.Location);

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
    }
}