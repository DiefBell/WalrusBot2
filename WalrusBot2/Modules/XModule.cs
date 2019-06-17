using System.Collections.Generic;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using WalrusBot2.Data;

namespace WalrusBot2.Modules
{
    [DontAutoLoad]
    public class XModule : ModuleBase<SocketCommandContext>
    {
        [Command]  // default command
        public async Task DefaultAsync()
            => await HelpAsync();

        [Command("help")]
        public virtual async Task HelpAsync()
            => await ReplyAsync("Help string for this command.");

        protected dbContextWalrus database = new dbContextWalrus();
        protected bool HasRole(IGuildUser user, string roleName)
        {
            List<ulong> roles = user.RoleIds as List<ulong>;
            return roles.ConvertAll<string>(new System.Converter<ulong, string>(id => id.ToString() ) ).Contains(database["role", roleName]);
        }
    }
}
