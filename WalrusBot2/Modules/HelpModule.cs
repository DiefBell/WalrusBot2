using Discord.Commands;
using System.Threading.Tasks;

namespace WalrusBot2.Modules
{
    [Group("help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        [Command]
        public async Task HelpAsync()
            => await ReplyAsync("This is the default help list!");

        [Command("help")]
        public async Task HelpHelpAsync()
            => await ReplyAsync("Used alone, will return a list of all the commands you can use. Follow it with a command name to get help with that command (congrats, you've already used this command successfully. Recursion at its finest!)");
    }
}
