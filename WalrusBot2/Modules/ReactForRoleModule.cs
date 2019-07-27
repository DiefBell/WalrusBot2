using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using WalrusBot2.Data;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Rest;
using System;

namespace WalrusBot2.Modules
{
    /// <todo>
    /// - Create a class (or possibly struct?) for the react role messages to clean up the code.
    /// - Rewrite anything to do with Embeds using SocketUserMessage because it only has a single embed in it
    /// </todo>
    [Group("reactforrole")]
    [Alias("rfr")]
    [Name("React for Role")]
    [RequireContext(ContextType.Guild)]
    [RequireUserRole(new string[] { "commitee", "tester" })]
    public class ReactForRole : XModuleUpdatable
    {

        #region Create React-for-Role message
        [Summary("Create a new React-for-Role message with given icon, title and description in the specified channel. Uses the current channel by default and icon is optional.")]
        [Command("create")]
        public async Task RfrCreateAsync(SocketGuildChannel channel, string title, string iconUrl, string desc)
        {
            if(await CreateAsync(channel, "React-for-Role", title, iconUrl, desc))
            {
                await ReplyAsync(database["string", "succRfrCreate"]);
                await ReplyAsync($"Created message ID: `{_msg.Id}`");
            }
        }
        [Command("create")]
        public async Task RfrCreateAsync(SocketGuildChannel channel, string title, string desc)
             => await RfrCreateAsync(channel, title, database["config", "rfrDefaultIconUrl"], desc);
        [Command("create")]
        public async Task RfrCreateAsync(string title, string desc)
             => await RfrCreateAsync(Context.Channel as SocketGuildChannel, title, database["config", "rfrDefaultIconUrl"], desc);
        [Command("create")]
        public async Task RfrCreateAsync(string title, string iconUrl, string desc)
            => await RfrCreateAsync(Context.Channel as SocketGuildChannel, title, iconUrl, desc);
        #endregion

        #region Add a role to React-for-Role message
        [Summary("Adds and entry to a React-for-Role message with the provided ID in the specified channel (uses current channel by default). Both the message ID and role ID should be numbers that can " +
            "be copied by enabling Settings>Appearance>Developer Mode. If you don't have Nitro then you can use an emote from another server by removing the angled brackets " +
            "'<' '>' from the escaped emote string.")]
        [Command("add")]
        public async Task RfrAddAsync(ulong msgId, string roleDisplayName, string roleEmote, string roleIdString, int position=0)
            => await RfrAddAsync(Context.Channel, msgId, roleDisplayName, roleEmote, roleIdString, position);
        [Command("add")]
        public async Task RfrAddAsync(IMessageChannel channel, ulong msgId, string roleDisplayName, string roleEmote, string roleIdString, int position=0)
        {
            #region Role Validity
            UInt64 roleId;
            try
            {
                roleId = Convert.ToUInt64(roleIdString);
            }
            catch
            {
                await ReplyAsync(database["string", "errParseRoleId"]);
                return;
            }
            IRole role = (channel as IGuildChannel).Guild.GetRole(roleId);
            if (role == null)
            {
                await ReplyAsync(database["string", "errRoleInvalid"]);
                return;
            }
            #endregion
            #region Emote Validity
            IEmote emote = parseEmote(ref roleEmote);
            if(emote == null)
            {
                await ReplyAsync(database["string", "errParseNewEmote"]);
            }
            #endregion
            if (await AddAsync(channel, msgId, "React-for-Role", roleDisplayName, roleEmote.ToString() + " " + role.Mention, position))
            {
                await _msg.AddReactionAsync(emote);
                await ReplyAndDeleteAsync(database["string", "succRfrAdd"], timeout: TimeSpan.FromSeconds(2));
            }

            // AddAsync() contains all of the error reporting, so this line is unnecessary
            //else await ReplyAsync(database["string", "errAddRfr"] );
        }
        #endregion

        #region Remove role from React-for-Role message
        [Summary("Deletes an entry with the given name from a React-for-Role message with the given ID in the specified channel (uses current channel by default).")]
        [Command("remove")]
        [Alias("rem", "delete", "del")]
        public async Task RfrRemoveAsync(ulong msgId, string roleDisplayName)
            => await RfrRemoveAsync(Context.Channel, msgId, roleDisplayName);

        [Command("remove")]
        [Alias("rem", "delete", "del")]
        public async Task RfrRemoveAsync(IMessageChannel channel, ulong msgId, string roleDisplayName)
        {
            if(!await RemoveAsync(channel, msgId, "React-for-Role", roleDisplayName) ) return;

            string emoteString = _oldEmbed.Fields.Where(em => em.Name == roleDisplayName).First().Value.Split(' ')[0];
            IEmote emote = parseEmote(ref emoteString);  // not actually going to use the returned emoteString here
            if(emote == null)
            {
                await ReplyAsync(database["string", "errParseOldEmote"]);
                return;
            }

            List<IUser> users = await _msg.GetReactionUsersAsync(emote, _msg.Reactions[emote].ReactionCount).Flatten().ToList();
            foreach (IUser user in users)
            {
                await _msg.RemoveReactionAsync(emote, user);
            }
            await ReplyAndDeleteAsync(database["string", "succRfrDel"], timeout: TimeSpan.FromSeconds(2));
        }
        #endregion

        #region Move a React-for-Role message
        [Summary("Moves the React-for-Role message with the given ID from the `fromChannel` to the `toChannel`, taking all reacts with it. " +
            "Uses the current channel for either the fromChannel or the toChannel, depending on which one is ommitted in the command.")]
        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        public async Task RfrMoveAsync(IMessageChannel fromChannel, ulong msgId, bool delOld=true)
            => await RfrMoveAsync(fromChannel, msgId, Context.Channel as IMessageChannel, delOld);
        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        public async Task RfrMoveAsync(ulong msgId, IMessageChannel toChannel, bool delOld=true)
            => await RfrMoveAsync(Context.Channel, msgId, toChannel, delOld);
        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        public async Task RfrMoveAsync(IMessageChannel fromChannel, ulong msgId, IMessageChannel toChannel, bool delOld = true)
        {
            if (await MoveMessageAsync(fromChannel, msgId, toChannel, "React-for-Role", delOld))
            {
                await ReplyAndDeleteAsync(database["string", "succRfrMove"], timeout: TimeSpan.FromSeconds(2));
                await ReplyAsync($"Moved message ID: `{_msg.Id}`");
            }
        }
        #endregion

        #region Position an entry in a React-for-Role message
        [Summary("Repositions the given entry in a React-for-Role message with a given id in the specified channel to the provided position. Uses the current channel by default.")]
        [Command("position")]
        [Alias("pos")]
        public async Task RfrPositionAsync(IMessageChannel channel, ulong msgId, string title, int position)
        {
            if(await PositionAsync(channel, "React-for-Role", msgId, title, position))
            {
                await ReplyAndDeleteAsync(database["string", "succRfrPos"], timeout: TimeSpan.FromSeconds(2));
            }
        }
        #endregion

        #region Role giving/removing
        public static async Task RfrAddRoleAsync(IEmbed embed, SocketReaction reaction)
        {
            IGuildUser user = reaction.User.Value as IGuildUser;
            dbWalrusContext database = new dbWalrusContext();

            if (user.GuildId == Convert.ToUInt64(database["config", "svgeServerId"])  // if this is the main server (means it can be used in other servers) 
                && !user.RoleIds.Contains<ulong>(Convert.ToUInt64(database["role", "communityMember"])) // and they don't have community membership
                && !user.RoleIds.Contains<ulong>(Convert.ToUInt64(database["role", "student"]) ) ) return;  // and aren't a student, then don't give a role.

            EmbedField field = embed.Fields.First(f => f.Value.StartsWith(reaction.Emote.ToString()));
            int atIndex = field.Value.IndexOf('@');
            ulong roleId = Convert.ToUInt64(field.Value.Remove(0, atIndex + 2).TrimEnd('>').ToString());
            IRole role = user.Guild.Roles.First(r => r.Id == roleId);
            await user.AddRoleAsync(role);
        }
        public static async Task RfrDelRoleAsync(IEmbed embed, SocketReaction reaction)
        {
            EmbedField field = embed.Fields.First(f => f.Value.StartsWith(reaction.Emote.ToString()));
            int atIndex = field.Value.IndexOf('@');
            ulong roleId = Convert.ToUInt64(field.Value.Remove(0, atIndex + 2).TrimEnd('>').ToString());
            IGuildUser user = reaction.User.Value as IGuildUser;
            IRole role = user.Guild.Roles.First(r => r.Id == roleId);
            await user.RemoveRoleAsync(role);
        }
        #endregion

        private IEmote parseEmote(ref string emoString)
        {
            Emote emote = null;
            Emoji emoji = null;
            if (emoString[0] == ':') emoString = '<' + emoString + '>';
            if (!Emote.TryParse(emoString, out emote))
            {
                try
                {
                    emoji = new Emoji(emoString);
                }
                catch
                {
                    return null;
                }
            }
            if (emote == null) return emoji as IEmote;
            else return emote as IEmote;
        }
    }
}
