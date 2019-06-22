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
    public class ReactForRole : XModule
    {

        
        [Command("create")]
        public async Task RfrCreateAsync(SocketGuildChannel channel, string title, string desc)
             => await RfrCreateAsync(channel, title, database["config", "rfrDefaultIconUrl"], desc);
        [Command("create")]
        public async Task RfrCreateAsync(SocketGuildChannel channel, string title, string iconUrl, string desc)
            => await generateRfrMessage(channel, title, iconUrl, desc);
        [Command("create")]
        public async Task RfrCreateAsync(string title, string desc)
             => await RfrCreateAsync(Context.Channel as SocketGuildChannel, title, database["config", "rfrDefaultIconUrl"], desc);
        [Command("create")]
        public async Task RfrCreateAsync(string title, string iconUrl, string desc)
            => await generateRfrMessage(Context.Channel as SocketGuildChannel, title, iconUrl, desc);

        private async Task<SocketUserMessage> generateRfrMessage(SocketGuildChannel channel, string title, string iconUrl, string desc)
        {
            EmbedBuilder builder = new EmbedBuilder();
            var author = new EmbedAuthorBuilder().WithName(title).WithIconUrl(iconUrl);
            builder.WithAuthor(author).WithDescription(desc).WithFooter("React-for-Role");
            var c = Context.Client.GetChannel(channel.Id) as ISocketMessageChannel;
            var msg = (IUserMessage)await c.SendMessageAsync("", false, builder.Build());
            return msg as SocketUserMessage;
        }

        [Command("add")]
        public async Task RfrAddAsync(ulong msgId, string roleDisplayName, string roleEmote, string roleIdString)
            => await RfrAddAsync(Context.Channel, msgId, roleDisplayName, roleEmote, roleIdString);
        [Command("add")]
        public async Task RfrAddAsync(IMessageChannel channel, ulong msgId, string roleDisplayName, string roleEmote, string roleIdString)
        {
            #region Role Validity
            UInt64 roleId;
            try
            {
                roleId = Convert.ToUInt64(roleIdString);
            }
            catch
            {
                await ReplyAsync("Failed to parse the supplied Role ID string. Double check you've copied it correctly!");
                return;
            }
            IRole role = (channel as IGuildChannel).Guild.GetRole(roleId);
            if (role == null)
            {
                await ReplyAsync("That doesn't seem to be a valid role for this server! Double check you've copied the role name correctly.");
                return;
            }
            #endregion
            #region Emote Validity
            Emote emote = null;
            Emoji emoji = null;
            if (roleEmote[0] == ':') roleEmote = '<' + roleEmote + '>';
            if (!Emote.TryParse(roleEmote, out emote))
            {
                try
                {
                    emoji = new Emoji(roleEmote);
                }
                catch
                {
                    await ReplyAsync("It appears that you're using an invalid emoji! If you're using an escaped string, double check it's correct.");
                    return;
                }
            }
            IEmote reactEmote;
            if (emote == null) reactEmote = emoji;
            else reactEmote = emote;
            #endregion
            RestUserMessage msg = await channel.GetMessageAsync(msgId) as RestUserMessage;
            #region Confirm Valid Message
            if (msg.Embeds.Count != 1)
            {
                await ReplyAsync("The message you've supplied doesn't seem to have the right number of embeds, double check you've got the right message ID! " +
                    $"You may need to create the message first with `{database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"]}rfr add`.");
                return;
            }
            Embed embed = msg.Embeds.ElementAt(0) as Embed;
            if (embed.Fields.Length >= 20)
            {
                await ReplyAsync("That message already has 20 fields, which is the most that it can support. " +
                    $"Create a new React-for-Role message using `{database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"]}rfr add`.");
                return;
            }
            #endregion
            EmbedBuilder builder = embed.ToEmbedBuilder();
            builder.AddField(roleDisplayName, $"{roleEmote} {role.Mention}");
            embed = builder.Build();
            await msg.ModifyAsync(m => m.Embed = embed);
            await msg.AddReactionAsync(reactEmote);
        }

        [Command("remove")]
        [Alias("rem", "delete", "del")]
        public async Task RfrRemoveAsync(ulong msgId, string roleDisplayName)
            => await RfrRemoveAsync(Context.Channel, msgId, roleDisplayName);

        [Command("remove")]
        [Alias("rem", "delete", "del")]
        public async Task RfrRemoveAsync(IMessageChannel channel, ulong msgId, string roleDisplayName)
        {
            RestUserMessage msg = await channel.GetMessageAsync(msgId) as RestUserMessage;
            #region Confirm Valid Message
            if (msg.Embeds.Count != 1)
            {
                await ReplyAsync("The message you've supplied doesn't seem to have the right number of embeds, double check you've got the right message ID! ");
                return;
            }
            Embed embed = msg.Embeds.ElementAt(0) as Embed;
            if (embed.Fields.Length < 1)
            {
                await ReplyAsync("There are no roles in that message yet. " +
                    $"Try adding some with `{database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"]}rfr add` first.");
                return;
            }
            #endregion
            EmbedBuilder builder = new EmbedBuilder();
            EmbedAuthorBuilder author = new EmbedAuthorBuilder().WithName(embed.Author.Value.Name).WithIconUrl(embed.Author.Value.IconUrl);
            builder.WithAuthor(author).WithFooter("React-for-Role").WithDescription(embed.Description);
            EmbedField deletedField = new EmbedField();
            foreach (EmbedField field in embed.Fields)
            {
                if (field.Name != roleDisplayName) builder.AddField(field.Name, field.Value);
                else
                {
                    deletedField = field;
                    break;
                }
            }
            if (deletedField.Name != roleDisplayName)
            {
                await ReplyAsync($"The role \"{roleDisplayName}\" doesn't even exist in the supplied message. Please check your spelling!");
                return;
            }
            embed = builder.Build();
            await msg.ModifyAsync(m => m.Embed = embed);
            // get emoji
            string emo = deletedField.Value.Split(' ')[0];
            Emote emote = null;
            Emoji emoji = null;
            if (emo[0] == ':') emo = '<' + emo + '>';
            if (!Emote.TryParse(emo, out emote))
            {
                try
                {
                    emoji = new Emoji(emo);
                }
                catch
                {
                    await ReplyAsync("There was an issue with removing the react for that role, so please go do that manually :)");
                    return;
                }
            }
            IEmote reactEmote;
            if (emote == null) reactEmote = emoji;
            else reactEmote = emote;
            List<IUser> users = await msg.GetReactionUsersAsync(reactEmote, msg.Reactions[reactEmote].ReactionCount).Flatten().ToList();
            foreach (IUser user in users)
            {
                await msg.RemoveReactionAsync(reactEmote, user);
            }
        }

        [Command("reposition")]
        [Alias("pos")]
        public async Task RfrRepositionAsync(ulong msgId, string roleDisplayName, int position)
            => await RfrRepositionAsync(Context.Channel, msgId, roleDisplayName, position);
        [Command("reposition")]
        [Alias("pos")]
        public async Task RfrRepositionAsync(IMessageChannel channel, ulong msgId, string roleDisplayName, int position)
        {
            RestUserMessage msg = await channel.GetMessageAsync(msgId) as RestUserMessage;
            #region Confirm Valid Message
            if (msg.Embeds.Count != 1)
            {
                await ReplyAsync("The message you've supplied doesn't seem to have the right number of embeds, double check you've got the right message ID! " +
                    $"You may need to create the message first with `{database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"]}rfr add`.");
                return;
            }
            Embed embed = msg.Embeds.ElementAt(0) as Embed;
            if (position > embed.Length)
            {
                await ReplyAsync("The position you've specified is greater than the number of roles in the React-for-Role embed! Please try again with a lower number... ");
                return;
            }
            if(embed.Fields == null)
            {
                await ReplyAsync("That doesn't appear to be a valid React-for-Role message. Double check you've got the corrent message ID!");
                return;
            }
            if (embed.Footer.Value.ToString() != "React-for-Role")  // throws an exception if the Embed has no footer...
            {
                await ReplyAsync("That doesn't appear to be a valid React-for-Role message. Double check you've got the corrent message ID!");
                return;
            }
            #endregion
            EmbedBuilder builder = new EmbedBuilder();
            EmbedAuthorBuilder author = new EmbedAuthorBuilder().WithName(embed.Author.Value.Name).WithIconUrl(embed.Author.Value.IconUrl);
            builder.WithAuthor(author).WithFooter("React-for-Role").WithDescription(embed.Description);
            EmbedField moveField = embed.Fields.Where(x => x.Name == roleDisplayName).First();
            int i = 1;
            foreach (EmbedField field in embed.Fields)
            {
                if (position == i++) builder.AddField(moveField.Name, moveField.Value);
                if (field.Name != roleDisplayName) builder.AddField(field.Name, field.Value);
            }
            embed = builder.Build();
            await msg.ModifyAsync(m => m.Embed = embed);
        }

        [Command("move")]
        [Alias("mv")]
        public async Task RfrMoveAsync(IMessageChannel fromChannel, ulong msgId, bool delOld=true)
            => await RfrMoveAsync(fromChannel, msgId, Context.Channel as IMessageChannel, delOld);
        [Command("move")]
        [Alias("mv")]
        public async Task RfrMoveAsync(ulong msgId, IMessageChannel toChannel, bool delOld=true)
            => await RfrMoveAsync(Context.Channel, msgId, toChannel, delOld);
        [Command("move")]
        [Alias("mv")]
        public async Task RfrMoveAsync(IMessageChannel fromChannel, ulong msgId, IMessageChannel toChannel, bool delOld=true)
        {
            RestUserMessage oldMsg = await fromChannel.GetMessageAsync(msgId) as RestUserMessage;
            #region Confirm Valid Message
            if (oldMsg.Embeds.Count != 1)
            {
                await ReplyAsync("The message you've supplied doesn't seem to have the right number of embeds, double check you've got the right message ID! " +
                    $"You may need to create the message first with `{database["config", Program.Debug ? "botDebugPrefix" : "botPrefix"]}rfr add`.");
                return;
            }
            Embed embed = oldMsg.Embeds.ElementAt(0) as Embed;
            if (embed.Fields == null)
            {
                await ReplyAsync("That doesn't appear to be a valid React-for-Role message. Double check you've got the corrent message ID!");
                return;
            }
            if (embed.Footer.Value.ToString() != "React-for-Role")  // throws an exception if the Embed has no footer...
            {
                await ReplyAsync("That doesn't appear to be a valid React-for-Role message. Double check you've got the corrent message ID!");
                return;
            }
            #endregion
            var newMsg = await toChannel.SendMessageAsync("", false, embed);
            List<IEmote> emotes = oldMsg.Reactions.Keys.ToList();
            //foreach
        }


    }
}
