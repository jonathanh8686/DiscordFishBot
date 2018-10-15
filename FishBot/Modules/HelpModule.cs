using System;
using System.Linq;
using System.Runtime.Serialization;
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
            var builder = new EmbedBuilder
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

                    if (cmd.Module.Name == "SecretModule") continue;
                    description += $"{prefix}{cmd.Aliases.First()} ";
                    description = cmd.Parameters.Aggregate(description, (current, pm) => current + $"[{pm.Name}] ");
                    description += Environment.NewLine;
                }

                if (!string.IsNullOrWhiteSpace(description))
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
            }

            await ReplyAsync("", false, builder.Build());
        }

        ////lul
        //[Command("purge")]
        //public async Task purge()
        //{
        //    var messages = Context.Channel.GetMessagesAsync(500, CacheMode.AllowDownload, RequestOptions.Default).FlattenAsync();
        //    foreach (var msg in messages.Result)
        //    {
        //           await msg.DeleteAsync();
        //    }
        //}

        //[Command("changelog")]
        //public async Task change()
        //{
        //    await Context.Message.DeleteAsync();

        //    var builder = new EmbedBuilder
        //    {
        //        Title = "**New Changes!**",
        //        Color = Color.Gold
        //    };

        //    builder.AddField("Designate Changes!",
        //        "Designate automatically changes to the next player in line, so it better reflects actual gameplay!");

        //    builder.AddField("`.team list` changes",
        //        "Now using the `.team list` command with no team parameter will display both teams and their members.");

        //    builder.AddField("`.cardcount` Changes!",
        //        "Using the `.cardcount` command with no selected player will show all players and the number of cards in their hand.");

        //    builder.AddField("Changelog Changes", "look at this cool changelog this took work");

        //    await ReplyAsync("", false, builder.Build());
        //}

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            var builder = new EmbedBuilder
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
            await ReplyAsync($"https://github.com/jonathanh8686/DiscordFishBot/blob/master/README.md");
        }
    }
}