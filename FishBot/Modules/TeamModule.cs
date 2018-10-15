using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using static FishBot.Program;

namespace FishBot.Modules
{
    [Name("Team")]
    [Group("team")]
    public class TeamModule : ModuleBase
    {
        [Command("join")]
        [Summary("Allows a player to join a team (Red or Blue)")]
        public async Task Join(string teamname)
        {
            if (variables[Context.Guild].GameInProgress) // make sure once game in the progress, teams can't change
            {
                await ReplyAsync($"Game is in progress; Teams are locked in!");
                return;
            }

            teamname = teamname.ToLower();
            if (teamname != "red" && teamname != "blue")
            {
                await ReplyAsync($"That team name is not valid! Please only use the teams \"Blue\" and \"Red\"");
                return;
            }

            string username = Context.User.Id.ToString();

            if (variables[Context.Guild].AuthorUsers.ContainsValue(Context.User))
            {
                await ReplyAsync($"{Context.User.Username} already has an assigned team!");
                return;
            }

            if (variables[Context.Guild].TeamDict.ContainsKey(username))
            {
                await ReplyAsync($"<@{username}> is already in a team! They are in `{variables[Context.Guild].TeamDict[username]}`");
                return;
            }

            variables[Context.Guild].TeamDict.Add(username, teamname);
            if (teamname == "red") variables[Context.Guild].RedTeam.Add(username);
            else if (teamname == "blue") variables[Context.Guild].BlueTeam.Add(username);
            variables[Context.Guild].Players.Add(username);

            variables[Context.Guild].AuthorUsers.Add(username, Context.User);

            await ReplyAsync($"Added <@{username}> to `{teamname}`");
        }

        [Command("leave")]
        [Summary("Allows a player to leave a team")]
        public async Task Leave()
        {
            string username = Context.User.Id.ToString();

            if (variables[Context.Guild].GameInProgress)
            {
                await ReplyAsync($"Game is in progress; Teams are locked in!");
                return;
            }

            if (!variables[Context.Guild].TeamDict.ContainsKey(username))
            {
                await ReplyAsync($":x: <@{username}> is not already on a team! :x:");
                return;
            }

            string prevTeam = variables[Context.Guild].TeamDict[username];
            variables[Context.Guild].TeamDict.Remove(username);
            variables[Context.Guild].Players.Remove(username);

            if (variables[Context.Guild].RedTeam.Contains(username))
                variables[Context.Guild].RedTeam.Remove(username);
            else if (variables[Context.Guild].BlueTeam.Contains(username))
                variables[Context.Guild].BlueTeam.Remove(username);

            var newAuthorUsers = new Dictionary<string, IUser>(variables[Context.Guild].AuthorUsers);
            foreach (var user in variables[Context.Guild].AuthorUsers)
                if (user.Value == Context.User)
                    newAuthorUsers.Remove(user.Key); // unpair discord IUser to username
            variables[Context.Guild].AuthorUsers = new Dictionary<string, IUser>(newAuthorUsers);

            await ReplyAsync($"Removed <@{username}> from `{prevTeam}`");
        }

        [Command("list")]
        [Summary("Lists the players on a team")]
        public async Task List(string teamname)
        {
            var teamColor = new Color();
            var teamString = "";

            teamname = teamname.ToLower();

            if (teamname != "red" && teamname != "blue")
            {
                await ReplyAsync($":x: That team name is not valid! Please only use the teams \"Blue\" and \"Red\" :anger:");
                return;
            }

            switch (teamname)
            {
                case "red":
                    teamColor = Color.Red;
                    teamString = "Red Team";
                    break;
                case "blue":
                    teamColor = Color.Blue;
                    teamString = "Blue Team";
                    break;
            }

            var builder = new EmbedBuilder
            {
                Color = teamColor
            };

            var players = variables[Context.Guild].TeamDict.Where(x => x.Value == teamname).ToList();


            string output = players.Aggregate("", (current, t) => current + $"<@{t.Key}>" + "\n");

            if (output != "")
            {
                builder.AddField(teamString, output, true);
                await ReplyAsync("", false, builder.Build());
            }
            else
                await ReplyAsync($":pensive: There are no players on `{teamname}` :pensive:");

        }

        [Command("list")]
        public async Task List()
        {
            await List("red");
            await List("blue");
        }
    }
}