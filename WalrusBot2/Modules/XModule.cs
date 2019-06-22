using System;
using System.Collections.Generic;
using System.Linq;
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

        protected dbContextWalrus database = new dbContextWalrus();
    }
    public abstract class XModuleUpdatable : XModule
    {
        protected RestUserMessage _msg;
        protected Embed _embed;

        protected async Task CreateAsync()
        {

        }
        protected async Task<bool> AddAsync(IMessageChannel channel, ulong msgId, string footer, string title, string content, int position=0)
        {
            if (!(await InitMessage(channel, msgId, footer) ) ) return false;
            EmbedBuilder builder;

            if (position == 0)
            {
                builder = _embed.ToEmbedBuilder();
                builder.AddField(title, content);
                _embed = builder.Build();

            }
            else
            {
                if (position > 20 || position > _embed.Fields.Length)
                {
                    await ReplyAsync(database["string", "errIndexTooHigh"]);
                    return false;
                }

                builder = new EmbedBuilder();
                builder.WithAuthor(new EmbedAuthorBuilder().WithName(_embed.Author.Value.Name).WithIconUrl(_embed.Author.Value.IconUrl));
                builder.WithFooter(footer);

                int i = 1;
                foreach (EmbedField field in _embed.Fields)
                {
                    if (position == 1 + i++) builder.AddField(title, content);
                    if (field.Name != title) builder.AddField(field.Name, field.Value);
                }
            }
            await _msg.ModifyAsync(m => m.Embed = _embed);
            return true;
        }
        protected async Task<bool> RemoveAsync(IMessageChannel channel, ulong msgId, string footer, string title)
        {
            if (!(await InitMessage(channel, msgId, footer))) return false;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(new EmbedAuthorBuilder().WithName(_embed.Author.Value.Name).WithIconUrl(_embed.Author.Value.IconUrl));
            builder.WithFooter(footer);
            EmbedField deletedField = new EmbedField();
            foreach(EmbedField field in _embed.Fields)
            {
                if (field.Name != title) builder.AddField(field.Name, field.Value);
                else
                {
                    deletedField = field;
                    break;
                }
            }
            if(deletedField.Name != title)
            {
                await ReplyAsync(database["string", "errRoleNotInMessage"]);
                return false;
            }
            _embed = builder.Build();
            await _msg.ModifyAsync(m => m.Embed = _embed);

            return true;
        }

        protected async Task MoveAsync()
        {

        }

        protected async Task<bool> InitMessage(IMessageChannel channel, ulong msgId, string footer)
        {
            _msg = await channel.GetMessageAsync(msgId) as RestUserMessage;
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
            _embed = _msg.Embeds.ElementAt(0);
            if (_embed.Fields.Length >= 20)
            {
                await ReplyAsync(database["string", "errTooManyFields"]);
                return false;
            }
            if (_embed.Footer.Value.ToString() != footer)
            {
                await ReplyAsync(database["string", "errWrongFooter"]);
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
            dbContextWalrus database = new dbContextWalrus();

            foreach (string role in _roles)
            {
                ulong roleId = 0;
                try { roleId = Convert.ToUInt64(database["role", role]); }
                catch { Console.WriteLine($"The role {role} is given in a RequireRole attribute but you haven't added it the the MySQL database!"); }
                if (guild.Roles.Any(r => r.Id == roleId) ) roleIds.Add(roleId);
            }
            if(roleIds.Count < 1) return PreconditionResult.FromError($"The guild does not have the role any of the roles required to access this command.");

            return user.RoleIds.Any(rId => roleIds.Contains(rId) ) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("You do not have the sufficient role required to access this command.");
        }
    }
}
