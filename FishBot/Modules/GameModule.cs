using System;
using System.Collections.Generic;
using System.Linq;
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
            if (variables[Context.Guild].GameInProgress) // check if game is running
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            if (variables[Context.Guild].AuthorUsers.Values.Contains(Context.User)) // check if username is taken
            {
                await ReplyAsync($"`{Context.User.Username}` has already claimed a username!");
                return;
            }

            if (!variables[Context.Guild].Players.Contains(username)) // check if username exists
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
            {
                await ReplyAsync(
                    $"`{username}` is already assigned to `{variables[Context.Guild].AuthorUsers[username]}`");
            }
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

            var newAuthorUsers = new Dictionary<string, IUser>(variables[Context.Guild].AuthorUsers);
            foreach (var user in variables[Context.Guild].AuthorUsers)
                if (user.Value == Context.User)
                    newAuthorUsers.Remove(user.Key); // unpair discord IUser to username
            variables[Context.Guild].AuthorUsers = new Dictionary<string, IUser>(newAuthorUsers);

            await ReplyAsync($"Removed all links associated with {Context.User.Username}");
        }

        [Command("start")]
        [Summary("Starts the game!")]
        public async Task Start()
        {
            if (variables[Context.Guild].GameInProgress) // check if the game is in progress
            {
                await ReplyAsync("Game is already in progess!");
                return;
            }

            //if (variables[Context.Guild].RedTeam.Count != variables[Context.Guild].BlueTeam.Count) // make sure the teams are even
            //{
            //    await ReplyAsync($"Teams are not even! Check teams using the `.team list` command");
            //    return;
            //}

            foreach (string player in variables[Context.Guild].Players) // make sure that each username is attached to a IUser
            {
                if (variables[Context.Guild].AuthorUsers.ContainsKey(player)) continue;
                await ReplyAsync($"{player} is not attached to a SocketUser!");
                return;
            }

            variables[Context.Guild].GameInProgress = true;
            variables[Context.Guild].CalledHalfSuits.Clear();

            await ReplyAsync("`Starting Game...`");
            variables[Context.Guild].PlayerTurn = variables[Context.Guild]
                .Players[new Random().Next(variables[Context.Guild].Players.Count)]; // get random player to start

            variables[Context.Guild].GameStart = true;
            await CardDealer.DealCards(Context.Guild);
            await ReplyAsync($"It's `{variables[Context.Guild].PlayerTurn}`'s turn!");
        }

        [Command("call")]
        [Summary("Allows a player to call a card from another")]
        public async Task Call(string target, string requestedCard)
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");

                await Context.Message.DeleteAsync();
                return;
            } // make sure the game is in progress

            if (variables[Context.Guild].AuthorUsers[variables[Context.Guild].PlayerTurn] != Context.Message.Author)
            {
                await Context.Message.DeleteAsync();
                return;
            } // make sure only the person's whose turn it is can call

            if (variables[Context.Guild].TeamDict[target] ==
                variables[Context.Guild].TeamDict[variables[Context.Guild].PlayerTurn])
            {
                await ReplyAsync($"Cannot call cards from someone on your team!");

                await Context.Message.DeleteAsync();
                return;
            } // make sure they call someone on the opposite team

            if (variables[Context.Guild].PlayerCards[target].Count == 0)
            {
                await ReplyAsync($"Cannot call a player with 0 cards!");

                await Context.Message.DeleteAsync();
                return;
            }

            // delete previous call info
            var rawMessages = Context.Channel.GetMessagesAsync().FlattenAsync();
            foreach (var msg in rawMessages.Result)
                if (msg.Author.Id == Context.Client.CurrentUser.Id && msg.Content.Contains("*TEMPORARY*")
                ) // everything marked with *TEMPORARY*
                    await msg.DeleteAsync();

            var req = CardDealer.GetCardByName(requestedCard.ToUpperInvariant());
            if (!variables[Context.Guild].Players.Contains(target)) // make sure the target is a valid player
            {
                await ReplyAsync($"`{target}` is not a player!");

                await Context.Message.DeleteAsync();
                return;
            }

            if (!CardDealer.CardNames.Contains(requestedCard)) // make sure requestedCard is a valid card
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
            int hsIndex = cardIndex / 6; // get index of halfsuit

            var hasHalfSuit = false;
            for (var i = 0; i < 6; i++) // loop through halfsuit and see if they have something in the same hs
                if (variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn]
                    .Contains(CardDealer.GetCardByName(CardDealer.CardNames[6 * hsIndex + i])))
                    hasHalfSuit = true;

            if (variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Contains(req) || !hasHalfSuit) // player already has the card or they don't have something in the halfsuit
            {
                builder.Color = Color.Magenta;
                builder.Description = "ILLEGAL CALL!";

                builder.AddField("Info",
                    $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` but it was **illegal**!\n It is now `{target}`'s turn.");
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

                builder.AddField("Info",
                    $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **hit**!\n It is now `{variables[Context.Guild].PlayerTurn}`'s turn.");

                variables[Context.Guild].PlayerCards[target].Remove(req);
                variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Add(req);
            }
            else
            {
                // miss
                builder.Color = Color.DarkRed;
                builder.Description = "Call was a miss!";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/FishBot/cards/miss.png";

                builder.AddField("Info",
                    $"`{variables[Context.Guild].PlayerTurn}` called the `{requestedCard}` from `{target}` and it was a **miss**!\n It is now `{target}`'s turn.");
                variables[Context.Guild].PlayerTurn = target;
            }

            await Context.Message.DeleteAsync();
            await ReplyAsync("*TEMPORARY*", false, builder.Build());
            await CardDealer.SendCards(Context.Guild);

            if (CheckPlayerTurnHandEmpty())
            {
                await ReplyAsync(
                    $"`{variables[Context.Guild].PlayerTurn}` is out of cards! Use the `.designate` command to select the next player!");
                variables[Context.Guild].NeedsDesignatedPlayer = true;
            }
        }

        [Command("callHS")]
        [Summary("Allows a player to call a halfsuit for their team")]
        public async Task CallHalfSuit(string halfsuit, string callstring)
        {
            halfsuit = halfsuit.ToUpperInvariant();

            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is not in progress yet!");
                return;
            } // make sure game is in progress

            if (!CardDealer.HalfSuitNames.Contains(halfsuit))
            {
                await ReplyAsync($"`{halfsuit}` is not a valid halfsuit!");
                return;
            } // make sure that the halfsuit called is valid

            if (variables[Context.Guild].CalledHalfSuits.Contains(halfsuit))
            {
                await ReplyAsync($"`{halfsuit}` was already called!");
                return;
            } // make sure that the halfsuit has not already been called

            var seperatedCallString = callstring.Split(" ");
            var claimedCards = new List<string>();
            var cuser = "";
            var authorName = "";

            foreach (var authorUserPair in variables[Context.Guild].AuthorUsers)
                if (authorUserPair.Value == Context.Message.Author)
                    authorName = authorUserPair.Key;

            var works = true;
            foreach (string strSeg in seperatedCallString)
            {
                if (!CardDealer.CardNames.Contains(strSeg) && !variables[Context.Guild].Players.Contains(strSeg))
                {
                    await ReplyAsync(
                        $"`{strSeg}` not recognized as a card or a player!"); // if a strSeg in the callString is not a player nor a card then it's invalid
                    return;
                }

                if (variables[Context.Guild].Players.Contains(strSeg)) // if this part of the segStr is a player
                {
                    foreach (string card in claimedCards)
                        if (!variables[Context.Guild].PlayerCards[cuser]
                            .Contains(CardDealer.GetCardByName(card.ToUpperInvariant())))
                            works = false;
                    cuser = strSeg;

                    if (variables[Context.Guild].TeamDict[cuser] !=
                        variables[Context.Guild].TeamDict[authorName])
                    {
                        await ReplyAsync($"callString included players not on your team!");
                        return;
                    }

                    claimedCards = new List<string>();
                }
                else
                {
                    if (CardDealer.CardNames.Contains(strSeg))
                        claimedCards.Add(strSeg);
                }
            }

            foreach (string card in claimedCards)
                if (!variables[Context.Guild].PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                    works = false;

            int hsindex = CardDealer.HalfSuitNames.IndexOf(halfsuit);
            for (var i = 0; i < 6; i++) // cards
                foreach (string t in variables[Context.Guild].Players)
                    variables[Context.Guild].PlayerCards[t]
                        .Remove(CardDealer.GetCardByName(CardDealer.CardNames[hsindex * 6 + i]));


            var username = "";
            foreach (var player in variables[Context.Guild].AuthorUsers)
                if (player.Value == Context.User)
                    username = player.Key;

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
                builder.Color = Color.DarkRed;
                builder.Description = $"`{username}` **missed** the `{halfsuit}`!";
                if (team == "red") variables[Context.Guild].BlueScore++;
                else variables[Context.Guild].RedScore++;
            }

            variables[Context.Guild].CalledHalfSuits.Add(halfsuit);
            builder.AddField("Score Update",
                $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");

            await ReplyAsync("", false, builder.Build());
            if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore >= 9)
            {
                variables[Context.Guild].GameInProgress = false;
                await DeclareResult();
            }

            if (CheckPlayerTurnHandEmpty())
            {
                await ReplyAsync(
                    $"{variables[Context.Guild].PlayerTurn} is out of cards! Use the `.designate` command to select the next player!");
                variables[Context.Guild].NeedsDesignatedPlayer = true;
            }
        }


        [Command("score")]
        [Summary("Shows the current score")]
        public async Task Score()
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync("Game not yet in progress!");
                return;
            }

            var builder = new EmbedBuilder();
            if (variables[Context.Guild].BlueScore > variables[Context.Guild].RedScore)
                builder.Color = Color.Blue;
            else if (variables[Context.Guild].BlueScore < variables[Context.Guild].RedScore)
                builder.Color = Color.Red;
            else
                builder.Color = Color.Purple;

            builder.AddField("Score Check",
                $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");

            await ReplyAsync("", false, builder.Build());
        }

        [Command("designate")]
        [Summary("Allows a player with no cards to designate the next player")]
        public async Task Designate(string username)
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is not yet in progress!");
                return;
            }

            if (variables[Context.Guild].NeedsDesignatedPlayer)
            {
                if (!variables[Context.Guild].Players.Contains(username))
                {
                    await ReplyAsync($"`{username}` is not a valid username!");
                    return;
                }

                variables[Context.Guild].PlayerTurn = username;
                await ReplyAsync($"It is now `{username}`'s turn!");
                variables[Context.Guild].NeedsDesignatedPlayer = false;

                if (CheckPlayerTurnHandEmpty())
                {
                    await ReplyAsync(
                        $"`{variables[Context.Guild].PlayerTurn}` is out of cards! Use the `.designate` command to select the next player!");
                    variables[Context.Guild].NeedsDesignatedPlayer = true;
                }
            }
            else
            {
                await ReplyAsync("A designated player is not needed right now!");
            }
        }

        [Command("cardcount")]
        [Summary("Tells you the number of cards a player has")]
        public async Task CardCount(string username)
        {
            await ReplyAsync($"`{username}` has **{variables[Context.Guild].PlayerCards[username].Count}** cards.");
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

            builder.AddField("Final Scores",
                $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
            await ReplyAsync("", false, builder.Build());

            variables[Context.Guild] = new DataStorage();
        }

        private bool CheckPlayerTurnHandEmpty()
        {
            return variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Count == 0;
        }
    }
}