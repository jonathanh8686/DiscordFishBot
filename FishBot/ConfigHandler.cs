using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Discord;
using FishBot;
using Newtonsoft.Json;

namespace FishBot
{
    public class ConfigObject
    {
        [JsonProperty("Token")] public string Token { get; set; }
    }

    internal static class ConfigHandler
    {
        private const string ConfigPath = "Config/config.json";
        private static ConfigObject _jsonObj;

        public static void InitalizeConfigHandler()
        {
            if (!File.Exists(ConfigPath))
            {
                Program.Log(new LogMessage(LogSeverity.Critical, "ConfigHandler", "No config.json file found!"));
                Program.Log(new LogMessage(LogSeverity.Info, "ConfigHandler", "Creating config.json file..."));

                if(!Directory.Exists("Config"))
                    Directory.CreateDirectory("Config");
                File.Create(ConfigPath);
            }
            else
            {
                _jsonObj = JsonConvert.DeserializeObject<ConfigObject>(File.ReadAllText(ConfigPath));
                Program.Log(new LogMessage(LogSeverity.Info, "ConfigHandler", "Found Config Data"));
            }
        }

        public static string GetToken()
        {
            return _jsonObj.Token;
        }
    }
}
