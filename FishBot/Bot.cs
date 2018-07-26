using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace FishBot
{
    class Bot
    {
        private DiscordSocketClient _client;
        private readonly string _token;

        public Bot()
        {
            _client = new DiscordSocketClient();
            ConfigHandler.InitalizeConfigHandler();

            _client.Log += Program.Log;
        }

        public async Task Connect()
        {
            await _client.LoginAsync(TokenType.Bot, ConfigHandler.GetToken());
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
