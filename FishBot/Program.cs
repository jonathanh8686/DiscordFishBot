using Discord;
using System;
using System.Threading.Tasks;

namespace FishBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            await Log(new LogMessage(LogSeverity.Verbose, String.Empty, "MainAsync Method entered"));
            Bot fishBot = new Bot();
        }

        public static Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg.Message);
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
