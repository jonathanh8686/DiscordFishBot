using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FishBot.Modules
{
    //code plagiarized from https://github.com/Aux/Discord.Net-Example/blob/1.0/src/Modules/HelpModule.cs xd

    public class HelpModule : ModuleBase
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            _service = service;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            string prefix = Config.Load().BotPrefix;
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (!result.IsSuccess) continue;

                    description += $"{prefix}{cmd.Aliases.First()} ";
                    description = cmd.Parameters.Aggregate(description, (current, pm) => current + $"[{pm.Name}] ");
                    description += Environment.NewLine;
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"Remarks: {cmd.Remarks}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("intro")]
        public async Task Introduce()
        {
            await ReplyAsync($"**FishBot** by *Jonathan Hsieh*\n\n" +
                             $"**First, Join a team using the `.team add [nickname]` command**.\n This nickname will be what" +
                             $"other players refer to you as for the rest of the game. There are only two teams, `Red` and `Blue`" +
                             $"However, the bot still needs to know what your actual discord account is so it can DM you your hand.\n\n" +
                             $"So the **Second step is to claim a username, using the `.claim [nickname]` command**.\n This will link" +
                             $" your discord account to the nickname. *Note that the nicknames are ALWAYS case-sensitive*\n\n" +
                             $"Finally, once everyone is linked to an account, **use the `.start` command to begin the game!**\n");

            await ReplyAsync($"Once in game, there are two commands that you need to know\n" +
                             $"The `.call [username] [card]` is the most important command for this game. To use this command, follow " +
                             $"the format given, keeping in mind that the `[username]` field is case-sensitive. The card should come in" +
                             $"the form `[number][suit]` for example, the `Ace of Spades` is `AS`. And the `Ten of Hearts` is `10H`. As a " +
                             $"final example, calling the Big Joker (`J+`) from a player called \"optimalplayer\" would look like:" +
                             $" `.call optimalplayer J+`\n\n" +
                             $"The second important command is `.callhs [halfsuit] [callString]`. The command is significantly more" +
                             $"complicated and harder to use because I'm lazy. First thing to know is that the halfsuits go by names " +
                             $"like `D-` (for lower diamonds) or `S+` (for upper spades). The jokers/eights go by the name `J8` The second " +
                             $"part of the command is the callString. To use the call string, type in a username followed by the cards that " +
                             $"you think that person has. For example, you (\"optimalplayer\") had the `AS, KS, and 10S` and you " +
                             $"think your teammate (\"feedingretard\") has the `9S, QS, and JS` calling the `S+` looks like: `.callhs S+" +
                             $" \"optimalplayer AS KS 10S feedingretard 9S QS JS\"` *Note that the callString MUST be surrounded by quotation marks" +
                             $" and that everything in this case is case-sensitive*");
            return;
        }
    }
}