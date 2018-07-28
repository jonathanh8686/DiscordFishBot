using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using static FishBot.Program;

namespace FishBot.Modules
{
    [Name("Game")]
    public class GameModule : ModuleBase
    {

        [Command("claim")]
        [Summary("Claim a username in the game")]
        public async Task Claim(string username)
        {
            if (variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            //if (AuthorUsers.Values.Contains(Context.User))
            //{
            //    await ReplyAsync($"`{Context.User.Username}` has already claimed a username!");
            //    return;
            //}

            if (!variables[Context.Guild].Players.Contains(username))
            {
                await ReplyAsync($"`{username}` is not a valid username!");
                return;
            }

            if (!variables[Context.Guild].AuthorUsers.ContainsKey(username))
            {
                variables[Context.Guild].AuthorUsers.Add(username, Context.User);
                await ReplyAsync($"`{username}` is now assigned to `{variables[Context.Guild].AuthorUsers[username]}`");
            }
            else
                await ReplyAsync($"`{username}` is already assigned to `{variables[Context.Guild].AuthorUsers[username]}`");
        }

        [Command("unclaim")]
        [Summary("Unclaim a username in game")]
        public async Task Unclaim()
        {
            if (variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            Dictionary<string, IUser> newAuthorUsers = new Dictionary<string, IUser>(variables[Context.Guild].AuthorUsers);
            foreach (var user in variables[Context.Guild].AuthorUsers)
            {
                if (user.Value == Context.User)
                    newAuthorUsers.Remove(user.Key);
            }
            variables[Context.Guild].AuthorUsers = new Dictionary<string, IUser>(newAuthorUsers);

            await ReplyAsync($"Removed all links associated with {Context.User.Username}");
        }

        [Command("start")]
        [Summary("Starts the game!")]
        public async Task Start()
        {
            if (variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            variables[Context.Guild].GameInProgress = true;
            variables[Context.Guild].CalledHalfSuits.Clear();

            if (variables[Context.Guild].RedTeam.Count != variables[Context.Guild].BlueTeam.Count)
            {
                await ReplyAsync($"Teams are not even! Check teams using the \".team list\" command");
                return;
            }

            foreach (string player in variables[Context.Guild].Players)
            {
                if (variables[Context.Guild].AuthorUsers.ContainsKey(player)) continue;
                await ReplyAsync($"{player} is not attached to a SocketUser!");
                return;
            }

            await ReplyAsync("`Starting Game...`");
            variables[Context.Guild].PlayerTurn = variables[Context.Guild].Players[new Random().Next(variables[Context.Guild].Players.Count)];

            variables[Context.Guild].GameStart = true;
            await CardDealer.DealCards(Context.Guild);
            await ReplyAsync($"It's `{variables[Context.Guild].PlayerTurn}`'s turn!");
        }

        [Command("call")]
        [Summary("Allows a player to call a card from another")]
        public async Task Call(string target, string requestedCard)
        {
            if (variables[Context.Guild].AuthorUsers[variables[Context.Guild].PlayerTurn] != Context.Message.Author)
            {
                await Context.Message.DeleteAsync();
                return;
            }

            if (variables[Context.Guild].TeamDict[target] == variables[Context.Guild].TeamDict[variables[Context.Guild].PlayerTurn])
            {
                await ReplyAsync($"Cannot call cards from someone on your team!");

                await Context.Message.DeleteAsync();
                return;
            }

            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");

                await Context.Message.DeleteAsync();
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
            if (!variables[Context.Guild].Players.Contains(target))
            {
                await ReplyAsync($"`{target}` is not a player!");

                await Context.Message.DeleteAsync();
                return;
            }

            if(!CardDealer.CardNames.Contains(requestedCard))
            {
                await ReplyAsync($"`{requestedCard}` is not a valid card!");

                await Context.Message.DeleteAsync();
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
                if (variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Contains(CardDealer.GetCardByName(CardDealer.CardNames[6 * hsIndex + i])))
                    hasHalfSuit = true;
            }

            if (variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Contains(req) || !hasHalfSuit) // player already has the card
            {
                builder.Color = Color.Red;
                builder.Description = "ILLEGAL CALL!";

                builder.AddField("Info", $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` but it was **illegal**! It is now `{target}`'s turn.");
                variables[Context.Guild].PlayerTurn = target;
                await ReplyAsync("*TEMPORARY*", false, builder.Build());

                await Context.Message.DeleteAsync();
                return;
            }

            if (variables[Context.Guild].PlayerCards[target].Contains(req))
            {
                // hit
                builder.Color = Color.Green;
                builder.Description = "Call was a hit!";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/hit.png";

                builder.AddField("Info", $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **hit**! It is now `{variables[Context.Guild].PlayerTurn}`'s turn.");

                variables[Context.Guild].PlayerCards[target].Remove(req);
                variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Add(req);

            }
            else
            {
                // miss
                builder.Color = Color.Red;
                builder.Description = "Call was a miss!";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/miss.png";

                builder.AddField("Info", $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **miss**! It is now `{target}`'s turn.");
                variables[Context.Guild].PlayerTurn = target;
            }

            await Context.Message.DeleteAsync();
            await ReplyAsync("*TEMPORARY*", false, builder.Build());
            await CardDealer.SendCards(Context.Guild);
        }

        [Command("callHS")]
        [Summary("Allows a player to call a halfsuit for their team")]
        public async Task CallHalfSuit(string halfsuit, string callstring)
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");
                return;
            }
            if (!CardDealer.HalfSuitNames.Contains(halfsuit))
            {
                await ReplyAsync($"`{halfsuit}` is not a valid halfsuit!");
                return;
            }
            if (variables[Context.Guild].CalledHalfSuits.Contains(halfsuit))
            {
                await ReplyAsync($"`{halfsuit}` was already called!");
                return;
            }

            var seperatedCallString = callstring.Split(" ");
            var claimedCards = new List<string>();
            string cuser = "";

            var works = true;
            foreach (string strSeg in seperatedCallString)
            {
                if (!CardDealer.CardNames.Contains(strSeg) && !variables[Context.Guild].Players.Contains(strSeg))
                {
                    await ReplyAsync($"{strSeg} not recognized as a card or a player!");
                    return;
                }

                if (variables[Context.Guild].Players.Contains(strSeg))
                {
                    foreach (string card in claimedCards)
                    {
                        if (!variables[Context.Guild].PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                            works = false;
                    }
                    cuser = strSeg;

                    if (variables[Context.Guild].TeamDict[cuser] !=
                        variables[Context.Guild].TeamDict[variables[Context.Guild].PlayerTurn])
                    {
                        await ReplyAsync($"callString included players not on your team!");
                        return;
                    }

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
                if (!variables[Context.Guild].PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                    works = false;
            }

            int hsindex = CardDealer.HalfSuitNames.IndexOf(halfsuit);
            for (var i = 0; i < 6; i++) // cards
                foreach (string t in variables[Context.Guild].Players)
                    variables[Context.Guild].PlayerCards[t].Remove(CardDealer.GetCardByName(CardDealer.CardNames[hsindex * 6 + i]));

            var username = "";
            foreach (var player in variables[Context.Guild].AuthorUsers)
            {
                if (player.Value == Context.User)
                    username = player.Key;
            }

            await CardDealer.SendCards(Context.Guild);

            string team = variables[Context.Guild].TeamDict[username];

            var builder = new EmbedBuilder {Title = "HalfSuit Call"};

            if (works)
            {
                builder.Color = Color.Green;
                builder.Description = $"`{username}` **hit** the `{halfsuit}`!";
                if (team == "red") variables[Context.Guild].RedScore++;
                else variables[Context.Guild].BlueScore++;
            }
            else
            {
                builder.Color = Color.Red;
                builder.Description = $"`{username}` **missed** the `{halfsuit}`!";
                if (team == "red") variables[Context.Guild].BlueScore++;
                else variables[Context.Guild].RedScore++;
            }

            variables[Context.Guild].CalledHalfSuits.Add(halfsuit);
            builder.AddField("Score Update", $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");

            await ReplyAsync("", false, builder.Build());

            if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore >= 9)
            {
                variables[Context.Guild].GameInProgress = false;
                await DeclareResult();
            }
        }

        [Command("reset")]
        [Summary("Resets the game")]
        public async Task Reset()
        {
            variables[Context.Guild] = new DataStorage();
            await ReplyAsync("All variables reinitalized.");
        }

        private async Task DeclareResult()
        {
            var builder = new EmbedBuilder {Title = "Game Result!"};

            if (variables[Context.Guild].RedScore > variables[Context.Guild].BlueScore)
            {
                builder.Color = Color.Red;
                builder.Description = "Red team wins!";
            }
            else
            {
                builder.Color = Color.Blue;
                builder.Description = "Blue team wins!";
            }

            builder.AddField("Final Scores", $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
            await ReplyAsync("", false, builder.Build());

            variables[Context.Guild] = new DataStorage();
        }
    }
}
