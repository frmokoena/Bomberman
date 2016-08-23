using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Interfaces;
using BomberBot.Services;
using NUnit.Framework;

namespace BomberBotTests.UnitTests
{
    [TestFixture]
    class GameServiceTest
    {
        [Test]
        public void InitialToExploreLocationsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\15";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data\initial";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);
                      

            var expectToExploreLocations = 280;

            //Act 
            var result = gameService.BlocksToExplore;

            // Assert
            Assert.AreEqual(expectToExploreLocations,result.Count);
        }

        [Test]
        public void LoadToExploreLocationsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\16";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data\load";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory,runDirectory);


            var expectToExploreLocations = 281;

            //Act 
            var result = gameService.BlocksToExplore;

            // Assert
            Assert.AreEqual(expectToExploreLocations, result.Count);
        }

        [Test]
        public void UpdateToExploreLocationsTest()
        {
            //Arrange
            string workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\16";
            string runDirectory = TestContext.CurrentContext.TestDirectory + @"\data\edit";
            string playerKey = "A";
            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory, runDirectory);

            var toRemove = new Location(1, 5);
            
            var expectAfterRemove = 280;

            //Act           

            gameService.UpdateBlocksToExplore(toRemove);          

            var result = gameService.BlocksToExplore;
            
            // Assert
            Assert.AreEqual(expectAfterRemove, result.Count);
        }
    }
}
