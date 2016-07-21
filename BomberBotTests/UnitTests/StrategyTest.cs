using BomberBot.Business.Helpers;
using BomberBot.Business.Strategy;
using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Enums;
using BomberBot.Interfaces;
using BomberBot.Services;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BomberBotTests.UnitTests
{
    [TestFixture]
    class StrategyTest
    {
        [Test]
        public void FindBombsInLOS()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state6";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);
            Location playerLoc = new Location(1, 1);

            var expect = new Location(1, 2);
            //Act

            var result = BotHelper.FindVisibleBombs(gameService.GameState, playerLoc);

            //Assert
            Assert.IsNotNull(gameService.GameState.GetBlock(expect).Bomb);
            Assert.AreEqual(expect.X, gameService.GameState.GetBlock(expect).Bomb.Location.X - 1);
            Assert.AreEqual(expect.Y, gameService.GameState.GetBlock(expect).Bomb.Location.Y - 1);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expect.X, result[0].Location.X - 1);
            Assert.AreEqual(expect.Y, result[0].Location.Y - 1);
        }

        [Test]
        public void FindMapPowerUpsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state7";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);

            var playerAloc = new Location(5, 3);

            //Act
            var result = bot.FindMapPowerUps(gameService.GameState, playerAloc);

            //Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FindNearByPowerUpTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state7";
            string playerKey = "C";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);

            var state = gameService.GameState;
            var playerAloc = new Location(5, 11);

            var expectLoc = new Location(6, 15);
            var expectDistance = 5;
            var expectMove = new Location(5, 12);
            var maxBombBlast = state.MapWidth > state.MapHeight ? state.MapWidth - 3:state.MapHeight - 3;
            var player = state.Players.Find(p => p.Key == playerKey);

            //Act
            var mapPowerUps = bot.FindMapPowerUps(gameService.GameState, playerAloc);
            var result = bot.FindNearByPowerUp(gameService.GameState,player, playerAloc,maxBombBlast);

            //Assert
            Assert.IsNotNull(result);

            Assert.AreEqual(expectLoc, result.Location);
            Assert.AreEqual(expectDistance, result.Distance);
            Assert.AreEqual(expectMove, result.NextMove);
        }

        [Test]
        public void NextMoveToNearByPowerUpTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state12";
            string playerKey = "D";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);



            var expect = Move.MoveLeft;

            // Act
            bot.Execute();


            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
        }

        [Test]
        public void FindSafeBlocksTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state8";
            string playerKey = "C";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);

            Location playerAloc = gameService.GameState.FindPlayerLocationOnMap(playerKey);

            var player = gameService.GameState.Players.Find(p => p.Key == playerKey);

            var bombs = BotHelper.FindVisibleBombs(gameService.GameState, playerAloc);

            var expect = new Location(5, 17);

            //Act
            var safeBlocks = bot.FindSafeBlocks(gameService.GameState, player, playerAloc, bombs[0]);

            var result = safeBlocks.OrderByDescending(b => b.VisibleWalls).ToList();
                             

            //Assert
            Assert.AreEqual(expect.X, result[0].Location.X);
            Assert.AreEqual(expect.Y, result[0].Location.Y);
        }

        [Test]
        public void WallsInLOSNotTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state9";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            var curLoc = gameService.GameState.FindPlayerLocationOnMap(playerKey);

            var player = gameService.GameState.Players.Find(p => p.Key == playerKey);


            // Act
            var result = BotHelper.FindVisibleWalls(gameService.GameState, curLoc, player);

            //Assert
            Assert.IsNull(result);
        }

        [Test]
        public void WallsInLOSTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state10";
            string playerKey = "C";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            var curLoc = gameService.GameState.FindPlayerLocationOnMap(playerKey);

            var player = gameService.GameState.Players.Find(p => p.Key == playerKey);

            var expect = 2;

            // Act
            var result = BotHelper.FindVisibleWalls(gameService.GameState, curLoc, player);

            //Assert
            Assert.AreEqual(expect, result.Count);

            Assert.AreEqual(1, result[0].Location.X - 1);
            Assert.AreEqual(11, result[0].Location.Y - 1);

            Assert.AreEqual(8, result[1].Location.X - 1);
            Assert.AreEqual(11, result[1].Location.Y - 1);

        }

        [Test]
        public void NextMoveInStayClearTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state11";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);

            var expect = Move.MoveRight;

            // Act
            bot.Execute();


            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
        }

        [Test]
        public void TriggerBombTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state13";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);



            var expect = Move.TriggerBomb;

            // Act
            bot.Execute();


            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
        }

        [Test]
        public void PlaceBombNowTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state14";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);



            var expect = Move.PlaceBomb;

            // Act
            bot.Execute();


            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
        }

        [Test]
        public void NextMoveToPlaceBombTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state15";
            string playerKey = "D";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);



            var expect = Move.MoveLeft;

            // Act
            bot.Execute();


            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
        }


        [Test]
        public void CanFindHidingBlockPriorPlantIfCantPlantTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state16";
            string playerKey = "D";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);
            var state = gameService.GameState;
            var player = state.GetPlayer(playerKey);
            var startLoc = state.FindPlayerLocationOnMap(playerKey);

            // Act
            var result = bot.CanFindHidingBlock(state, player, startLoc);

            //Assert
            Assert.IsFalse(result);            
        }

        [Test]
        public void CanFindHidingBlockPriorPlantIfCanPlantTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\Sample State Files\state17";
            string playerKey = "D";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);
            var state = gameService.GameState;
            var player = state.GetPlayer(playerKey);
            var startLoc = state.FindPlayerLocationOnMap(playerKey);

            // Act
            var result = bot.CanFindHidingBlock(state, player, startLoc);

            //Assert
            Assert.IsTrue(result);
        }



        //Read file
        private string ReadMoveFile(string workingDirectory)
        {
            var filename = Path.Combine(workingDirectory, "move.txt");

            try
            {
                string jsonText;
                using (var file = new StreamReader(filename))
                {
                    jsonText = file.ReadToEnd();
                }

                return jsonText;
            }
            catch (IOException e)
            {
                var trace = new StackTrace(e);
                return null;
            }
        }
    }
}