using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using WalrusBot2.Data;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using Discord.WebSocket;

namespace WalrusBot2.Modules
{
    [Group("userinfo")]
    [Name("UserInfo")]
    [RequireContext(ContextType.Guild)]
    [RequireUserRole(new string[] { "committee", "tester" })]
    public class UserInfoModule : XModule
    {
        [Command("permarole")]
        public async Task PermaRoleAsync(ulong userId, string roleName, ulong roleId)
        {
            SocketGuildUser user = Context.Guild.Users.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                await ReplyAsync("That user doesn't appear to be on this server!");
                return;
            }
            await PermaRoleAsync(user, roleName, roleId);
        }

        [Command("permarole")]
        public async Task PermaRoleAsync(SocketGuildUser user, IRole role)
        {
            await PermaRoleAsync(user, role.Name, role.Id);
        }

        [Command("permarole")]
        public async Task PermaRoleAsync(SocketGuildUser user, string roleName, ulong roleId)
        {
            if (!(Context.Guild.Roles.Any(r => r.Id == roleId)))
            {
                await ReplyAsync("That role doesn't appear to exist on this server! (honestly not sure how you even managed that tbh...)");
                return;
            }

            string userIdString = user.Id.ToString();

            WalrusUserInfo userInfo = database.WalrusUserInfoes.Where(x => x.UserId == userIdString).FirstOrDefault();
            if (userInfo == null)
            {
                userInfo = new WalrusUserInfo
                {
                    UserId = user.Id.ToString(),
                    Verified = false,
                    Username = user.Username + "#" + user.Discriminator,
                    Email = null,
                    Code = null,
                    IGNsJSON = @"{}",
                    AdditionalRolesJSON = @"{}"
                };
            }

            // not running checks as should always be valid JSON
            JObject additionalRolesJson = JObject.Parse(userInfo.AdditionalRolesJSON);
            additionalRolesJson.Add(roleName, roleId.ToString());
            userInfo.AdditionalRolesJSON = additionalRolesJson.ToString();

            await database.SaveChangesAsync();
            await ReplyAsync("Permanent role added to user :)");
        }
    }
}