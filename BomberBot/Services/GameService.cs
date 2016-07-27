using BomberBot.Domain.Model;
using BomberBot.Enums;
using BomberBot.Interfaces;
using BomberBot.Properties;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace BomberBot.Services
{
    public class GameService : IGameService<GameState>
    {
        public string HomeKey { get; set; }
        public string WorkingDirectory { get; set; }

        public GameState GameState
        {
            get
            {
                return LoadGameState();
            }
        }

        public GameService(string key, string workingDirectory)
        {
            HomeKey = key;
            WorkingDirectory = workingDirectory;
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