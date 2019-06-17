using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace WalrusBot2.Modules
{
    public class MiscModule : XModule
    {
        [Command("ping")]
        public async Task PingAsync()
            => await ReplyAsync("Pong!");

        [Command("info")]
        public async Task InfoAsync()
            => await ReplyAsync($"Hello, I am a bot called **{Context.Client.CurrentUser.Username}** written in **Discord.Net 2.1.1**!\n");

        [Command("GDPR")]
        public async Task GdprAsync()
            => await ReplyAsync(database["string", "gdpr"]);

        [Command("website")]
        [Alias("site")]
        public async Task WebsiteAsync()
            => await ReplyAsync(database["string", "website"]);

        [Command("bork")]
        public async Task Bork()
        {
            if(!HasRole(Context.User as IGuildUser, "committee"))
            {
                await ReplyAsync(database["string","lacksperms"]);
                return;
            }

            await ReplyAsync("Woof woof", true);
        }
    }
}
