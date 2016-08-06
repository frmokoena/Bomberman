using BomberBot.Business.Helpers;
using BomberBot.Common;
using BomberBot.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace BomberBotTests.UnitTests
{
    [TestFixture]
    class MapTraversalTest
    {
        [Test]
        public void ReturnAvailableLocationsAroundAPlayerTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state2";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var state = gameService.GameState;
            var expectLocs = new List<Location>()
            {
                new Location(3, 2),
                new Location(4, 3),
                new Location(3, 4),
                new Location(2, 3)

            };

            //Act
            var actualLocs = BotHelper.ExpandMoveBlocks(state, new Location(3, 3), new Location(3, 3));

            for (var i = 0; i < expectLocs.Count; i++)
            {
                Assert.AreEqual(expectLocs[i], actualLocs[i]);
            }
        }

        [Test]
        public void SelectSimpleRouteToPowerUpTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state2";
            var playerKey = "C";

            var gameService = new GameService(playerKey, workingDirectory);

            var state = gameService.GameState;

            var expectLoc = new Location(4, 15);

            //Act
            var actualLoc = BotHelper.FindPathToTarget(state, new Location(3, 15), new Location(5, 17));


            //Assert
            Assert.IsNull(actualLoc);
        }

        //[Test]
        public void CanReturnBombsInLOS()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state4";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var state = gameService.GameState;

            var expectBombs = new List<Location> { new Location(19, 3) };

            //Act
            var actualBombs = BotHelper.FindVisibleBombs(state, new Location(19, 1)).ToList();

            //Assert
            for (var i = 0; i < expectBombs.Count; i++)
            {
                Assert.AreEqual(expectBombs[i], actualBombs[i]);
            }
        }

        [Test]
        public void MaxMapDistanceTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "B";

            var gameService = new GameService(playerKey, workingDirectory);

            var state = gameService.GameState;
            var expectDistance = 2*state.MaxBombBlast;
            var startLoc = new Location(1, 1);
            var targetLoc = new Location(19, 19);

            //Act
            var result = BotHelper.FindPathToTarget(state, startLoc, targetLoc);

            //Assert
            Assert.AreEqual(expectDistance, result.FCost);
            
        }
    }
}