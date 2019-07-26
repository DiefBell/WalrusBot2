using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WalrusBot2.Data;
using WalrusBot2.Modules;

namespace WalrusBot2.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private IServiceProvider _provider;

        #region Command Handling Service
        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
            _provider = provider;

            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            // Add additional initialization code here...
        }
        #endregion

        #region Event Handlers
        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            dbContextWalrus db = new dbContextWalrus();
            int argPos = 0;

            if (!message.HasStringPrefix(db["config", Program.Debug ? "botDebugPrefix" : "botPrefix"], ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString());
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IMessage message = await msg.GetOrDownloadAsync();
            switch (message.Embeds.Count)
            {
                case 0:
                    break;  // might do something with this eventually
                case 1:
                    IEmbed embed = message.Embeds.ElementAt<IEmbed>(0);
                    if (embed.Footer.ToString() == "React-for-Role Embed" && reaction.UserId != _client.CurrentUser.Id) await ReactForRole.RfrAddRoleAsync(embed, reaction);
                    break;
                default:
                    break;
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IMessage message = await msg.GetOrDownloadAsync();
            switch (message.Embeds.Count)
            {
                case 0:
                    break;  // might do something with this eventually
                case 1:
                    IEmbed embed = message.Embeds.ElementAt<IEmbed>(0);
                    if (embed.Footer.ToString() == "React-for-Role Embed" && reaction.UserId != _client.CurrentUser.Id) await ReactForRole.RfrDelRoleAsync(embed, reaction);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
