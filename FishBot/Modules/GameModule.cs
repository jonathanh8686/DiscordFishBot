using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using static FishBot.Program;

namespace FishBot.Modules
{
    [Name("Game")]
    public class GameModule : ModuleBase
    {
        //[Command("claim")]
        //[Summary("Claim a username in the game")]
        //public async Task Claim(string username)
        //{
        //    if (variables[Context.Guild].GameInProgress) // check if game is running
        //    {
        //        if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
        //        {
        //            await ReplyAsync(
        //                ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
        //            return;
        //        }
        //        await ReplyAsync(":x: Game is already in progess! :x:");
        //        return;
        //    }

        //    if (variables[Context.Guild].AuthorUsers.Values.Contains(Context.User)) // check if username is taken
        //    {
        //        await ReplyAsync($":x: `{Context.User.Username}` has already claimed a username! :x:");
        //        return;
        //    }

        //    if (!variables[Context.Guild].Players.Contains(username)) // check if username exists
        //    {
        //        await ReplyAsync($":x: `{username}` is not a valid username! :x:");
        //        return;
        //    }

        //    if (!variables[Context.Guild].AuthorUsers.ContainsKey(username)) // link it
        //    {
        //        variables[Context.Guild].AuthorUsers.Add(username, Context.User);
        //        await ReplyAsync($":link: `{username}` is now assigned to `{variables[Context.Guild].AuthorUsers[username]}` :link:");
        //    }
        //    else
        //    {
        //        await ReplyAsync(
        //            $":x: `{username}` is already assigned to `{variables[Context.Guild].AuthorUsers[username]}` :x:");
        //    }
        //}

        //[Command("unclaim")]
        //[Summary("Unclaim a username in game")]
        //public async Task Unclaim()
        //{
        //    if (variables[Context.Guild].GameInProgress)
        //    {
        //        if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
        //        {
        //            await ReplyAsync(
        //                ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
        //            return;
        //        }
        //        await ReplyAsync(":x: Game is already in progess! :x:");
        //        return;
        //    }

        //    var newAuthorUsers = new Dictionary<string, IUser>(variables[Context.Guild].AuthorUsers);
        //    foreach (var user in variables[Context.Guild].AuthorUsers)
        //        if (user.Value == Context.User)
        //            newAuthorUsers.Remove(user.Key); // unpair discord IUser to username
        //    variables[Context.Guild].AuthorUsers = new Dictionary<string, IUser>(newAuthorUsers);

        //    await ReplyAsync($":white_check_mark: Removed all links associated with {Context.User.Username}");
        //} // old claiming code

        [Command("start")]
        [Summary("Starts the game!")]
        public async Task Start()
        {
            if (variables[Context.Guild].GameInProgress) // check if the game is in progress
            {
                if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
                {
                    await ReplyAsync(
                        ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
                    return;
                }
                await ReplyAsync(":trophy: Game is already in progess! :trophy:");
                return;
            }

            if (variables[Context.Guild].Players.Count == 0)
            {
                await ReplyAsync(":x: There are not enough players!! :x:");
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
                await ReplyAsync($":x: <@{player}> is not attached to a SocketUser! :x:");
                return;
            }

            variables[Context.Guild].GameInProgress = true;
            variables[Context.Guild].CalledHalfSuits.Clear();

            //Create AFN header
            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].Players.Count;
            variables[Context.Guild].AlgebraicNotation += "{" + variables[Context.Guild].RedTeam.Count + "|";
            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].BlueTeam.Count +"}";
            foreach (string redPlayer in variables[Context.Guild].RedTeam)
            {
                variables[Context.Guild].AlgebraicNotation +=
                    redPlayer + "[" + variables[Context.Guild].AuthorUsers[redPlayer] + "]";
            }

            foreach (string bluePlayer in variables[Context.Guild].BlueTeam)
            {
                variables[Context.Guild].AlgebraicNotation +=
                    bluePlayer + "[" + variables[Context.Guild].AuthorUsers[bluePlayer] + "]";
            }

            // Starting game
            await ReplyAsync(":trophy: `Starting Game...` :weary:");
            variables[Context.Guild].PlayerTurn = variables[Context.Guild]
                .Players[new Random().Next(variables[Context.Guild].Players.Count)]; // get random player to start

            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].PlayerTurn; // add first player to afn

            variables[Context.Guild].GameStart = true;
            await CardDealer.DealCards(Context.Guild);
            await ReplyAsync($":game_die: It's <@{variables[Context.Guild].PlayerTurn}>'s turn!");
        }

        [Command("call")]
        [Alias("ask")]
        [Summary("Allows a player to call a card from another")]
        public async Task Call(string target, string requestedCard)
        {
            target = target.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");

            if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
            {
                await ReplyAsync(
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
                return;
            }

            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not in progress yet! :x:");

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
                await ReplyAsync($":x: Cannot call cards from someone on your team! :x:");

                await Context.Message.DeleteAsync();
                return;
            } // make sure they call someone on the opposite team

            if (variables[Context.Guild].PlayerCards[target].Count == 0)
            {
                await ReplyAsync($":x: Cannot call a player with no cards! :x:");

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
                await ReplyAsync($":x: `{target}` is not a player! :x:");

                await Context.Message.DeleteAsync();
                return;
            }

            if (!CardDealer.CardNames.Contains(requestedCard)) // make sure requestedCard is a valid card
            {
                await ReplyAsync($":x: `{requestedCard}` is not a valid card! :x:");

                await Context.Message.DeleteAsync();
                return;
            }

            var builder = new EmbedBuilder
            {
                Title = "Call Result",
                ImageUrl = "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/cards/" +
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
                await Context.Message.DeleteAsync();
                return;
                //variables[Context.Guild].AlgebraicNotation += $"call {target} {requestedCard} illegal;";

                //builder.Color = Color.Magenta;
                //builder.Description = ":oncoming_police_car: ILLEGAL CALL! :oncoming_police_car:";

                //builder.AddField("Info",
                //    $"<@{variables[Context.Guild].PlayerTurn}> called the `{requestedCard}` from <@{target}> but it was **illegal**!\n It is now <@{target}>'s turn.");
                //variables[Context.Guild].PlayerTurn = target;
                //await ReplyAsync("*TEMPORARY*", false, builder.Build());

                //await Context.Message.DeleteAsync();
                //return;
            }

            if (variables[Context.Guild].PlayerCards[target].Contains(req))
            {
                // hit
                variables[Context.Guild].AlgebraicNotation += $"call {target} {requestedCard} hit;";

                builder.Color = Color.Green;
                builder.Description = ":boom: Call was a hit! :boom:";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/cards/hit.png";

                builder.AddField("Info",
                    $"<@{variables[Context.Guild].PlayerTurn}> called the `{requestedCard}` from <@{target}> and it was a **hit**!\n It is now <@{variables[Context.Guild].PlayerTurn}>'s turn.");

                variables[Context.Guild].PlayerCards[target].Remove(req);
                variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Add(req);
            }
            else
            {
                // miss
                variables[Context.Guild].AlgebraicNotation += $"call {target} {requestedCard} miss;";

                builder.Color = Color.DarkRed;
                builder.Description = ":thinking: Call was a miss! :thinking:";
                builder.ThumbnailUrl =
                    "https://raw.githubusercontent.com/jonathanh8686/DiscordFishBot/master/cards/miss.png";

                builder.AddField("Info",
                    $"<@{variables[Context.Guild].PlayerTurn}> called the `{requestedCard}` from <@{target}> and it was a **miss**!\n It is now <@{target}>'s turn.");
                variables[Context.Guild].PlayerTurn = target;
            }

            await Context.Message.DeleteAsync();
            await ReplyAsync("*TEMPORARY*", false, builder.Build());
            await CardDealer.SendCards(Context.Guild);

            if (CheckPlayerTurnHandEmpty())
            {
                await ReplyAsync(
                    $"<@{variables[Context.Guild].PlayerTurn}> is out of cards! Use the `.designate` command to select the next player!");
                variables[Context.Guild].NeedsDesignatedPlayer = true;
            }
        }

        [Command("callHS")]
        [Summary("Allows a player to call a halfsuit for their team")]
        public async Task CallHalfSuit(string halfsuit, string callstring)
        {
            callstring = callstring.Trim();

            halfsuit = halfsuit.ToUpperInvariant();

            if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
            {
                await ReplyAsync(
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
                return;
            }

            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not in progress yet! :x:");
                return;
            } // make sure game is in progress

            if (!CardDealer.HalfSuitNames.Contains(halfsuit))
            {
                await ReplyAsync($":x: `{halfsuit}` is not a valid halfsuit! :x:");
                return;
            } // make sure that the halfsuit called is valid

            if (variables[Context.Guild].CalledHalfSuits.Contains(halfsuit))
            {
                await ReplyAsync($":x: `{halfsuit}` was already called! :x:");
                return;
            } // make sure that the halfsuit has not already been called

            var seperatedCallString = callstring.Split(" ");
            var claimedCards = new List<string>();
            var allClaimed = new List<string>();
            var cuser = "";
            var authorName = "";

            foreach (var authorUserPair in variables[Context.Guild].AuthorUsers)
                if (authorUserPair.Value == Context.Message.Author)
                    authorName = authorUserPair.Key;

            var works = true;
            foreach (string strSeg in seperatedCallString)
            {
                string editedSeg = strSeg.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");

                //TODO: Fix multiple people call not working
                if (editedSeg == "") continue;
                if (!CardDealer.CardNames.Contains(editedSeg) && !variables[Context.Guild].Players.Contains(editedSeg))
                {
                    await ReplyAsync(
                        $":x: `{editedSeg}` not recognized as a card or a player! :x:"); // if a strSeg in the callString is not a player nor a card then it's invalid
                    return;
                }

                if (variables[Context.Guild].Players.Contains(editedSeg)) // if this part of the segStr is a player
                {
                    foreach (string card in claimedCards)
                        if (!variables[Context.Guild].PlayerCards[cuser].Contains(CardDealer.GetCardByName(card.ToUpperInvariant())))
                            works = false;
                    cuser = editedSeg;

                    if (variables[Context.Guild].TeamDict[cuser] !=
                        variables[Context.Guild].TeamDict[authorName])
                    {
                        await ReplyAsync($":x: callString included players not on your team! :x:");
                        return;
                    }

                    claimedCards = new List<string>();
                }
                else
                {
                    if (CardDealer.CardNames.Contains(editedSeg))
                    {
                        claimedCards.Add(editedSeg);
                        allClaimed.Add(editedSeg);
                    }
                }
            }

            foreach (string card in claimedCards)
            {
                if (!variables[Context.Guild].PlayerCards[cuser].Contains(CardDealer.GetCardByName(card)))
                    works = false;
            }

            int hsindex = CardDealer.HalfSuitNames.IndexOf(halfsuit);

            foreach (string card in allClaimed)
            {
                for (var i = 0; i < 6; i++)
                    if (!allClaimed.Contains(CardDealer.CardNames[hsindex * 6 + i]))
                    {
                        await ReplyAsync(":x: Cards do not match up with the halfsuit :x: (you autist)");
                        return;
                    }
            }

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

            var builder = new EmbedBuilder {Title = ":telephone_receiver: HalfSuit Call :telephone_receiver:" };

            if (works)
            {
                variables[Context.Guild].AlgebraicNotation += $"callhs {username} {halfsuit} {callstring} hit;";

                builder.Color = Color.Green;
                builder.Description = $":boom: <@{username}> **hit** the `{halfsuit}`! :boom:";
                if (team == "red") variables[Context.Guild].RedScore++;
                else variables[Context.Guild].BlueScore++;
            }
            else
            {
                variables[Context.Guild].AlgebraicNotation += $"callhs {username} {halfsuit} {callstring} miss;";

                builder.Color = Color.DarkRed;
                builder.Description = $":thinking: <@{username}> **missed** the `{halfsuit}`! :thinking:";
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
            else
            {
                if (variables[Context.Guild].RedScore >= 5)
                {
                    await ReplyAsync("Red Team has clinched the game!! Use `.reset` to stop the game now, or continue playing! Also you may view the AFN using the `.afn` command");
                    variables[Context.Guild].GameClinch = true;
                }
                else if (variables[Context.Guild].BlueScore >= 5)
                {
                    await ReplyAsync("Blue Team has clinched the game!! Use `.reset` to stop the game now, or continue playing! Also you may view the AFN using the `.afn` command");
                    variables[Context.Guild].GameClinch = true;
                }
            }

            if (CheckPlayerTurnHandEmpty() && variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync(
                    $":open_mouth: {variables[Context.Guild].PlayerTurn} is out of cards! Use the `.designate` command to select the next player! :open_mouth:");
                variables[Context.Guild].NeedsDesignatedPlayer = true;
            }
        }

        [Command("score")]
        [Summary("Shows the current score")]
        public async Task Score()
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync(":x: Game not yet in progress! :x:");
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
            username = username.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");

            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not yet in progress! :x:");
                return;
            }

            if (variables[Context.Guild].NeedsDesignatedPlayer)
            {
                if (!variables[Context.Guild].Players.Contains(username))
                {
                    await ReplyAsync($":x: {username} is not a valid username! :x:");
                    return;
                }

                variables[Context.Guild].PlayerTurn = username;
                await ReplyAsync($":weary: It is now <@{username}>'s turn! :weary:");
                variables[Context.Guild].NeedsDesignatedPlayer = false;

                variables[Context.Guild].AlgebraicNotation += $"designate {username};";

                if (CheckPlayerTurnHandEmpty())
                {
                    await ReplyAsync(
                        $":open_mouth: <@{variables[Context.Guild].PlayerTurn}> is out of cards! Use the `.designate` command to select the next player! :open_mouth:");
                    variables[Context.Guild].NeedsDesignatedPlayer = true;
                }
            }
            else
            {
                await ReplyAsync(":rage: A designated player is not needed right now! :rage:");
            }
        }

        [Command("cardcount")]
        [Summary("Tells you the number of cards a player has")]
        public async Task CardCount(string username)
        {
            username = username.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");
            await ReplyAsync($"<@{username}> has **{variables[Context.Guild].PlayerCards[username].Count}** cards.");
        }

        [Command("reset")]
        [Summary("Resets the game")]
        public async Task Reset()
        {
            variables[Context.Guild] = new DataStorage();
            await ReplyAsync(":gear: All variables reinitalized. :gear:");
        }

        [Command("afn")]
        [Summary("Displays the AFN for the game")]
        public async Task AFN()
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not yet in progress! :x:");
                return;
            }

            if (!variables[Context.Guild].GameClinch)
            {
                await ReplyAsync(":x: Cannot view AFN while game is in progress! :x:");
                return;
            }
           
            await ReplyAsync(variables[Context.Guild].AlgebraicNotation);
        }

        [Command("surrender")]
        [Alias("ff", "forfeit")]
        [Summary("Allows a team to surrender")]
        public async Task Surrender()
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not yet in progress! :x:");
                return;
            }

            if (variables[Context.Guild].TeamDict[Context.User.Id.ToString()] == "red")
            {
                variables[Context.Guild].RedSurrenderVotes++;
                await ReplyAsync(
                    $":flag_white: {Context.User.Mention} has voted to surrender! ({variables[Context.Guild].RedSurrenderVotes} / {variables[Context.Guild].RedTeam.Count}) :flag_white:");
            }
            else if (variables[Context.Guild].TeamDict[Context.User.Id.ToString()] == "blue")
            {
                variables[Context.Guild].BlueSurrenderVotes++;
                await ReplyAsync(
                    $":flag_white: {Context.User.Mention} has voted to surrender! ({variables[Context.Guild].BlueSurrenderVotes} / {variables[Context.Guild].BlueTeam.Count}) :flag_white:");
            }
            else
                await ReplyAsync(":x: Player not recognized on either team! :x:");

            var builder = new EmbedBuilder { Title = "Game Result!" };

            if (variables[Context.Guild].RedSurrenderVotes / (double) variables[Context.Guild].RedTeam.Count >=
                2.0 / 3.0)
            {
                await ReplyAsync($":flag_white: **Red Team has surrendered!** :flag_white:");

                builder.Color = Color.Blue;
                builder.Description = ":large_blue_circle: Blue team wins! :large_blue_circle:";
                builder.AddField("Final Scores",
                    $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
                await ReplyAsync("", false, builder.Build());

                await ReplyAsync(
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
            }
            else if (variables[Context.Guild].BlueSurrenderVotes / (double) variables[Context.Guild].BlueTeam.Count >=
                     2.0 / 3.0)
            {
                await ReplyAsync($":flag_white: **Blue Team has surrendered!** :flag_white:");

                builder.Color = Color.Red;
                builder.Description = ":red_circle: Red team wins! :red_circle:";
                builder.AddField("Final Scores",
                    $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
                await ReplyAsync("", false, builder.Build());

                await ReplyAsync(
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
            }
        }

        private async Task DeclareResult()
        {
            var builder = new EmbedBuilder {Title = "Game Result!"};

            if (variables[Context.Guild].RedScore > variables[Context.Guild].BlueScore)
            {
                builder.Color = Color.Red;
                builder.Description = ":red_circle: Red team wins! :red_circle:";
            }
            else
            {
                builder.Color = Color.Blue;
                builder.Description = ":large_blue_circle: Blue team wins! :large_blue_circle:";
            }

            builder.AddField("Final Scores",
                $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
            await ReplyAsync("", false, builder.Build());

            await ReplyAsync(
                ":checkered_flag: The game has ended! Use the `.reset` command to play again! Or use the `.afn` to view the algebraic notation :checkered_flag:");
        }
        
        private bool CheckPlayerTurnHandEmpty() // ?
        {
            return variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Count == 0;
        }
    }
}