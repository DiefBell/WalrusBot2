using Discord.Commands;
using System.Threading.Tasks;

namespace WalrusBot2.Modules
{
    [Group("groupname_changethis")]
    public class NewModule : ModuleBase<SocketCommandContext>
    {
        [Command]  // default command
        public async Task DefaultAsync()
            => await ReplyAsync("This is the default command for this group.");

        [Command("help")]
        public async Task HelpAsync()
            => await ReplyAsync("Help string for this command.");
    }
}
