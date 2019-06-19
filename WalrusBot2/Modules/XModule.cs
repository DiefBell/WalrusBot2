using System.Collections.Generic;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

using WalrusBot2.Data;

namespace WalrusBot2.Modules
{
    public abstract class XModule : InteractiveBase
    {
        public static CommandService Service;

        protected dbContextWalrus database = new dbContextWalrus();
        protected bool HasRole(IGuildUser user, string roleName)
        {
            List<ulong> roles = user.RoleIds as List<ulong>;
            return roles.ConvertAll<string>(new System.Converter<ulong, string>(id => id.ToString() ) ).Contains(database["role", roleName]);
        }
    }
}
