using System;
using System.Collections.Generic;
using System.IO;
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
                        ":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
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
            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].Players.Count + ";";
            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].RedTeam.Count + ";";
            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].BlueTeam.Count + ";";
            foreach (string redPlayer in variables[Context.Guild].RedTeam)
            {
                variables[Context.Guild].AlgebraicNotation +=
                    redPlayer + ":" + variables[Context.Guild].AuthorUsers[redPlayer] + ";";
            }

            foreach (string bluePlayer in variables[Context.Guild].BlueTeam)
            {
                variables[Context.Guild].AlgebraicNotation +=
                    bluePlayer + ":" + variables[Context.Guild].AuthorUsers[bluePlayer] + ";";
            }

            // Starting game
            await ReplyAsync(":trophy: `Starting Game...` :weary:");
            variables[Context.Guild].PlayerTurn = variables[Context.Guild]
                .Players[new Random().Next(variables[Context.Guild].Players.Count)]; // get random player to start

            variables[Context.Guild].AlgebraicNotation += variables[Context.Guild].PlayerTurn + ";"; // add first player to afn

            variables[Context.Guild].GameStart = true;
            await CardDealer.DealCards(Context.Guild);
            await ReplyAsync($":game_die: It's <@{variables[Context.Guild].PlayerTurn}>'s turn!");
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

            if (variables[Context.Guild].NeedsDesignatedPlayer && variables[Context.Guild].Designator == Context.Message.Author.Mention.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", ""))
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
                    await ReplyAsync($":open_mouth: <@{variables[Context.Guild].PlayerTurn}> is out of cards! Use the `.designate` command to select the next player! :open_mouth:");
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
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not yet in progress! :x:");
                return;
            }

            username = username.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");
            await ReplyAsync($"<@{username}> has **{variables[Context.Guild].PlayerCards[username].Count}** cards.");
        }

        [Command("cardcount")]
        public async Task CardCount()
        {
            if (!variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($":x: Game is not yet in progress! :x:");
                return;
            }

            string ccString = "";
            foreach (var playerHand in variables[Context.Guild].PlayerCards)
                ccString += $"<@{playerHand.Key}> has **{variables[Context.Guild].PlayerCards[playerHand.Key].Count}** cards.\n";
            await ReplyAsync(ccString);

        }

        [Command("reset")]
        [Summary("Resets the game")]
        public async Task Reset()
        {
            string[] blackList = { "183403512468733953", "216800764927016960", "157998279760674817", "117458353839538181" };

            if (blackList.Contains(Context.Message.Author.Id.ToString()))
            {
                await ReplyAsync(":hear_no_evil: :hear_no_evil: :hear_no_evil: ");
            }
            else
            {
                variables[Context.Guild] = new DataStorage();
                await ReplyAsync(":gear: All variables reinitalized. :gear:");
            }
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

            if (variables[Context.Guild].TeamDict[Context.User.Id.ToString()] == "red" && !variables[Context.Guild].RedSurrenders.Contains(Context.User.Id.ToString()))
            {
                variables[Context.Guild].RedSurrenders.Add(Context.User.Id.ToString());
                variables[Context.Guild].RedSurrenderVotes++;
                await ReplyAsync($":flag_white: {Context.User.Mention} has voted to surrender! ({variables[Context.Guild].RedSurrenderVotes} / {variables[Context.Guild].RedTeam.Count}) :flag_white:");
            }
            else if (variables[Context.Guild].TeamDict[Context.User.Id.ToString()] == "blue" && !variables[Context.Guild].BlueSurrenders.Contains(Context.User.Id.ToString()))
            {
                variables[Context.Guild].BlueSurrenders.Add(Context.User.Id.ToString());
                variables[Context.Guild].BlueSurrenderVotes++;
                await ReplyAsync($":flag_white: {Context.User.Mention} has voted to surrender! ({variables[Context.Guild].BlueSurrenderVotes} / {variables[Context.Guild].BlueTeam.Count}) :flag_white:");
            }
            else
                await ReplyAsync(":x: Player not recognized on either team! :x:");

            var builder = new EmbedBuilder { Title = "Game Result!" };

            if (variables[Context.Guild].RedSurrenderVotes / (double)variables[Context.Guild].RedTeam.Count >= 4.0 / 5.0)
            {
                await ReplyAsync($":flag_white: **Red Team has surrendered!** :flag_white:");

                builder.Color = Color.Blue;
                builder.Description = ":large_blue_circle: Blue team wins! :large_blue_circle:";
                builder.AddField("Final Scores",
                    $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
                await ReplyAsync("", false, builder.Build());

                await ReplyAsync(":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");

                File.WriteAllText("afn.txt", variables[Context.Guild].AlgebraicNotation);
            }
            else if (variables[Context.Guild].BlueSurrenderVotes / (double)variables[Context.Guild].BlueTeam.Count >= 4.0 / 5.0)
            {
                await ReplyAsync($":flag_white: **Blue Team has surrendered!** :flag_white:");

                builder.Color = Color.Red;
                builder.Description = ":red_circle: Red team wins! :red_circle:";
                builder.AddField("Final Scores",
                    $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
                await ReplyAsync("", false, builder.Build());

                await ReplyAsync(":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
                File.WriteAllText("afn.txt", variables[Context.Guild].AlgebraicNotation);
            }
        }

        public async Task DeclareResult()
        {
            var builder = new EmbedBuilder { Title = "Game Result!" };

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

            await ReplyAsync(":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
            File.WriteAllText("afn.txt", variables[Context.Guild].AlgebraicNotation);
        }

        private bool CheckPlayerTurnHandEmpty() // ?
        {
            return variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Count == 0;
        }


    }
}