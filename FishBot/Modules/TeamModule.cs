using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FishBot.Modules
{
    [Name("Team")]
    [Group("team")]
    public class TeamModule : ModuleBase
    {
        public static Dictionary<string, string> TeamDict = new Dictionary<string, string>();
        public static List<string> RedTeam = new List<string>();
        public static List<string> BlueTeam = new List<string>();

        [Command("add")]
        [Summary("Adds a player to red team or blue team")]
        public async Task Add(string username, string teamname)
        {
            teamname = teamname.ToLower();

            if (teamname != "red" && teamname != "blue")
            {
                await ReplyAsync($"That team name is not valid! Please only use the teams \"Blue\" and \"Red\"");
                return;
            }

            if (TeamDict.ContainsKey(username))
                await ReplyAsync($"{username} is already in a team! They are in {TeamDict[username]}");
            else
            {
                TeamDict.Add(username, teamname);
                if(teamname == "red") RedTeam.Add(username);
                else if(teamname == "blue") BlueTeam.Add(username);
                GameModule.Players.Add(username);

                await ReplyAsync($"Added {username} to {teamname}");
            }
        }

        [Command("remove")]
        [Summary("Removes a player from team")]
        public async Task Remove(string username)
        {
            if (!TeamDict.ContainsKey(username))
                await ReplyAsync($"{username} is not already on a team! Add them onto a team using \"-team add USERNAME\"");
            else
            {
                string prevTeam = TeamDict[username];
                TeamDict.Remove(username);

                if (RedTeam.Contains(username)) RedTeam.Remove(username);
                else if (BlueTeam.Contains(username)) BlueTeam.Remove(username);

                await ReplyAsync($"Removed {username} from {prevTeam}");
            }
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
                await ReplyAsync($"That team name is not valid! Please only use the teams \"Blue\" and \"Red\"");
                return;
            }

            if (teamname == "red")
            {
                teamColor = Color.Red;
                teamString = "Red Team";
            }
            else if (teamname == "blue")
            {
                teamColor = Color.Blue;
                teamString = "Blue Team";
            }

            var builder = new EmbedBuilder()
            {
                Color = teamColor,
            };

            var players = TeamDict.Where(x => x.Value == teamname).ToList();
            string output = players.Aggregate("", (current, t) => current + (t.Key + "\n"));

            builder.AddField(teamString, output, true);

            await ReplyAsync("", false, builder.Build());
        }

    }
}
