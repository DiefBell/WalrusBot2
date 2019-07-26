using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using WalrusBot2.Data;
using System.Collections.Generic;

namespace WalrusBot2.Modules
{
    [Group("help")]
    [Name("Help")]
    public class HelpModule : XModule
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            _service = service;
        }

        [Summary("Displays a list of commands that the user can use.")]
        [Command]
        [Name("")]
        public async Task HelpAsync()
        {
            string prefix = database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use." + (Program.Debug ? " Debugging is on..." : "")
            };

            foreach (var module in _service.Modules)
            {
                List<string> descriptions = new List<string>();
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        string modCmd = cmd.Module.Aliases.First();
                        string d = $"{prefix}{modCmd + " " + cmd.Name}";
                        if (!descriptions.Contains(d)) descriptions.Add(d);
                    }
                }
                string description = string.Join("\n", descriptions.ToArray());

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

        [Summary("Displays the help message for the given command.")]
        [Command]
        [Name("<command> [sub-command]")]
        public async Task HelpAsync([Remainder]string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"];
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
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
