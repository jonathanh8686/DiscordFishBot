using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace FishBot
{
    /// <summary>
    ///     Main class for handling commands etc.
    /// </summary>
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _cmds;

        private IServiceProvider _services;

        public async Task InstallCommands(DiscordSocketClient c)
        {
            _client = c;
            _cmds = new CommandService();


            _services = new ServiceCollection().BuildServiceProvider();

            await _cmds.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return;
            if (msg.Author.IsBot) return;

            var context = new CommandContext(_client, msg);

            if (!Program.variables.ContainsKey(context.Guild))
            {
                await context.Channel.SendMessageAsync("Guild not recognized! **Tell Jonathan!!!!!**");
                return;
            }

            var argPos = 0;
            if (msg.HasStringPrefix(Config.Load().BotPrefix, ref argPos) ||
                msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await Program.Log(new LogMessage(LogSeverity.Info, "", msg.Author + ": \"" + msg.Content + "\""));
                var result = await _cmds.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess) await context.Channel.SendMessageAsync(result.ToString());
            }
            else
            {
                await Program.Log(new LogMessage(LogSeverity.Verbose, "", msg.Author + ": \"" + msg.Content + "\""));
            }
        }
    }
}