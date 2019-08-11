using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using WalrusBot2.Data;

namespace WalrusBot2.Modules
{
    public abstract class XModule : InteractiveBase
    {
        public static CommandService Service;

        protected dbWalrusContext database = new dbWalrusContext();
    }

    public abstract class XModuleUpdatable : XModule
    {
        protected RestUserMessage _msg;
        protected Embed _oldEmbed;
        protected Embed _newEmbed;

        [Command("create")]  // these are just put in as recommended command names, they don't actually do anything here
        protected async Task<bool> CreateAsync(SocketGuildChannel channel, string footer, string title, string iconUrl, string desc)
        {
            EmbedBuilder builder = new EmbedBuilder();
            var author = new EmbedAuthorBuilder().WithName(title).WithIconUrl(iconUrl);
            builder.WithAuthor(author).WithDescription(desc).WithFooter(footer);
            var c = Context.Client.GetChannel(channel.Id) as ISocketMessageChannel;
            _msg = (RestUserMessage)await c.SendMessageAsync("", false, builder.Build());
            return true;
        }

        [Command("add")]
        [Alias("pos")]
        protected async Task<bool> AddAsync(IMessageChannel channel, ulong msgId, string footer, string title, string content, int position = 0)  // footer required to verify embed "type" (e.g. React-for-Role, server-status etc.)
        {
            if (!(await InitMessage(channel, msgId, new string[] { footer }))) return false;
            EmbedBuilder builder;

            if (position == 0) // adds it to the end of the existing embed
            {
                builder = _oldEmbed.ToEmbedBuilder();
                builder.AddField(title, content);
                _newEmbed = builder.Build();
            }
            else
            {
                if (position > 20 || position > _oldEmbed.Fields.Length + 1)
                {
                    await ReplyAsync(database["string", "errTooManyFields"]);
                    return false;
                }

                builder = new EmbedBuilder();
                builder.WithAuthor(new EmbedAuthorBuilder().WithName(_oldEmbed.Author.Value.Name).WithIconUrl(_oldEmbed.Author.Value.IconUrl));
                builder.WithFooter(footer);

                int i = 1;
                foreach (EmbedField field in _oldEmbed.Fields)
                {
                    if (i++ == position) builder.AddField(title, content);
                    if (field.Name != title) builder.AddField(field.Name, field.Value);  // allows you to move items within the embed (instead of deleting it then re-adding it)
                }
                _newEmbed = builder.Build();
            }
            await _msg.ModifyAsync(m => m.Embed = _newEmbed);
            return true;
        }

        [Command("delete")]
        [Alias(new string[] { "del", "remove", "rem" })]
        protected async Task<bool> RemoveAsync(IMessageChannel channel, ulong msgId, string footer, string title)
        {
            if (!(await InitMessage(channel, msgId, new string[] { footer }, false))) return false;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(new EmbedAuthorBuilder().WithName(_oldEmbed.Author.Value.Name).WithIconUrl(_oldEmbed.Author.Value.IconUrl));
            builder.WithFooter(footer);

            List<EmbedField> deletedFields = _oldEmbed.Fields.Where(em => em.Name == title).ToList();
            if (deletedFields.Count < 1)
            {
                await ReplyAsync(database["string", "errEntryNotInMessage"]);
                return false;
            }
            EmbedField deletedField = deletedFields[0];

            foreach (EmbedField f in _oldEmbed.Fields)
            {
                if (f.Name != title) builder.AddField(f.Name, f.Value);
            }

            _newEmbed = builder.Build();
            await _msg.ModifyAsync(m => m.Embed = _newEmbed);

            return true;
        }

        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        protected async Task<bool> MoveMessageAsync(IMessageChannel oldChannel, ulong msgId, IMessageChannel newChannel, string footer, bool delOld = true)
        {
            if (!(await InitMessage(oldChannel, msgId, new string[] { footer, "React-for-Role" }, false))) return false;  //finds and sets _msg and _oldEmbed
            if (_oldEmbed.Footer != null) if (_oldEmbed.Footer.Value.Text != "React-for-Role Embed")
                {
                    EmbedBuilder builder = _oldEmbed.ToEmbedBuilder();
                    builder.Footer.Text = "React-for-Role Embed";
                    _newEmbed = builder.Build();
                }
                else
                {
                    _newEmbed = _oldEmbed;
                }
            RestUserMessage newMsg = await newChannel.SendMessageAsync("", false, _newEmbed) as RestUserMessage;

            try
            {
                IEmote[] emotes = _msg.Reactions.Keys.ToArray();
                await newMsg.AddReactionsAsync(emotes);
                if (delOld) await _msg.DeleteAsync();
                _msg = newMsg;
            }
            catch (Exception e)
            {
                await ReplyAsync(e.Message);
                return false;
            }
            return true;
        }

        [Command("position")]
        [Alias("pos")]
        protected async Task<bool> PositionAsync(IMessageChannel channel, string footer, ulong msgId, string title, int position)
        {
            if (!(await InitMessage(channel, msgId, new string[] { footer, "React-for-Role" }, false))) return false;

            if (position > 20 || position > _oldEmbed.Fields.Length)
            {
                await ReplyAsync(database["string", "errTooManyFields"]);
                return false;
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(new EmbedAuthorBuilder().WithName(_oldEmbed.Author.Value.Name).WithIconUrl(_oldEmbed.Author.Value.IconUrl));
            builder.WithFooter(footer);

            List<EmbedField> fields = _oldEmbed.Fields.Where(em => em.Name == title).ToList();
            if (fields.Count < 1)
            {
                await ReplyAsync(database["string", "errEntryNotInMessage"]);
                return false;
            }
            EmbedField field = fields[0];

            for (int i = 0; i < _oldEmbed.Fields.Length; i++)
            {
                EmbedField f = _oldEmbed.Fields[i];
                if (f.Name != title) builder.AddField(f.Name, f.Value);
                if (i + 1 == position) builder.AddField(field.Name, field.Value);
            }
            _newEmbed = builder.Build();
            await _msg.ModifyAsync(m => m.Embed = _newEmbed);
            return true;
        }

        protected async Task<bool> InitMessage(IMessageChannel channel, ulong msgId, string[] footer, bool checkLen = true)
        {
            try
            {
                _msg = await channel.GetMessageAsync(msgId) as RestUserMessage;
            }
            catch
            {
                await ReplyAsync(database["string", "errParseMsgId"]);
            }
            if (_msg == null)
            {
                await ReplyAsync(database["string", "errMsgNotFound"]);
                return false;
            }
            if (_msg.Embeds.Count != 1)
            {
                await ReplyAsync(database["string", "errMsgNotValid"]);
                return false;
            }
            _oldEmbed = _msg.Embeds.ElementAt(0);
            //don't run this on delete or move
            if (_oldEmbed.Fields.Length >= 20 && checkLen)
            {
                await ReplyAsync(database["string", "errTooManyFields"]);
                return false;
            }
            if (!footer.Contains(_oldEmbed.Footer.Value.ToString()))
            {
                await ReplyAsync(database["string", "errMsgNotValid"]);
            }
            return true;
        }
    }

    public class RequireUserRole : PreconditionAttribute
    {
        private readonly string[] _roles;

        public RequireUserRole(string[] roles)
        {
            _roles = roles;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var user = context.User as IGuildUser;
            if (user == null) return PreconditionResult.FromError("This command cannot be called from outside a Discord server!");
            var guild = user.Guild;

            List<ulong> roleIds = new List<ulong>();
            dbWalrusContext database = new dbWalrusContext();

            foreach (string role in _roles)
            {
                ulong roleId = 0;
                try { roleId = Convert.ToUInt64(database["role", role]); }
                catch { Console.WriteLine($"The role {role} is given in a RequireRole attribute but you haven't added it the the MySQL database!"); }
                if (guild.Roles.Any(r => r.Id == roleId)) roleIds.Add(roleId);
            }
            if (roleIds.Count < 1) return PreconditionResult.FromError($"The guild does not have the role any of the roles required to access this command.");

            return user.RoleIds.Any(rId => roleIds.Contains(rId)) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You do not have the sufficient role required to access this command.");
        }
    }

    public class TimerService
    {
        private readonly List<TimerInfo> _timers = new List<TimerInfo>();

        public class TimerInfo
        {
            public TimerInfo(Timer t, TimeCalcFunction st, TimeCalcFunction ri)
            {
                timer = t;
                StartTime = st;
                RepeatInterval = ri;
            }

            public Timer timer;

            public delegate TimeSpan TimeCalcFunction();

            public TimeCalcFunction StartTime;
            public TimeCalcFunction RepeatInterval;
        }

        public TimerService()
        {
        }

        public void AddTimer(TimerInfo timerInfo)
        => _timers.Add(timerInfo);

        public void Stop() // 6) Example to make the timer stop running
        {
            foreach (TimerInfo t in _timers) t.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart() // 7) Example to restart the timer
        {
            foreach (TimerInfo t in _timers) t.timer.Change(t.StartTime(), t.RepeatInterval());
        }
    }
}