using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace WalrusBot2.Modules
{
    [Name("Miscellaneous Commands")]
    public class MiscModule : XModule
    {
        [Summary("The most basic command for this bot. Returns \"Pong!\" to check that the server's  working.")]
        [Command("ping")]
        public async Task PingAsync()
            => await ReplyAsync("Pong!");

        [Summary("Listen the the holy word of our lord and saviour, the Walrus King.")]
        [Command("praytowalrus")]
        public async Task PrayAsync()
            => await ReplyAsync("https://youtu.be/X2jjf_XRpKc");

        [Summary("Displays some info about this bot.")]
        [Command("info")]
        public async Task InfoAsync()
            => await ReplyAsync($"Hello, I am a bot called **{Context.Client.CurrentUser.Username}** written in **Discord.Net 2.1.1**!\n");

        [Summary("Shows our GDPR message.")]
        [Command("gdpr")]
        public async Task GdprAsync()
            => await ReplyAsync(database["string", "gdpr"]);

        [Summary("Returns a link to our website.")]
        [Command("website")]
        [Alias("site")]
        public async Task WebsiteAsync()
            => await ReplyAsync(database["string", "website"]);

        [Summary("Returns a link to our SUSU page.")]
        [Command("susu")]
        [Alias("usus")]
        public async Task SusuAsync()
            => await ReplyAsync("https://www.susu.org/groups/southampton-university-esports-society");

        [Summary("Makes Maisie say \"woof\" in a text-to-speak message.")]
        [Command("bork")]
        [RequireUserPermission(GuildPermission.SendTTSMessages)]
        [RequireUserRole(new string[] { "commitee", "tester" })]
        public async Task Bork()
            => await ReplyAsync("Woof woof", true);

        /*
        [Command("test")]
        public async Task TestAsync()
            => await ReplyAsync("this - is - a - test".Split(new[] { '-' }, 2)[1]);
            */
    }
}