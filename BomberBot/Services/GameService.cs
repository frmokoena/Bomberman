using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Enums;
using BomberBot.Interfaces;
using BomberBot.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BomberBot.Services
{
    public class GameService : IGameService<GameState>
    {
        public string HomeKey { get; set; }
        public string WorkingDirectory { get; set; }
        public string RunDirectory { get; set; }

        public HashSet<Location> ToExploreLocations
        {
            get
            {
                return GetToExploreLocations();
            }
        }
        public GameState GameState
        {
            get
            {
                return LoadGameState();
            }
        }

        public HashSet<Location> GetToExploreLocations()
        {
            if (GameState.CurrentRound == 0)
            {
                return InitializeToExploreLocations();
            }
            return LoadToExploreLocations();
        }

        private HashSet<Location> LoadToExploreLocations()
        {
            var filename = Path.Combine(RunDirectory, Settings.Default.ToExplore);

            try
            {
                string jsonText;
                using (var file = new StreamReader(filename))
                {
                    jsonText = file.ReadToEnd();
                }
                var toExplore = JsonConvert.DeserializeObject<HashSet<Location>>(jsonText);
                return toExplore;
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to read state file: {0}", filename));
                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
                return null;
            }
        }

        private HashSet<Location> InitializeToExploreLocations()
        {
            var width = GameState.MapWidth;
            var height = GameState.MapHeight;
            var state = GameState;
            var toExplore = new HashSet<Location>();

            var toSave = Path.Combine(RunDirectory, Settings.Default.ToExplore);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (!state.IsIndestructibleWall(x, y)) toExplore.Add(new Location(x, y));
                }
            }
            File.WriteAllText(toSave, JsonConvert.SerializeObject(toExplore.ToArray()));
            return toExplore;
        }

        public GameService(string key, string workingDirectory, string runDirectory)
        {
            HomeKey = key;
            WorkingDirectory = workingDirectory;
            RunDirectory = runDirectory;
        }

        public void UpdateToExploreLocations(Location loc)
        {
            var toExploreLocations = ToExploreLocations;
            bool removed = toExploreLocations.Remove(loc);

            if (removed)
            {
                var toSave = Path.Combine(RunDirectory, Settings.Default.ToExplore);
                File.WriteAllText(toSave, JsonConvert.SerializeObject(toExploreLocations.ToArray()));
            }

        }
        private GameState LoadGameState()
        {
            var jsonText = ReadGameStateFile();
            var gameState = JsonConvert.DeserializeObject<GameState>(jsonText,
               new JsonSerializerSettings
               {
                   Converters = { new EntityConverter() },
                   NullValueHandling = NullValueHandling.Ignore
               });
            return gameState;
        }

        private string ReadGameStateFile()
        {
            var filename = Path.Combine(WorkingDirectory, Settings.Default.StateFile);

            try
            {
                string jsonText;
                using (var file = new StreamReader(filename))
                {
                    jsonText = file.ReadToEnd();
                    jsonText = jsonText.Replace("\"$type\"", "\"type\"");
                }

                return jsonText;
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to read state file: {0}", filename));
                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
                return null;
            }
        }

        private void Log(string message)
        {
            Console.WriteLine("[BOT]\t{0}", message);
        }

        public void WriteMove(Move move)
        {
            var moveInt = (int)move;
            var filename = Path.Combine(WorkingDirectory, Settings.Default.OutputFile);

            try
            {
                using (var file = new StreamWriter(filename))
                {
                    file.WriteLine(moveInt);
                }

                Log("Command: " + move);
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to write command file: {0}", filename));

                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
            }
        }

        public static EntityType GetEntityType(string jsonTypeTxt)
        {
            var entityType = Regex.Match(jsonTypeTxt, @"(?:\.)(\w*)(?:,)").Groups[1].ToString();
            var result = ParseEnum<Enums.EntityType>(entityType);
            return result;
        }

        private static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}