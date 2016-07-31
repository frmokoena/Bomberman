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

        public Strategy(IGameService<GameState> gameServie)
        {
            GameService = gameServie;
        }

        /// <summary>
        /// Public API
        /// </summary>
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
                var bombToDodge = visibleBombs[0];


                Location opponentLocation = null;
                List<Bomb> opponentBombs = null;
                List<Bomb> opponentVisibleBombs = null;
                List<MapSafeBlock> opponentSafeBlocks = null;


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
                    var ownBomb = visibleBombs.Find(bomb => bomb.Owner.Key == homePlayerKey);

                    bool playerVisible = ownBomb == null ? false : BotHelper.IsAnyPlayerVisible(state, ownBomb);


                    var chainBombs = BotHelper.FindVisibleBombs(state, new Location(bombToDodge.Location.X - 1, bombToDodge.Location.Y - 1), chaining: true);

                    var findNearestHiding = chainBombs != null || playerVisible;

                    var prioritySafeBlocks = findNearestHiding ? safeBlocks : safeBlocks.OrderBy(block => block.SuperDistance)
                                                                                        .ThenByDescending(block => block.VisibleWalls)
                                                                                        .ThenBy(block => block.Distance)
                                                                                        .ToList();

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
                                if (opponentSafeBlocks == null || safeBlock.Distance <= opponentSafeBlocks[0].Distance)
                                {
                                    var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                    GameService.WriteMove(move);
                                    return;
                                }

                                // We might clear away from dangerous bomb well in time
                                var maxSearch = opponentSafeBlocks[0].Distance;
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
                                if (safeBlock.Distance <= opponentSafeBlocks[0].Distance + 1)
                                {
                                    var move = GetMoveFromLocation(homePlayerLocation, safeBlock.LocationToBlock);
                                    GameService.WriteMove(move);
                                    return;
                                }
                            }
                            else
                            {
                                // else just take the closet safe block
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlocks[0].LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }
                        }
                    }
                }

                // op bomb 
                if (!homePlayer.IsBombOwner(bombToDodge))
                {
                    var mapSafeBlock = FindSafeBlockFromPlayer(state, homePlayer, homePlayerLocation, visibleBombs);

                    if (mapSafeBlock != null)
                    {
                        if (opponentSafeBlocks != null)
                        {
                            if (mapSafeBlock.Distance <= opponentSafeBlocks[0].Distance + 1)
                            {
                                // grab the location
                                var move = GetMoveFromLocation(homePlayerLocation, mapSafeBlock.LocationToBlock);
                                GameService.WriteMove(move);
                                return;
                            }

                            if (mapSafeBlock.Distance < 2)
                            {
                                // grab the location
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


            var nearByPowerUp = FindNearByMapPowerUpBlock(state, homePlayerLocation);

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

                //if bomb is super power up
                if (nearByPowerUp.PowerUP is SuperPowerUp)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                    GameService.WriteMove(move);
                    return;
                }
            }

            // Place bomb       
            List<MapBombPlacementBlock> bombPlacementBlocks = null;
            int maxPlacements;
            var computePlacements = true;

            var visibleWalls = BotHelper.FindVisibleWalls(state, homePlayerLocation, homePlayer);

            if ((homePlayerBombs == null || homePlayerBombs.Count < homePlayer.BombBag) && visibleWalls != null)
            {
                Move move;

                maxPlacements = state.PercentageWall > 10 ? 5 : 1;
                bombPlacementBlocks = FindBombPlacementBlocks(state, homePlayer, homePlayerLocation, 5);
                computePlacements = false;
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

            // Chase power up
            if (nearByPowerUp != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.LocationToBlock);
                GameService.WriteMove(move);
                return;
            }

            // search for placement block
            if (computePlacements)
            {
                maxPlacements = state.PercentageWall > 10 ? 5 : 1;
                bombPlacementBlocks = FindBombPlacementBlocks(state, homePlayer, homePlayerLocation, maxPlacements);
            }
            

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
                var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlocks[0].LocationToBlock);
                GameService.WriteMove(move);
                return;
            }

            if (state.WallsLeft == 0)
            {
                if (BotHelper.IsAnyPlayerVisible(state, homePlayer, homePlayerLocation))
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

            //well, we didn't get any good decision
            GameService.WriteMove(Move.DoNothing);
        }

        private bool CanFindHidingBlock(GameState state, Player player, Location startLoc)
        {
            var blastRadius = player.BombRadius;
            var bombTimer = Math.Min(9, (player.BombBag * 3)) + 1;

            var openList = new List<Location> { startLoc };
            var closeList = new List<Location>();
            var visitedList = new List<Location>();


            while (openList.Count != 0)
            {
                var qLoc = openList[0];
                openList.RemoveAt(0);
                closeList.Add(qLoc);

                var possibleBlocks = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);


                foreach (var loc in possibleBlocks)
                {
                    if (!visitedList.Contains(loc))
                    {
                        var safeNode = BotHelper.BuildPathToTarget(state, startLoc, loc);

                        if (safeNode != null && safeNode.FCost < bombTimer)
                        {
                            var visibleBombs = BotHelper.FindVisibleBombs(state, loc);

                            if (visibleBombs == null)
                            {
                                if (loc.X != startLoc.X && loc.Y != startLoc.Y) return true;

                                var blockDistance = loc.X == startLoc.X ? Math.Abs(loc.Y - startLoc.Y) : Math.Abs(qLoc.X - startLoc.X);

                                if (blockDistance > blastRadius) return true;

                                if (!closeList.Contains(loc))
                                {
                                    openList.Add(loc);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private List<MapBombPlacementBlock> FindBombPlacementBlocks(GameState state, Player player, Location startLoc, int maxPlacementBlocks)
        {
            var openList = new List<Location>() { startLoc };
            var closedList = new List<Location>();
            var visitedList = new List<Location>();
            List<List<DestructibleWall>> destroyedWalls = new List<List<DestructibleWall>>();

            var bombPlacementBlocks = new List<MapBombPlacementBlock>();
            Location qLoc;

            while (openList.Count != 0)
            {

                if (bombPlacementBlocks.Count > maxPlacementBlocks)
                {
                    return bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                              .ThenByDescending(b => b.VisibleWalls)
                                              .ThenBy(b => b.Distance)
                                              .ToList();
                }

                qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlocksLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);

                        var visibleWalls = BotHelper.FindVisibleWalls(state, loc, player);

                        if (visibleWalls != null)
                        {
                            if (!WallsDestroyed(destroyedWalls, visibleWalls))
                            {
                                destroyedWalls.Add(visibleWalls);

                                var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);
                                if (mapNode != null)
                                {
                                    var mapBlock = new MapBombPlacementBlock
                                    {
                                        Location = loc,
                                        Distance = mapNode.FCost,
                                        LocationToBlock = BotHelper.ReconstructPath(mapNode),
                                        VisibleWalls = visibleWalls.Count,
                                        SuperDistance = state.SuperLocation == null ? 0 : BotHelper.BuildPathToTarget(state, loc, state.SuperLocation, super: true).FCost
                                    };

                                    bombPlacementBlocks.Add(mapBlock);
                                }
                            }
                        }

                        if (!closedList.Contains(loc))
                        {
                            openList.Add(loc);
                        }
                    }
                }
            }
            return bombPlacementBlocks.Count == 0 ? null : bombPlacementBlocks.OrderBy(b => b.SuperDistance)
                                                                              .ThenByDescending(b => b.VisibleWalls)
                                                                              .ThenBy(b => b.Distance)
                                                                              .ToList();
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

        /// <summary>
        /// Find safe block, running away from danger
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startLoc"></param>
        /// <returns>Block which is bomb clear</returns>
        private List<MapSafeBlock> FindSafeBlocks(GameState state, Player player, Location startLoc, List<Bomb> bombsToDodge)
        {
            var safeBlocks = new List<MapSafeBlock>();

            var openList = new List<Location> { startLoc }; //To be expanded
            var closedList = new List<Location>();          //Expanded
            var visitedList = new List<Location>();         //checked


            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleBlockLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc, player, bombsToDodge, stayClear: true);

                foreach (var loc in possibleBlockLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);
                        MapNode safeNode = BotHelper.BuildPathToTarget(state, startLoc, loc, player, bombsToDodge, stayClear: true);

                        //if we can reach this location, and in time
                        var bomb = bombsToDodge[0];
                        if (safeNode != null && safeNode.FCost < bomb.BombTimer)
                        {
                            var visibleBombs = BotHelper.FindVisibleBombs(state, loc);

                            if (visibleBombs == null)
                            {
                                var visibleWalls = BotHelper.FindVisibleWalls(state, loc, player);

                                //add block
                                var mapBlock = new MapSafeBlock
                                {
                                    Location = loc,
                                    Distance = safeNode.FCost,
                                    LocationToBlock = BotHelper.ReconstructPath(safeNode),
                                    VisibleWalls = visibleWalls == null ? 0 : visibleWalls.Count,
                                    SuperDistance = state.SuperLocation == null ? 0 : state.SuperLocation == null ? 0 : BotHelper.BuildPathToTarget(state, loc, state.SuperLocation, super: true).FCost,
                                    MapNode = safeNode
                                };
                                safeBlocks.Add(mapBlock);

                            }

                            if (!closedList.Contains(loc))
                            {
                                openList.Add(loc);
                            }
                        }
                    }
                }
            }
            return safeBlocks.Count == 0 ? null : safeBlocks.OrderBy(block => block.Distance).ToList();
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
            var openList = new List<Location> { startLoc };
            var closedList = new List<Location>();
            var visitedList = new List<Location>();

            Location qLoc;


            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlocksLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        if (BotHelper.IsAnyPlayerVisible(state, player, loc))
                        {
                            var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);

                            if (mapNode != null)
                            {
                                return new MapBombPlacementBlock
                                {
                                    Location = loc,
                                    LocationToBlock = BotHelper.ReconstructPath(mapNode)
                                };
                            }
                        }

                        visitedList.Add(loc);

                        if (!closedList.Contains(loc))
                        {
                            openList.Add(loc);
                        }
                    }
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

        private MapSafeBlock FindSafeBlockFromPlayer(GameState state, Player player, Location startLoc, List<Bomb> bombsToDodge)
        {
            var openList = new List<Location> { startLoc };
            var closedList = new List<Location>();
            var visitedList = new List<Location>();

            while (openList.Count != 0)
            {
                var qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleMoveLocations = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc, player, bombsToDodge, stayClear: true);

                foreach (var loc in possibleMoveLocations)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);

                        MapNode safeNode = BotHelper.BuildPathToTarget(state, startLoc, loc, player, bombsToDodge, stayClear: true);

                        var bomb = bombsToDodge[0];
                        if (safeNode != null && safeNode.FCost < bomb.BombTimer)
                        {
                            var visibleBombs = BotHelper.FindVisibleBombs(state, loc);

                            if (visibleBombs == null)
                            {
                                return new MapSafeBlock
                                {
                                    Location = loc,
                                    Distance = safeNode.FCost,
                                    LocationToBlock = BotHelper.ReconstructPath(safeNode),
                                    MapNode = safeNode
                                };
                            }

                            var bombToDodge = visibleBombs.Find(b => b == bomb);

                            if (bombToDodge == null)
                            {

                                return new MapSafeBlock
                                {
                                    Location = loc,
                                    Distance = safeNode.FCost,
                                    LocationToBlock = BotHelper.ReconstructPath(safeNode),
                                    MapNode = safeNode
                                };
                            }

                            if (!closedList.Contains(loc))
                            {
                                openList.Add(loc);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private bool IsBlockInPlayerRange(GameState state, Location startLoc, Location targetLoc, int range)
        {
            var openList = new List<Location> { startLoc };
            var closedList = new List<Location>();
            var visitedList = new List<Location>();

            while (openList.Count != 0)
            {
                var qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleMovesLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleMovesLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        var blockNode = BotHelper.BuildPathToTarget(state, startLoc, loc);

                        if (blockNode != null && blockNode.FCost < range)
                        {
                            if (loc.Equals(targetLoc)) return true;

                            if (state.IsPowerUp(loc)) return false;

                            visitedList.Add(loc);

                            if (!closedList.Contains(loc))
                            {
                                openList.Add(loc);
                            }
                        }
                    }
                }
            }
            return false;
        }



        private MapPowerUpBlock FindNearByMapPowerUpBlock(GameState state, Location startLoc)
        {

            var opponentLocations = new List<Location>();

            state.Players.Where(p => p.Key != GameService.HomeKey && !p.Killed)
                         .ToList()
                         .ForEach(p => opponentLocations.Add(new Location(p.Location.X - 1, p.Location.Y - 1)));

            var openList = new List<Location> { startLoc };  //To be expanded
            var closedList = new List<Location>();           //Expanded
            var visitedList = new List<Location>();          //checked
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.RemoveAt(0);
                closedList.Add(qLoc);

                var possibleMovesLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleMovesLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        var mapEntity = state.GetBlockAtLocation(loc).PowerUp;

                        if (mapEntity != null)
                        {
                            var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);

                            if (mapNode != null)
                            {
                                var foundPowerUpBlock = true;

                                foreach (var playerLoc in opponentLocations)
                                {
                                    if (IsBlockInPlayerRange(state, playerLoc, loc, mapNode.FCost))
                                    {
                                        foundPowerUpBlock = false;
                                        break;
                                    }
                                }

                                if (foundPowerUpBlock)
                                {
                                    return new MapPowerUpBlock
                                    {
                                        Location = loc,
                                        Distance = mapNode.FCost,
                                        LocationToBlock = BotHelper.ReconstructPath(mapNode),
                                        PowerUP = mapEntity
                                    };
                                }
                            }
                        }

                        visitedList.Add(loc);
                        if (!closedList.Contains(loc))
                        {
                            openList.Add(loc);
                        }
                    }
                }
            }
            return null;
        }
    }
}