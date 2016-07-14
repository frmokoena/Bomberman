using BomberBot.Business.Helpers;
using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using BomberBot.Interfaces;
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
            var state = GameService.GameState;
            var homePlayerKey = GameService.HomeKey;
            var homePlayer = state.Players.Find(p => p.Key == homePlayerKey);
            var homePlayerLocation = state.FindPlayerLocationOnMap(homePlayerKey);

            //Player killed
            if (homePlayerLocation == null) return;

            // Stay clear
            var bombs = BotHelper.FindBombsInLOS(state, homePlayerLocation);

            if (bombs != null)
            {
                var safeBlock = FindSafeBlock(state, homePlayerLocation);

                if (safeBlock != null)
                {
                    var safeBlockOnMap = BotHelper.BuildPathToTarget(state, homePlayerLocation, safeBlock, stayClear: true);
                    if (safeBlockOnMap != null)
                    {
                        var nextLocationToSafeBlock = BotHelper.RecontractPath(homePlayerLocation, safeBlockOnMap);
                        var move = GetMoveFromLocation(homePlayerLocation, nextLocationToSafeBlock);
                        GameService.WriteMove(move);
                        return;
                    }
                }
            }

            // Chase power up
            var nearByPowerUp = FindNearByPowerUp(state, homePlayerLocation);

            if (nearByPowerUp != null && nearByPowerUp.NextMove != null)
            {
                var move = GetMoveFromLocation(homePlayerLocation, nearByPowerUp.NextMove);
                GameService.WriteMove(move);
                return;
            }

            // Trigger bomb

            List<Bomb> playerBombs = state.GetPlayerBombs(homePlayerKey);

            if (playerBombs != null && playerBombs[0].BombTimer > 2)
            {
                var move = Move.TriggerBomb;
                GameService.WriteMove(move);
                return;
            }


            // Place bomb

            if (playerBombs == null || playerBombs.Count < playerBombs[0].Owner.BombBag)
            {
                var walls = BotHelper.FindWallsInLOS(state, homePlayerLocation, homePlayer);

                if (walls != null)
                {
                    var move = Move.PlaceBomb;
                    GameService.WriteMove(move);
                    return;
                }

                // search for placement block
                Location bombPlantBlock = FindBombPlacementBlock(state, homePlayerLocation, homePlayer);

                if (bombPlantBlock != null)
                {
                    var bombPlantLocationOnMap = BotHelper.BuildPathToTarget(state, homePlayerLocation, bombPlantBlock);

                    if (bombPlantLocationOnMap != null)
                    {
                        var nextLocationToPlant = BotHelper.RecontractPath(homePlayerLocation, bombPlantLocationOnMap);

                        var move = GetMoveFromLocation(homePlayerLocation, nextLocationToPlant);
                        GameService.WriteMove(move);
                        return;
                    }
                }
            }

            GameService.WriteMove(Move.DoNothing);
        }

        private Location FindBombPlacementBlock(GameState state, Location startLoc, Player player)
        {
            var openList = new List<Location>() { startLoc };
            var closedList = new List<Location>();
            var visited = new List<Location>();
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList.First();
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlocksLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlocksLoc)
                {
                    if (!visited.Contains(loc))
                    {
                        var walls = BotHelper.FindWallsInLOS(state, loc, player);
                        if (walls != null)
                        {
                            return loc;
                        }

                        visited.Add(loc);
                    }

                    if (!closedList.Contains(loc))
                    {
                        openList.Add(loc);
                    }
                }
            }
            return null;
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
        public List<MapPowerUp> FindMapPowerUps(GameState state, Location startLoc)
        {
            var mapPowerUps = new List<MapPowerUp>();

            var openList = new List<Location> { startLoc };//To be expanded
            var closedList = new List<Location>();       //Expanded
            var visitedList = new List<Location>();      //checked
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList.First();
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlockLoc = BotHelper.ExpandMoveBlocks(state, startLoc, qLoc);

                foreach (var loc in possibleBlockLoc)
                {
                    if (!visitedList.Contains(loc))
                    {
                        if (state.IsPowerUp(loc))
                        {
                            var mapNode = BotHelper.BuildPathToTarget(state, startLoc, loc);
                            mapPowerUps.Add(new MapPowerUp { Location = loc, Distance = mapNode == null ? 0 : mapNode.FCost, NextMove = BotHelper.RecontractPath(startLoc, mapNode) });
                        }
                        visitedList.Add(loc);
                    }

                    if (!closedList.Contains(loc))
                    {
                        openList.Add(loc);
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
        public MapPowerUp FindNearByPowerUp(GameState state, Location curLoc)
        {
            var mapPowerUps = FindMapPowerUps(state, curLoc);

            if (mapPowerUps == null) return null;

            bool foundPowerUp;
            var oponents = state.Players.Where(p => p.Key != GameService.HomeKey && !p.Killed).ToList();

            foreach (var powerUp in mapPowerUps)
            {
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
        private bool PlayerCanReachPowerUp(Player player, MapPowerUp powerUp, GameState state)
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
        /// <param name="curLoc"></param>
        /// <returns>Block which is bomb clear</returns>
        public Location FindSafeBlock(GameState state, Location curLoc)
        {
            var openList = new List<Location> { curLoc };//To be expanded
            var closedList = new List<Location>();       //Expanded
            var visitedList = new List<Location>();      //checked
            Location qLoc;

            while (openList.Count != 0)
            {
                qLoc = openList.First();
                openList.Remove(qLoc);
                closedList.Add(qLoc);

                var possibleBlockLos = BotHelper.ExpandSafeBlocks(state, qLoc);

                foreach (var loc in possibleBlockLos)
                {
                    if (!visitedList.Contains(loc))
                    {
                        var bombsInLos = BotHelper.FindBombsInLOS(state, loc);

                        if (bombsInLos == null)
                        {
                            // TODO: easier to plant???
                            return loc;
                        }

                        visitedList.Add(loc);
                    }

                    if (!closedList.Contains(loc))
                    {
                        openList.Add(loc);
                    }
                }
            }
            return null;
        }
    }
}