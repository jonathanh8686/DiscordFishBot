using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using static FishBot.Program;
using static FishBot.Modules.GameModule;

namespace FishBot.Modules
{
    [Name("Call")]
    public class CallModule : ModuleBase
    {
        [Command("call")]
        [Alias("ask")]
        [Summary("Allows a player to call a card from another")]
        public async Task Call(string target, string requestedCard)
        {
            target = target.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");
            requestedCard = requestedCard.ToUpperInvariant();

            if (variables[Context.Guild].RedScore + variables[Context.Guild].BlueScore == 9)
            {
                await ReplyAsync(
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
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
                if (msg.Author.Id == Context.Client.CurrentUser.Id && msg.Content.Contains("*TEMPORARY*")) // everything marked with *TEMPORARY*
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

            // handing illegal moves
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
                    "not sure how this can ever get called but ill leave this here for a few games and see if this text ever shows up and if it doesn't i guess ill delete it xd");

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
                    ":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
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

            foreach (string _ in allClaimed)
            {
                for (var i = 0; i < 6; i++)
                    if (!allClaimed.Contains(CardDealer.CardNames[hsindex * 6 + i]))
                    {
                        await ReplyAsync(":x: Cards do not match up with the halfsuit :x:");
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

            var builder = new EmbedBuilder { Title = ":telephone_receiver: HalfSuit Call :telephone_receiver:" };

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
                // maybe refactor this and combine with the one in gamemodule
                var builder2 = new EmbedBuilder { Title = "Game Result!" };

                if (variables[Context.Guild].RedScore > variables[Context.Guild].BlueScore)
                {
                    builder2.Color = Color.Red;
                    builder2.Description = ":red_circle: Red team wins! :red_circle:";
                }
                else
                {
                    builder2.Color = Color.Blue;
                    builder2.Description = ":large_blue_circle: Blue team wins! :large_blue_circle:";
                }

                builder2.AddField("Final Scores",
                    $"Blue Team: {variables[Context.Guild].BlueScore}\n Red Team: {variables[Context.Guild].RedScore}");
                await ReplyAsync("", false, builder2.Build());

                await ReplyAsync(":checkered_flag: The game has ended! Use the `.reset` command to play again! :checkered_flag:");
                File.WriteAllText("afn.txt", variables[Context.Guild].AlgebraicNotation);
                // end of that upper comment thing
            }
            else
            {
                if (variables[Context.Guild].RedScore >= 5)
                {
                    await ReplyAsync("Red Team has clinched the game!! Use `.reset` to stop the game now, or continue playing!");
                    variables[Context.Guild].GameClinch = true;
                }
                else if (variables[Context.Guild].BlueScore >= 5)
                {
                    await ReplyAsync("Blue Team has clinched the game!! Use `.reset` to stop the game now, or continue playing!");
                    variables[Context.Guild].GameClinch = true;
                }
            }

            if (CheckPlayerTurnHandEmpty() && variables[Context.Guild].GameInProgress)
            {
                string newPlayerID = variables[Context.Guild].PlayerTurn;

                if (variables[Context.Guild].TeamDict[variables[Context.Guild].PlayerTurn] == "red")
                {
                    // red team
                    int loopCount = 0;
                    int playerIndex = variables[Context.Guild].RedTeam.FindIndex(a => a == newPlayerID);
                    do
                    {
                       if (loopCount++ > variables[Context.Guild].BlueTeam.Count)
                            break;
                        playerIndex = variables[Context.Guild].RedTeam.FindIndex(a => a == newPlayerID);
                        newPlayerID = variables[Context.Guild].RedTeam[(playerIndex + 1) % variables[Context.Guild].RedTeam.Count];
                    } while (variables[Context.Guild].PlayerCards[newPlayerID].Count == 0);
                }
                else if (variables[Context.Guild].TeamDict[variables[Context.Guild].PlayerTurn] == "blue")
                {
                    // blue team
                    int loopCount = 0;
                    int playerIndex = variables[Context.Guild].BlueTeam.FindIndex(a => a == newPlayerID);
                    do
                    {
                        if (loopCount++ > variables[Context.Guild].BlueTeam.Count)
                            break;
                        playerIndex = variables[Context.Guild].BlueTeam.FindIndex(a => a == newPlayerID);
                        newPlayerID = variables[Context.Guild].BlueTeam[(playerIndex + 1) % variables[Context.Guild].BlueTeam.Count];
                    } while (variables[Context.Guild].PlayerCards[newPlayerID].Count == 0);
                    
                }

                await ReplyAsync($":open_mouth: <@{variables[Context.Guild].PlayerTurn}> is out of cards! Turn will move to <@{newPlayerID}> :open_mouth:");
                variables[Context.Guild].PlayerTurn = newPlayerID;
                //await ReplyAsync($":open_mouth: {variables[Context.Guild].PlayerTurn} is out of cards! Use the `.designate` command to select the next player! :open_mouth:");
                //variables[Context.Guild].NeedsDesignatedPlayer = true;
                //variables[Context.Guild].Designator = variables[Context.Guild].PlayerTurn.Replace("@", "").Replace("<", "").Replace(">", "").Replace("!", "");
            }
        }

        private bool CheckPlayerTurnHandEmpty() // ?
        {
            return variables[Context.Guild].PlayerCards[variables[Context.Guild].PlayerTurn].Count == 0;
        }


    }
}