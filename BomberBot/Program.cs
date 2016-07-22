using BomberBot.Business.Strategy;
using BomberBot.Domain.Model;
using BomberBot.Interfaces;
using BomberBot.Services;
using System;
using System.Diagnostics;
using System.IO;

namespace BomberBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            RunBot(args);

            stopwatch.Stop();
            var elaps = stopwatch.ElapsedMilliseconds;         
            Console.WriteLine("[BOT]\tBot finished in {0} ms.", stopwatch.ElapsedMilliseconds);

        }

        private static void RunBot(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                Environment.Exit(1);
            }

            var workingDirectory = args[1];
            if (!Directory.Exists(workingDirectory))
            {
                PrintUsage();
                Console.WriteLine();
                Console.WriteLine("Error: Working directory \"" + workingDirectory + "\" does not exist.");
                Environment.Exit(1);
            }

            var playerKey = args[0];
            if (playerKey.Length != 1)
            {
                PrintUsage();
                Console.WriteLine();
                Console.WriteLine("Error: Player Key should be anything from A-L");
                Environment.Exit(1);
            }


            IGameService<GameState> gameService = new GameService(playerKey, workingDirectory);

            Strategy bot = new Strategy(gameService);
            bot.Execute();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Bomberman bot usage: BomberBot.exe <PlayerKey> <WorkingDirectoryFilename>");
            Console.WriteLine();
            Console.WriteLine("\tPlayerKey\tThe key assigned to the bomberman bot.");
            Console.WriteLine("\tWorkingDirectoryFilename\tThe working directory folder where the match runner will output map and state files and look for the move file.");
        }
    }
}