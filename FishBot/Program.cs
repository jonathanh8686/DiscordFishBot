using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace FishBot
{
    internal class Program
    {
        public static Dictionary<IGuild, DataStorage> variables = new Dictionary<IGuild, DataStorage>();

        private AuditLog _auditLog;
        private DiscordSocketClient _client;
        private CommandHandler _cmdHandler;

        private static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Main problem start
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            await Log(new LogMessage(LogSeverity.Verbose, "", "Bot Started..."));
            Console.ForegroundColor = ConsoleColor.White;

            Config.EnsureExists();
            AuditLog.EnsureExists();

            var botdata = Config.Load();

            // Check if the .json file contains "BotPrefix"
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 2000
            });

            _client.Log += Log;
            _client.GuildAvailable += GuildAvailable;

            _auditLog = new AuditLog();
            _auditLog.Mount(_client);

            // Allow problem to use Auditing

            _cmdHandler = new CommandHandler();
            await _cmdHandler.InstallCommands(_client);
            // Inserts commands into bot

            await Login(botdata);

            await Task.Delay(-1);
        }

        private async Task GuildAvailable(SocketGuild g)
        {
            variables.Add(g, new DataStorage());
        }

        /// <summary>
        ///     Login and Connect to the Discord
        /// </summary>
        /// <param name="botdata">Config class containing data about the bot</param>
        private async Task Login(Config botdata)
        {
            await Log(new LogMessage(LogSeverity.Info, "", "BotToken: " + botdata.BotToken + " located!"));
            Console.WriteLine("");

            try
            {
                await _client.LoginAsync(TokenType.Bot, botdata.BotToken);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                // Will throw "401: Unauthorized" if the BotToken is invalid
                await Log(new LogMessage(LogSeverity.Critical, "", ex.Message));
                await Log(new LogMessage(LogSeverity.Error, "", "Possibly invalid token?"));
            }
        }

        /// <summary>
        ///     Logs data to the Console
        /// </summary>
        /// <param name="msg">LogMessage object to describe log data</param>
        public static Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Info:
                    Print("INFO: " + msg.Message, ConsoleColor.Cyan);
                    break;
                case LogSeverity.Verbose:
                    Print("VERBOSE: " + msg.Message, ConsoleColor.Green);
                    break;
                case LogSeverity.Debug:
                    Print("DEBUG: " + msg.Message, ConsoleColor.DarkGreen);
                    break;
                case LogSeverity.Warning:
                    Print("WARN: " + msg.Message, ConsoleColor.Yellow);
                    break;
                case LogSeverity.Error:
                    Print("ERROR: " + msg.Message, ConsoleColor.DarkRed);
                    break;
                case LogSeverity.Critical:
                    Print("CRITICAL: " + msg.Message, ConsoleColor.Red);
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Simple print data to Console
        /// </summary>
        /// <param name="msg">String to print to the Console</param>
        /// <param name="color">Color of print</param>
        public static void Print(string msg, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now + "\t" + msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static string GetMiddle(string data, string begin, string end)
        {
            int beginPosition = data.IndexOf(begin, StringComparison.Ordinal);
            if (beginPosition < 0) return "";
            int valueEnd = data.IndexOf(end, beginPosition + begin.Length, StringComparison.Ordinal);
            if (valueEnd > beginPosition + begin.Length)
                return data.Substring(beginPosition + begin.Length, valueEnd - beginPosition + begin.Length).Trim();
            return "";
        }

        public static List<string> GetMiddleList(string data, string begin, string end)
        {
            data = data.Replace("\n", "");
            data = data.Replace("\r", "");
            string pattern = begin + ".*?" + end;
            pattern = pattern.Replace("(", "\\(");
            var matches = Regex.Matches(data, pattern);
            return (from Match nextOne in matches
                select nextOne.Value
                into strTemp
                select GetMiddle(strTemp, begin, end).Replace("&amp; ", "")).ToList();
        }

        public static string StripHTML(string htmlString)
        {
            htmlString = htmlString.Replace("\r", "");
            htmlString = htmlString.Replace("\n", "");
            htmlString = htmlString.Replace("\\", "");
            return Regex.Replace(htmlString, @"<(.|\n)*?>", string.Empty);
        }
    }
}