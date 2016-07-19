﻿using BomberBot.Business.Helpers;
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
    public class Strategy
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
            Player homePlayer = state.Players.Find(p => p.Key == homePlayerKey);
            Location homePlayerLocation = state.FindPlayerLocationOnMap(homePlayerKey);
            int maxBombBlast = state.MapWidth > state.MapHeight ? state.MapWidth - 3 : state.MapHeight - 3;

            //Player killed
            if (homePlayerLocation == null) return;

            // Update procedure
            // 1. Stay clear of bombs
            // 2. Triger bomb
            // 3. Chase power up if near than 3 blokcs
            // 4. Plant plant bomb
            // 5. Chase after power up
            // 6. Search for the placementbomb or plant bomb
            //

            // Stay clear
            var visibleBombs = BotHelper.FindVisibleBombs(state, homePlayerLocation);

            if (visibleBombs != null)
            {
                var bombToDodge = visibleBombs[0];

                var safeBlocks = FindSafeBlocks(state, homePlayer, homePlayerLocation, bombToDodge);

                if (safeBlocks != null)
                {
                    var chainBombs = BotHelper.FindVisibleBombs(state, new Location(bombToDodge.Location.X - 1, bombToDodge.Location.Y - 1), chaining: true);

                    var safeBlocksInPriority = chainBombs == null ? safeBlocks.OrderByDescending(block => block.VisibleWalls)
                                                         .ThenBy(block => block.Distance).ToList() : safeBlocks;

                    var opponentLocation = state.FindPlayerLocationOnMap(bombToDodge.Owner.Key);
                    List<Bomb> opponentBombs = null;
                    List<Bomb> opponentVisibleBombs = null;
                    List<MapBlock> opponentSafeBlocks = null;

                    if (opponentLocation != null)
                    {
                        opponentBombs = state.GetPlayerBombs(bombToDodge.Owner.Key);
                        opponentVisibleBombs = BotHelper.FindVisibleBombs(state, opponentLocation);

                        if (opponentVisibleBombs != null)
                        {
                            opponentSafeBlocks = FindSafeBlocks(state, bombToDodge.Owner, opponentLocation, opponentVisibleBombs[0]);
                        }
                    }

                    foreach (var safeBlock in safeBlocksInPriority)
                    {
                        if (bombToDodge.Owner.Key == homePlayerKey)
                        {
                            var move = GetMoveFromLocation(homePlayerLocation, safeBlock.NextMove);
                            GameService.WriteMove(move);
                            return;
                        }
                        else
                        {
                            if (opponentLocation == null)
                            {
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlock.NextMove);
                                GameService.WriteMove(move);
                                return;
                            }

                            if (bombToDodge.BombTimer > opponentBombs[0].BombTimer)
                            {
                                var move = GetMoveFromLocation(homePlayerLocation, safeBlock.NextMove);
                                GameService.WriteMove(move);
                                return;
                            }

                            // Need rework here, otherwise we might get stranded
                            if (opponentVisibleBombs != null)
                            {

                                //if we can reach our safe block before op
                                if (opponentSafeBlocks == null || safeBlock.Distance <= opponentSafeBlocks[0].Distance)
                                {
                                    var move = GetMoveFromLocation(homePlayerLocation, safeBlock.NextMove);
                                    GameService.WriteMove(move);
                                    return;
                                }

                                // TODO: we might clear away from dangerous bomb well in time
                                var maxSearch = opponentSafeBlocks[0].Distance;
                                var searchLocations = GetRouteLocations(safeBlock.MapNode);

                                for (var i = 0; i < maxSearch; i++)
                                {
                                    var bombsToDodge = BotHelper.FindVisibleBombs(state, searchLocations[i]);
                                    if (bombsToDodge == null || !bombsToDodge.Contains(bombToDodge))
                                    {
                                        var move = GetMoveFromLocation(homePlayerLocation, safeBlock.NextMove);
                                        GameService.WriteMove(move);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Trigger bomb
            var playerBombs = state.GetPlayerBombs(homePlayerKey);
            if (visibleBombs == null
                && playerBombs != null
                && playerBombs[0].BombTimer > 2)
            {
                var move = Move.TriggerBomb;
                GameService.WriteMove(move);
                return;
            }

            // chase power up if 3 blokcs or nearer
            var nearByPowerUp = FindNearByPowerUp(state, homePlayer, homePlayerLocation, maxBombBlast);

            if (nearByPowerUp != null)
            {
                if (nearByPowerUp.Distance < 4)
                {
                    var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.NextMove);
                    GameService.WriteMove(move);
                    return;
                }
            }


            // Place bomb       
            List<MapBlock> bombPlacementBlocks = null;
            bool computeBombPlacementBlocks = true;

            var walls = BotHelper.FindVisibleWalls(state, homePlayerLocation, homePlayer);

            if (walls != null)
            {
                Move move;

                if (playerBombs == null || playerBombs.Count < playerBombs[0].Owner.BombBag)
                {
                    if (walls.Count == 1)
                    {
                        computeBombPlacementBlocks = false;
                        bombPlacementBlocks = FindBombPlacementBlocks(state, homePlayerLocation, homePlayer);

                        // if a better location in 2 blocks of nearer
                        if (bombPlacementBlocks[0].VisibleWalls > 1)
                        {
                            var bombPlacementBlock = bombPlacementBlocks.Where(b => b.VisibleWalls > 1)
                                                                        .FirstOrDefault(b => b.Distance < 2);

                            if (bombPlacementBlock != null)
                            {
                                move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlock.NextMove);
                                GameService.WriteMove(move);
                                return;
                            }
                        }
                    }

                    // Plant anyway
                    move = Move.PlaceBomb;
                    GameService.WriteMove(move);
                    return;
                }
            }

            // Chase power up
            if (nearByPowerUp != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.NextMove);
                GameService.WriteMove(move);
                return;
            }

            // search for placement block
            bombPlacementBlocks = computeBombPlacementBlocks ? FindBombPlacementBlocks(state, homePlayerLocation, homePlayer) : bombPlacementBlocks;

            if (walls == null && bombPlacementBlocks != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, bombPlacementBlocks[0].NextMove);
                GameService.WriteMove(move);
                return;
            }

            GameService.WriteMove(Move.DoNothing);
        }

        private List<MapBlock> FindBombPlacementBlocks(GameState state, Location startLoc, Player player)
        {
            var openList = new List<Location>() { startLoc };
            var closedList = new List<Location>();
            var visitedList = new List<Location>();
            var bombPlacementBlocks = new List<MapBlock>();
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlocksLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);

                        var walls = BotHelper.FindVisibleWalls(state, loc, player);

                        if (walls != null)
                        {
                            var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);
                            var mapBlock = new MapBlock
                            {
                                Location = loc,
                                Distance = mapNode == null ? Int32.MaxValue : mapNode.FCost,
                                NextMove = BotHelper.RecontractPath(mapNode),
                                VisibleWalls = walls.Count
                            };

                            //return loc;
                            bombPlacementBlocks.Add(mapBlock);
                        }

                        if (!closedList.Contains(loc))
                        {
                            openList.Add(loc);
                        }
                    }
                }
            }
            return bombPlacementBlocks.Count == 0 ? null : bombPlacementBlocks.OrderByDescending(b => b.Distance)
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
        /// Find all recheable power ups on the map
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns>List of power ups on the map</returns>
        public List<MapBlock> FindMapPowerUps(GameState state, Location startLoc)
        {
            var mapPowerUps = new List<MapBlock>();

            var openList = new List<Location> { startLoc };//To be expanded
            var closedList = new List<Location>();       //Expanded
            var visitedList = new List<Location>();      //checked
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlockLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlockLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);
                        if (state.IsPowerUp(loc))
                        {
                            var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);
                            mapPowerUps.Add(new MapBlock { Location = loc, Distance = mapNode == null ? 0 : mapNode.FCost, NextMove = BotHelper.RecontractPath(mapNode) });
                        }

                        if (!closedList.Contains(loc))
                        {
                            openList.Add(loc);
                        }
                    }
                }
            }
            return mapPowerUps.Count == 0 ? null : mapPowerUps.OrderBy(mapPowerUp => mapPowerUp.Distance).ToList();
        }

        /// <summary>
        /// Find all power ups to my advantage
        /// </summary>
        /// <param name="mapPowerUps"></param>
        /// <param name="curLoc"></param>
        /// <param name="state"></param>
        /// <returns>Power up to my advantage</returns>
        public MapBlock FindNearByPowerUp(GameState state, Player player, Location curLoc, int maxBombBlast)
        {
            var mapPowerUps = FindMapPowerUps(state, curLoc);

            if (mapPowerUps == null) return null;

            bool foundPowerUp;
            var oponents = state.Players.Where(p => p.Key != GameService.HomeKey && !p.Killed).ToList();

            foreach (var powerUp in mapPowerUps)
            {
                var powerUpKind = state.GetBlock(powerUp.Location);

                var isBombRad = powerUpKind.IsBombRadiusPowerUp();

                if (powerUpKind.IsBombRadiusPowerUp())
                {
                    if (player.BombRadius >= maxBombBlast) continue;
                }

                foundPowerUp = true;

                foreach (var oponent in oponents)
                {
                    if (PlayerCanReachPowerUp(oponent, powerUp, state))
                    {
                        foundPowerUp = false;
                        break;
                    }
                }

                if (foundPowerUp)
                {
                    return powerUp;
                }
            }
            return null;
        }


        /// <summary>
        /// Find if certain player can reach power up before home player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="powerUp"></param>
        /// <param name="state"></param>
        /// <returns>(true or false) Can opponent reach the power up before me</returns>
        private bool PlayerCanReachPowerUp(Player player, MapBlock powerUp, GameState state)
        {
            var start = state.FindPlayerLocationOnMap(player.Key);
            if (start == null) return false;

            var targetLoc = powerUp.Location;

            var oponentTarget = BotHelper.BuildPathToTarget(state, start, targetLoc);

            if (oponentTarget == null) return false;

            if (oponentTarget.FCost < powerUp.Distance)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Find safe block, running away from danger
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startLoc"></param>
        /// <returns>Block which is bomb clear</returns>
        public List<MapBlock> FindSafeBlocks(GameState state, Player player, Location startLoc, Bomb bomb)
        {
            var safeBlocks = new List<MapBlock>();

            var openList = new List<Location> { startLoc }; //To be expanded
            var closedList = new List<Location>();          //Expanded
            var visitedList = new List<Location>();         //checked
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList[0];
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlockLoc = BotHelper.ExpandSafeBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlockLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        visitedList.Add(loc);
                        MapNode safeNode = BotHelper.BuildPathToTarget(state, startLoc, loc, stayClear: true);

                        //if we can reach this location, and in time
                        if (safeNode != null && safeNode.FCost < bomb.BombTimer)
                        {
                            var visibleBombs = BotHelper.FindVisibleBombs(state, loc);

                            if (visibleBombs == null)
                            {
                                var visibleWalls = BotHelper.FindVisibleWalls(state, loc, player);

                                //add block
                                var mapBlock = new MapBlock
                                {
                                    Location = loc,
                                    Distance = safeNode.FCost,
                                    NextMove = BotHelper.RecontractPath(safeNode),
                                    VisibleWalls = visibleWalls == null ? 0 : visibleWalls.Count,
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
            return safeBlocks.Count == 0 ? null : safeBlocks;
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
    }
}