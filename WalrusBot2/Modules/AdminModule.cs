using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalrusBot2.Modules
{
    [Group("admin")]
    [RequireUserRole(new string[] { "committee", "tester" })]
    [RequireContext(ContextType.Guild)]
    public class AdminModule : XModule
    {
        #region Purge

        [Command("purge", RunMode = RunMode.Async)]
        [Summary("Delete n messages (97 max) from the given channel.")]
        public async Task PurgeAsync(IMessageChannel channel, int n)
        {
            await ReplyAndDeleteAsync($"Are you sure you want to delete {n} messages from {channel.Name}? Type \"yes\" to confirm or \"no\" to cancel.", timeout: TimeSpan.FromSeconds(31));
            var response = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(30));
            if (response == null) return;
            if (response.Content.ToLower() != "yes")
            {
                await ReplyAndDeleteAsync("Purge cancelled.", timeout: TimeSpan.FromSeconds(5));
                return;
            }

            n = n >= 97 ? 100 : n + 3;
            var messages = await Context.Channel.GetMessagesAsync(n).FlattenAsync();
            foreach (IMessage message in messages) await message.DeleteAsync();
            await ReplyAndDeleteAsync("Messages deleted...", timeout: TimeSpan.FromSeconds(3));
        }

        [Command("purge", RunMode = RunMode.Async)]
        [Summary("Delete n messages (97 max) from the current channel.")]
        public async Task PurgeAsync(int n)
            => await PurgeAsync(Context.Channel, n);

        #endregion Purge

        #region Popularity

        [Command("popularity", RunMode = RunMode.Async)]
        [Alias("pop")]
        [Summary("Lists the n channels (20 max) with the oldest most-recent messages, excluding channels in the provided categories, " +
            "including the date of the most recent message and the number of users able to view that channel. Ignores channels with names including \"ignoreTerms\".")]
        public async Task PopularityAsync(int n, string channelIgnoreTerms, string categoryIgnoreTerms, [Remainder] string exclude)
        {
            List<ulong> excludeIds = new List<ulong>();
            foreach (string s in exclude.Split(' '))
            {
                if (UInt64.TryParse(s, out ulong u)) excludeIds.Add(u);
                else
                {
                    await ReplyAsync($"Failed to parse exclusion ID {s} at position {excludeIds.Count + 1}. Please check that it's a valid number!");
                    return;
                }
            }
            List<string> chanIgnoreStrings = channelIgnoreTerms.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> catIgnoreStrings = categoryIgnoreTerms.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            IGuild guild = Context.Guild;  // don't need to check null as must be called from guild
            List<KeyValuePair<IGuildChannel, DateTime>> channels = new List<KeyValuePair<IGuildChannel, DateTime>>();

            IMessage alertMsg = await ReplyAsync("Sorting channels by oldest last message. This may take a while, please wait...");

            foreach (IMessageChannel c in await guild.GetTextChannelsAsync())
            {
                if (excludeIds.Contains(c.Id)) continue;
                if (chanIgnoreStrings.Any(s => c.Name.ToLower().Contains(s.ToLower()))) continue;  // don't check announcement channels as they tend not to be used as often

                ulong? category = (c as SocketTextChannel).CategoryId;
                if (category == null) continue;  // not going to check anything that's not in a category
                if (excludeIds.Contains(category.Value)) continue;
                if (catIgnoreStrings.Any(s => (c as SocketTextChannel).Category.Name.ToLower().Contains(s.ToLower()))) continue;

                var msg = (await c.GetMessagesAsync(1).FlattenAsync())?.FirstOrDefault();
                DateTime ts = msg != null ? msg.Timestamp.DateTime : c.CreatedAt.DateTime;

                channels.Add(new KeyValuePair<IGuildChannel, DateTime>(c as IGuildChannel, ts));
            }
            channels.Sort((x, y) => DateTime.Compare(x.Value, y.Value));

            EmbedBuilder builder = new EmbedBuilder();
            n = n <= 20 ? n : 20;
            n = n <= channels.Count ? n : channels.Count;
            for (int i = 0; i < n; i++)
            {
                IGuildChannel c = channels[i].Key as IGuildChannel;
                DateTime dt = channels[i].Value;
                int numUsers = (await c.GetUsersAsync().FlattenAsync()).Count();
                builder.AddField($"{(c as SocketTextChannel).Category.Name}/{c.Name}", $"{(c as SocketTextChannel).Mention}\n" + $"Number of users: {numUsers}.\n" +
                    $"Last message at {dt.ToString()}.\n" +
                    $"Channel created at {c.CreatedAt.DateTime}");
            }

            await alertMsg.DeleteAsync();
            await Context.Channel.SendMessageAsync("*Most recent activity of channels in this server:*", false, builder.Build());
        }

        #endregion Popularity

        [Command("resetserverperms")]
        [Name("Reset Server Permissions")]
        [Summary("Resets the view permissions for all channels in the guild to a specified list of roles, except for the categories and channels provided")]
        /// <sudo>
        /// Foreach channel:
        ///     Is it in the list of exempt channels? continue;
        ///     Is its category in the list of exempt categories? continue;
        ///     Set it to only be seeable by given roles (e.g. student, alumni, community member)
        /// </sudo>
        public async Task ResetGuildPermsAsync(string roles, string categories, string channels)
        {
        }
    }
}