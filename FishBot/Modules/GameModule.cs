using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FishBot.Modules
{
    [Name("Game")]
    public class GameModule : ModuleBase
    {
        public static bool GameStart;
        public static List<string> Players = new List<string>();
        public static Dictionary<string, List<Card>> PlayerCards = new Dictionary<string, List<Card>>();
        public static Dictionary<string, IUser> AuthorUsers = new Dictionary<string, IUser>();
        public static string PlayerTurn;

        public static bool GameInProgress;

        public static int RedScore;
        public static int BlueScore;


        [Command("claim")]
        [Summary("Claim a username in the game")]
        public async Task Claim(string username)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            if (AuthorUsers.Values.Contains(Context.User))
            {
                await ReplyAsync($"`{Context.User.Username}` has already claimed a username!");
                return;
            }

            if (!AuthorUsers.ContainsKey(username))
            {
                AuthorUsers.Add(username, Context.User);
                await ReplyAsync($"`{username}` is now assigned to `{AuthorUsers[username]}`");
            }
            else
                await ReplyAsync($"`{username}` is already assigned to `{AuthorUsers[username]}`");
        }

        [Command("start")]
        [Summary("Starts the game!")]
        public async Task Start()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            GameInProgress = true;
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

            await ReplyAsync("`Starting Game...`");
            PlayerTurn = Players[new Random().Next(Players.Count)];

            GameStart = true;
            await CardDealer.DealCards();
            await ReplyAsync($"It's `{PlayerTurn}`'s turn!");
        }

        [Command("call")]
        [Summary("Allows a player to call a card from another")]
        public async Task Call(string target, string requestedCard)
        {
            if (!GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");
                return;
            }
            
            // delete previous call info
            var rawMessages = Context.Channel.GetMessagesAsync().FlattenAsync();
            foreach (var msg in rawMessages.Result)
            {
                if (msg.Author.Id == Context.Client.CurrentUser.Id && msg.Content.Contains("*TEMPORARY*"))
                    await msg.DeleteAsync();
            }

            var req = CardDealer.GetCardByName(requestedCard);
            if (!Players.Contains(target))
            {
                await ReplyAsync($"`{target}` is not a player!");
                return;
            }

            if(!CardDealer.CardNames.Contains(requestedCard))
            {
                await ReplyAsync($"`{requestedCard}` is not a valid card!");
                return;
            }

            var builder = new EmbedBuilder
            {
                Title = "Call Result",
                ImageUrl = "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/" +
                           req.CardName + ".png"
            };

            int cardIndex = CardDealer.CardNames.IndexOf(requestedCard);
            int hsIndex = cardIndex / 6;

            var hasHalfSuit = false;
            for (var i = 0; i < 6; i++)
            {
                if (PlayerCards[PlayerTurn].Contains(CardDealer.GetCardByName(CardDealer.CardNames[6 * hsIndex + i])))
                    hasHalfSuit = true;
            }

            if (PlayerCards[PlayerTurn].Contains(req) || !hasHalfSuit) // player already has the card
            {
                builder.Color = Color.Red;
                builder.Description = "ILLEGAL CALL!";

                builder.AddField("Info", $"`{PlayerTurn}` called the `{requestedCard}` from `{target}` but it was **illegal**! It is now `{target}`'s turn.");
                PlayerTurn = target;
                await ReplyAsync("*TEMPORARY*", false, builder.Build());

                return;
            }

            if (PlayerCards[target].Contains(req))
            {
                // hit
                builder.Color = Color.Green;
                builder.Description = "Call was a hit!";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/hit.png";

                builder.AddField("Info", $"`{PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **hit**! It is now `{PlayerTurn}`'s turn.");

                PlayerCards[target].Remove(req);
                PlayerCards[PlayerTurn].Add(req);

            }
            else
            {
                // miss
                builder.Color = Color.Red;
                builder.Description = "Call was a miss!";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/miss.png";

                builder.AddField("Info", $"`{PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **miss**! It is now `{target}`'s turn.");
                PlayerTurn = target;
            }

            await ReplyAsync("*TEMPORARY*", false, builder.Build());

            await Context.Message.DeleteAsync();
            await CardDealer.SendCards();
        }

        [Command("callHS")]
        [Summary("Allows a player to call a halfsuit for their team")]
        public async Task CallHalfSuit(string halfsuit, string callstring)
        {
            if (!GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");
                return;
            }

            var seperatedCallString = callstring.Split(" ");
            var claimedCards = new List<string>();
            string cuser = "";

            var works = true;
            foreach (string strSeg in seperatedCallString)
            {
                if (Players.Contains(strSeg))
                {
                    foreach (string card in claimedCards)
                    {
                        if (!PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                            works = false;
                    }
                    cuser = strSeg;
                    claimedCards = new List<string>();
                }
                else
                {
                    if(CardDealer.CardNames.Contains(strSeg))
                        claimedCards.Add(strSeg);
                }
            }
            foreach (string card in claimedCards)
            {
                if (!PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                    works = false;
            }

            int hsindex = CardDealer.HalfSuitNames.IndexOf(halfsuit);
            for (var i = 0; i < 6; i++) // cards
                foreach (string t in Players)
                    PlayerCards[t].Remove(CardDealer.GetCardByName(CardDealer.CardNames[hsindex * 6 + i]));

            var username = "";
            foreach (var player in AuthorUsers)
            {
                if (player.Value == Context.User)
                    username = player.Key;
            }

            await CardDealer.SendCards();

            string team = TeamModule.TeamDict[username];

            var builder = new EmbedBuilder {Title = "HalfSuit Call"};

            if (works)
            {
                builder.Color = Color.Green;
                builder.Description = $"`{username}` **hit** the `{halfsuit}`!";
                if (team == "red") RedScore++;
                else BlueScore++;
            }
            else
            {
                builder.Color = Color.Red;
                builder.Description = $"`{username}` **missed** the `{halfsuit}`!";
                if (team == "red") BlueScore++;
                else RedScore++;
            }
            builder.AddField("Score Update", $"Blue Team: {BlueScore}\n Red Team: {RedScore}");

            await ReplyAsync("", false, builder.Build());

            if (RedScore + BlueScore >= 9)
            {
                GameInProgress = false;
                await DeclareResult();
            }
        }

        private async Task DeclareResult()
        {
            var builder = new EmbedBuilder();
            builder.Title = "Game Result!";

            if (RedScore > BlueScore)
            {
                builder.Color = Color.Red;
                builder.Description = "Red team wins!";
            }
            else
            {
                builder.Color = Color.Blue;
                builder.Description = "Blue team wins!";
            }

            builder.AddField("Final Scores", $"Blue Team: {BlueScore}\n Red Team: {RedScore}");
            await ReplyAsync("", false, builder.Build());
        }
    }
}
