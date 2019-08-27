using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WalrusBot2.Modules
{
    [DontAutoLoad]
    [Group("meeting")]
    internal class VoteModule : XModule
    {
        // voice channel ID used as key
        private static Dictionary<ulong, Meeting> _meetings = new Dictionary<ulong, Meeting>();

        [Command("create")]
        [RequireUserRole(new string[] { "committee", "tester" })]
        [RequireContext(ContextType.Guild)]
        public async Task CreateAsync(string name, ulong chairId)
        {
            SocketGuildUser user = Context.Guild.Users.Where(u => u.Id == chairId).FirstOrDefault();
            if (user == null)
            {
                await ReplyAsync("That user is not a member of this server!");
                return;
            }
            if (user.IsBot)
            {
                await ReplyAsync("You can't set a bot as the chair!");
                return;
            }
            ISocketAudioChannel voiceChannel = user.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("That user is not currently in a voice channel!");
                return;
            }
            if (!Context.Guild.VoiceChannels.Contains(voiceChannel))
            {
                await ReplyAsync("That user currently isn't in a voice channel on this server! " +
                    "Either ask them to join a channel on this server, or run this command from their server.");
                return;
            }
            // check there isn't already a meeting for that channel, and that the chair isn't already the chair of another meeting
            _meetings.Add(voiceChannel.Id, new Meeting(name, chairId));
            await ReplyAsync($"Meeting created with {user.Username} as the chair!");
        }

        [Command("addvote")]
        public async Task AddVoteAsync(string name, [Remainder] string options)
        {
            ISocketAudioChannel voiceChannel = (Context.User as SocketGuildUser).VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("Sorry, but you can't use that command right now! " +
                    "If you're the chair of the meeting then make sure you're in the voice channel!");
            }
            if (!_meetings.ContainsKey(voiceChannel.Id))
            {
            }
        }

        #region sub-classes

        private class Meeting
        {
            public Meeting(string name, ulong chairId)
            {
                Name = name;
                ChairId = chairId;
            }

            public readonly string Name;
            public readonly ulong ChairId;

            // string is name of vote e.g. "President"
            private Dictionary<string, Vote> votes = new Dictionary<string, Vote>();

            private class Vote
            {
                private bool _isOpen = true;

                public bool IsOpen
                {
                    get { return _isOpen; }
                }

                public Vote(string name, List<string> options)
                {
                    foreach (string option in options) this.options.Add(option, 0);
                }

                public bool AddVote(string option)
                {
                    if (options.ContainsKey(option))
                    {
                        options[option]++;
                        return true;
                    }
                    return false;
                }

                public void CloseVote()
                {
                    _isOpen = false;
                }

                // option name e.g. Dief, number of votes e.g. 69
                private Dictionary<string, int> options = new Dictionary<string, int>();
            }
        }

        #endregion sub-classes
    }
}