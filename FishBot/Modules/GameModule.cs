using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FishBot.Modules
{
    [Name("Game")]
    public class GameModule : ModuleBase
    {
        public static bool GameStart;
        public static List<string> Players = new List<string>();
        public static Dictionary<string, List<Card>> PlayerCards = new Dictionary<string, List<Card>>();
        public static Dictionary<string, IUser> AuthorUsers = new Dictionary<string, IUser>();

        [Command("claim")]
        [Summary("Claim a username in the game")]
        public async Task Claim(string username)
        {
            if(!AuthorUsers.ContainsKey(username))
                AuthorUsers.Add(username, Context.User);
            else
            {
                await ReplyAsync($"{username} is already assigned to {AuthorUsers[username]}");
            }
        }

        [Command("start")]
        [Summary("Starts the game!")]
        public async Task Start()
        {
            if (TeamModule.RedTeam.Count != TeamModule.BlueTeam.Count)
            {
                await ReplyAsync($"Teams are not even! Check teams using the \".team list\" command");
                return;
            }

            foreach (string player in Players)
            {
                if (AuthorUsers.ContainsKey(player)) continue;
                await ReplyAsync($"{player} is not attached to a SocketUser!");
                return;
            }

            GameStart = true;
            await CardDealer.DealCards();

        }
    }
}
