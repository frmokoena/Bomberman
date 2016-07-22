using BomberBot.Common;
using BomberBot.Domain.Objects;
using BomberBot.Enums;
using BomberBot.Services;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace BomberBotTests.UnitTests
{
    [TestFixture]
    class GameServiceTest
    {

        [SetUp]
        public void TestInit()
        {

        }

        [Test]
        public void ActualSimpleVariablesReturnedTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expectRound = 31;
            var expectBounty = 310;
            var expectHeight = 21;
            var expectWidth = 21;
            //Act
            var state = gameService.GameState;

            //Assert
            Assert.AreEqual(expectRound, state.CurrentRound);
            Assert.AreEqual(expectBounty, state.PlayerBounty);
            Assert.AreEqual(expectHeight, state.MapHeight);
            Assert.AreEqual(expectWidth, state.MapWidth);
        }

        [Test]
        public void ReadRegisteredPlayerEntitiesTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expectName = "Lazy Bomba";
            var expectKey = "D";
            var expectPoints = -310;
            var expectBombBag = 1;
            var expectBombRadius = 1;
            var expectloc = new Location(20, 2);

            //Act
            var state = gameService.GameState;
            var players = state.Players;

            //Assert
            Assert.AreEqual(players.Count, 4);

            Assert.AreEqual(players[2].Points, 52);

            Assert.AreEqual(expectName, players[3].Name);
            Assert.AreEqual(expectKey, players[3].Key);
            Assert.AreEqual(expectPoints, players[3].Points);
            Assert.AreEqual(expectBombBag, players[3].BombBag);
            Assert.AreEqual(expectBombRadius, players[3].BombRadius);
            Assert.AreEqual(expectloc, players[3].Location);

            Assert.IsTrue(players[3].Killed);
        }

        [Test]
        public void ReadTheFirstGameBlockTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expectLocation = new Location(1, 1);

            //Act
            var state = gameService.GameState;
            var map = state.Map;

            //Assert
            Assert.AreEqual(1, map[0][0].Location.X);
            Assert.AreEqual(1, map[0][0].Location.Y);

            Assert.IsNull(map[0][0].Bomb);
            Assert.IsNull(map[0][0].PowerUp);

            Assert.IsFalse(map[0][0].Exploding);

            Assert.AreEqual(EntityType.IndestructibleWallEntity, map[0][0].Entity.Type);
        }

        [Test]
        public void ReturnCorrectTypeTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expectType = EntityType.BombBagPowerUpEntity;

            var state = gameService.GameState;
            var map = state.Map;

            //Act
            var actual = map[3][18].PowerUp.Type;

            //Assert
            Assert.AreEqual(expectType, actual);
        }

        [Test]
        public void GetPlayerSittingOnBombTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state2";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expectTimer = 7;
            var expectRadius = 2;
            var expectOwnerPoints = 73;

            //act
            var state = gameService.GameState;
            var map = state.Map;

            var player = map[3][15].Entity;
            var bomb = map[3][15].Bomb;

            //Assert
            Assert.IsInstanceOf<Player>(player);

            Assert.AreEqual(expectTimer, bomb.BombTimer);
            Assert.AreEqual(expectRadius, bomb.BombRadius);
            Assert.IsFalse(bomb.IsExploding);
            Assert.IsFalse(bomb.Owner.Killed);
            Assert.AreEqual(expectOwnerPoints, bomb.Owner.Points);
        }

        [Test]
        public void ReturnExplodingBombTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);


            //Act
            var state = gameService.GameState;

            var explodingBomb = state.Map[17][16];

            //Assert
            Assert.IsTrue(explodingBomb.Exploding);
        }

        [Test]
        public void ReturnPlantedBombTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);


            //Act
            var state = gameService.GameState;

            var plantedBomb = state.Map[3][17].Bomb;

            //Assert
            Assert.IsNotNull(plantedBomb);
            Assert.IsFalse(plantedBomb.IsExploding);
        }

        [Test]
        public void PerformEqualityInLocationsTest()
        {
            //Arrange
            var p1 = new Location(1, 1);
            var p2 = new Location(1, 1);

            var h1 = p1.GetHashCode();
            var h2 = p2.GetHashCode();

            //Act

            //Assert
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
            Assert.IsFalse(p1 != p2);

            Assert.AreEqual(h1, h2);
            Assert.AreEqual(p1, p2);
        }

        [Test]
        public void SpotIfBlockEmptyTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);


            //Act
            var state = gameService.GameState;

            var notEmptyBlock = state.Map[0][0];
            var anotherNotsoEmpty = state.Map[16][17];

            var emptyBlock = state.Map[1][2];

            //Assert
            Assert.IsFalse(notEmptyBlock.IsEmpty());
            Assert.IsFalse(anotherNotsoEmpty.IsEmpty());
            Assert.IsTrue(emptyBlock.IsEmpty());
        }

        [Test]
        public void GetEntityOccupyingTheBlockTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\state1";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);
                        

            //Act
            var map = gameService.GameState.Map;

            var block = map[17][17];           

            //Assert
            Assert.IsTrue(block.IsBombExploding());            
        }

        [Test]
        public void WriteMoveTest()
        {
            //Arrange
            var workingDirectory = TestContext.CurrentContext.TestDirectory + @"\states\stateMove";
            var playerKey = "A";

            var gameService = new GameService(playerKey, workingDirectory);

            var expect = Move.PlaceBomb;

            //Act
            gameService.WriteMove(expect);

            var moveTxt = ReadMoveFile(workingDirectory);

            int result;
            if (Int32.TryParse(moveTxt, out result)) { }

            //Assert
            Assert.AreEqual((int)expect, result);
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