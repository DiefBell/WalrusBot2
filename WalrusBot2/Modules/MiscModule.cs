using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace WalrusBot2.Modules
{
    public class MiscModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task PingAsync()
            => await ReplyAsync("Pong!");

        [Command("info")]
        public async Task InfoAsync()
            => await ReplyAsync($"Hello, I am a bot called **{Context.Client.CurrentUser.Username}** written in **Discord.Net 2.1.1**!\n");

        [Command("GDPR")]
        public async Task GdprAsync()
            => await ReplyAsync("This is useful if, for whatever reason, you run an organisation that stores members' data.");

        [Command("website")]
        [Alias("site")]
        public async Task WebsiteAsync()
            => await ReplyAsync("Put a link to your website here! :)");

        [Command("bork")]
        public async Task Bork()
        {
            ulong adminRoleId;
            try
            {
                adminRoleId = ulong.Parse(Program._config["AdminRoleID"]);
                if (!((IList<ulong>)((IGuildUser)Context.User).RoleIds).Contains(adminRoleId))
                {
                    // in case you don't give your server mods the Admin permission (which to be frank you really shouldn't...)
                    await ReplyAsync("That command is committee only! Please message a member of the committee if you need help with something :)");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            await ReplyAsync("Woof woof", true);
        }
    }
}
