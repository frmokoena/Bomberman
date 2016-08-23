using BomberBot.Business.Strategy;
using BomberBot.Domain.Model;
using BomberBot.Enums;
using BomberBot.Interfaces;
using BomberBot.Services;
using BomberBotTests.Properties;
using NUnit.Framework;
using System.IO;

namespace BomberBotTests.UnitTests
{
    [TestFixture]
    class StrategyTest
    {
        [Test]
        public void StrikeFalseOpponentWhenHeIsBusyTryingToEscapeBombsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\0";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreNotEqual((int)expectMove, result);                           
        }

        [Test]
        public void StrikeTrueOpponentWhenHeIsBusyTryingToEscapeBombsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\1";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void AnotherStrikeTrueOpponentWhenHeIsBusyTryingToEscapeBombsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\2";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void StrikeTrueOpponentWhenHeIsBusyTryingToEscapeBombsAndMoveInToMyViewTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\3";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void AnotherStrikeTrueOpponentWhenHeIsBusyTryingToEscapeBombsAndMoveInToMyViewTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\4";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void EscapeViaOpponentBombTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\5";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.MoveUp;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void EscapeMyMultipleBombsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\6";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.MoveRight;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void EscapeCriticalBombsScenarioTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\7";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.TriggerBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void AnotherEscapeCriticalBombsScenarioTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\8";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "C";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.DoNothing;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void SecondBombPlantTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\9";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void ThirdBombPlantTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\10";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void PlantFalseInMultipleBombsToThreatenPlayerTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\11";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreNotEqual((int)expectMove, result);
        }

        [Test]
        public void PlantTrueInMultipleBombsToThreatenPlayerTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\12";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.PlaceBomb;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void YetAnotherEscapeCriticalBombsScenarioTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\13";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.DoNothing;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        [Test]
        public void UnusualEscapeFailTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\14";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data";
            string playerKey = "B";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);

            Strategy bot = new Strategy(gameService);

            var expectMove = Move.MoveLeft;

            //Act 
            bot.Execute();
            var result = ReadMove(workingDirectory);

            // Assert
            Assert.AreEqual((int)expectMove, result);
        }

        // Read move
        private int ReadMove(string workingDirectory)
        {
            
            var filename = Path.Combine(workingDirectory, Settings.Default.OutputFile );
            int moveInt;
            try
            {
                string moveCommand;
                using (var file = new StreamReader(filename))
                {
                    moveCommand = file.ReadToEnd();                    
                }

                if(int.TryParse(moveCommand, out moveInt))
                {
                    return moveInt;
                }
                else
                {
                    return -1;
                }                
            }
            catch
            {                
                return -1;
            }
        }
    }
}