using System;
using System.IO;
using Newtonsoft.Json;

namespace FishBot
{
    public class Config
    {
        public static string FilePath { get; } = "config/configuration.json";

        public string BotToken { get; set; }
        public string BotPrefix { get; set; }

        public static void EnsureExists()
        {
            if (!File.Exists(FilePath))
            {
                Program.Print(FilePath + " not found!", ConsoleColor.DarkRed);
                string path = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var con = new Config();

                Program.Print("Please enter BotToken: ");
                string bottoken = Console.ReadLine();

                Program.Print("Please enter Prefix: ");
                string botprefix = Console.ReadLine();

                con.BotToken = bottoken;
                con.BotPrefix = botprefix;

                con.Save();
            }

            Program.Print("Configuration Loaded", ConsoleColor.Green);
        }

        public void Save()
            => File.WriteAllText(FilePath, ToJson());

        public static Config Load()
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));
        }

        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}