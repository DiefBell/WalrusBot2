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
            if(roleEmote[0] == ':') roleEmote = '<' + roleEmote + '>';
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
            if(embed.Fields.Length >= 20)
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
            EmbedBuilder builder = new EmbedBuilder();
            EmbedAuthorBuilder author = new EmbedAuthorBuilder().WithName(embed.Author.Value.Name).WithIconUrl(embed.Author.Value.IconUrl);
            builder.WithAuthor(author).WithFooter("React-for-Role").WithDescription(embed.Description);
            bool deleted = false;
            foreach (EmbedField field in embed.Fields)
            {
                if (field.Name != roleDisplayName) builder.AddField(field.Name, field.Value);
                else deleted = true;
            }
            if(!deleted)
            {
                await ReplyAsync($"The role \"{roleDisplayName}\" doesn't even exist in the supplied message. Please check your spelling!");
                return;
            }
            embed = builder.Build();
            await msg.ModifyAsync(m => m.Embed = embed);
        }


    }
}
