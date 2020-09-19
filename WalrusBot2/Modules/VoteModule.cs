using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WalrusBot2.Modules
{
    [Group("vote")]
    public class VoteModule : XModule
    {
        private static ConcurrentDictionary<string, Vote> _votes = new ConcurrentDictionary<string, Vote>();
        private const int TIMEOUT = 45;

        #region Commands

        [Command("create", RunMode = RunMode.Async)]
        [RequireUserRole(new string[] { "committee", "tester", "chief" })]
        [RequireContext(ContextType.Guild)]
        public async Task CreateVote(string voteName, string voteDescription)
            => await CreateVote(voteName, voteDescription, 1);

        [Command("create", RunMode = RunMode.Async)]
        [RequireUserRole(new string[] { "committee", "tester", "chief" })]
        [RequireContext(ContextType.Guild)]
        public async Task CreateVote(string voteName, string voteDescription, int numVotes)
        {
            await ReplyAsync($"We're now setting up a vote for **{voteName}**. Type \"cancel\" at any time to cancel this process. " +
                $"All replies have a {TIMEOUT.ToString()} second timout after which this command will reset.\n\nDo voters need to be in a specific channel? " +
                $"Either type \"no\" or the channel ID.");

            SocketMessage reply;
            voteName = voteName.Trim();

            #region Voice Channel

            SocketVoiceChannel voiceChannel = null;
            while (true)
            {
                reply = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(TIMEOUT));

                if (reply == null)
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "cancel")
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "no")
                {
                    await ReplyAsync("Okay, voters don't need to be in a particular voice channel.\n\nWho will be chairing this vote? " +
                    "This person will be the only one capable of closing the vote and will have the deciding vote if there is a draw.");
                    break;
                }
                if (UInt64.TryParse(reply.Content, out ulong voiceChannelId))
                {
                    voiceChannel = Context.Guild.VoiceChannels.Where(c => c.Id == voiceChannelId).FirstOrDefault();
                    if (voiceChannel == null)
                    {
                        await ReplyAsync("The channel ID you've supplied doesn't seem to be a voice channel in this server! Please try again.");
                    }
                    else
                    {
                        await ReplyAsync($"Okay, only users in the {voiceChannel.Name} voice channel will be asked to vote.\n\nWho will be chairing this vote? " +
                    "This person will be the only one capable of closing the vote and will have the deciding vote if there is a draw.");
                        break;
                    }
                }
                else
                {
                    await ReplyAsync("The channel ID you've supplied doesn't appear to be a valid ID. " +
                        "This should be a number, so please double check you've copied it correctly.");
                }
            }

            #endregion Voice Channel

            #region Chair

            IGuildUser chair;
            while (true)
            {
                reply = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(TIMEOUT));

                if (reply == null)
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "cancel")
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                chair = reply.MentionedUsers.FirstOrDefault() as IGuildUser;
                if (chair != null)
                {
                    await ReplyAsync($"Okay, the chair is set to {chair.Username}." +
                        $"\n\nWhat role(s) must people have to be included in this vote? Simply @ the roles with spaces between them.");
                    break;
                }
                else
                {
                    await ReplyAsync("That doesn't appear to be a valid user. Please try again!");
                }
            }

            #endregion Chair

            #region Roles

            List<IRole> roles;
            while (true)
            {
                reply = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(TIMEOUT));

                if (reply == null)
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "cancel")
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                roles = reply.MentionedRoles.ToList<IRole>(); ;
                if (roles != null)
                {
                    await ReplyAsync("Okay, people with the role these roles will be asked to vote.\n\n" +
                        "Please add an option, or type \"done\" if you're finished adding options. " +
                        "Add these by typing [option name] + [option description]. You don't need square brackets [] but you do need the '+' symbol. " +
                        "You may want use use a URL in the description.");
                    break;
                }
                else
                {
                    await ReplyAsync("That doesn't appear to be a valid user. Please try again!");
                }
            }

            #endregion Roles

            #region Options

            List<OptionInfo> options = new List<OptionInfo>();
            while (reply.Content.ToLower() != "done" || options.Count >= 10)
            {
                reply = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(TIMEOUT));

                if (reply == null)
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "cancel")
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }

                if (reply.Content.ToLower() == "done")
                {
                    if (options.Count < 2)
                    {
                        await ReplyAsync("You need at least two options for people to vote on!");
                    }
                    else break;
                }
                if (reply.Content.Split('+').Length != 2)
                {
                    await ReplyAsync("I had an issue parsing your option. This may be caused if you've used a '+' in your description or omitted the description. " +
                        "Please try again.");
                }
                else
                {
                    string name = reply.Content.Split('+')[0].Trim(' ');
                    string desc = reply.Content.Split('+')[1].Trim(' ');
                    //name = name.Replace('-', '_');
                    options.Add(new OptionInfo() { name = name, description = desc });
                    string optionInfoString = $"The vote option **{reply.Content.Split('+')[0]}** has been added to the list of choices.";

                    if (options.Count >= 10)
                    {
                        await ReplyAsync(optionInfoString + " You've reached the maximum number of vote options (10).");
                        break;
                    }
                    await ReplyAsync(optionInfoString + " Please continue to add options or type \"done\" if you've finished.");
                }
            }

            if (numVotes < 1) numVotes = 1;
            if (numVotes > options.Count) numVotes = options.Count;

            #endregion Options

            #region Confirm

            #region Embed and Reacts

            EmbedBuilder builder = new EmbedBuilder();
            var author = new EmbedAuthorBuilder().WithName(voteName).WithIconUrl(
                "https://d2gg9evh47fn9z.cloudfront.net/800px_COLOURBOX6244939.jpg");
            builder.WithAuthor(author);
            builder.WithFooter($"Test - {voteName}");
            builder.WithDescription(voteDescription + "\n\u200B");

            int i = options.Count == 10 ? 0 : 1;
            string voteInfoField = "";
            foreach (OptionInfo option in options)
            {
                builder.AddField(i++.ToString() + " - " + option.name, option.description + "\n\u200B");
                voteInfoField += $"**{option.name}**: {cross.ToString()}\n";
            }

            string maxVotes = numVotes + " choice" + (numVotes == 1 ? "" : "s");
            builder.AddField($"\u200B\nYou have used 0 / {maxVotes}. Your Votes:", voteInfoField);

            List<IEmote> reactions = new List<IEmote>();
            bool zeroSet = options.Count == 10;
            for (int r = 0; r < options.Count; r++)
            {
                int n = r + (zeroSet ? 0 : 1);
                reactions.Add(_emojis[n]);
            }

            #endregion Embed and Reacts

            while (reply.Content.ToLower() != "yes")
            {
                var testMsg = await ReplyAsync("Below is an example of what will be sent out to users.", false, builder.Build());
                await testMsg.AddReactionsAsync(reactions.ToArray());
                await ReplyAsync("Are you ready to send this out? Type \"yes\" when ready!");

                reply = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(TIMEOUT));
                if (reply == null)
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
                if (reply.Content.ToLower() == "cancel")
                {
                    await ReplyAsync("Vote creation cancelled.");
                    return;
                }
            }
            await ReplyAsync("Okay, I'll create your vote then DM the people you've selected!");

            #endregion Confirm

            #region Send Votes

            Dictionary<string, List<ulong>> votes = new Dictionary<string, List<ulong>>();
            foreach (var v in options)
            {
                votes.Add(v.name, new List<ulong>());
            }
            if (!_votes.TryAdd(voteName, new Vote
            {
                MaxVotes = numVotes,
                ChairId = chair.Id,
                Votes = votes
            }))
            {
                await ReplyAsync("Something's gone wrong adding your vote! I'm so sorry, but you'll have to try again :/");
                return;
            }
            List<SocketGuildUser> users;
            if (voiceChannel == null)
            {
                Console.WriteLine("Channel null");
                users = Context.Guild.Users.Where(u => u.Roles.Intersect(roles).Any()).Distinct().ToList();
            }
            else
            {
                Console.WriteLine("Channel not null");
                users = voiceChannel.Users.Where(u => u.Roles.Intersect(roles).Any()).Distinct().ToList();
            }
            foreach (SocketUser user in users)
            {
                if (user.IsBot) continue;
                builder.WithFooter($"Vote - {voteName}");

                var dmChannel = await user.GetOrCreateDMChannelAsync();
                try
                {
                    var msg = await dmChannel.SendMessageAsync(
                        "You've been asked to participate in the following vote. " +
                        "To vote, simply click on the corresponding reaction to this message. " +
                        $"You may vote for up to {numVotes} option{(numVotes > 1 ? "s" : "")}.",
                        false, builder.Build());

                    await msg.AddReactionsAsync(reactions.ToArray());
                }
                catch
                {
                    //
                }
            }
            await ReplyAsync($"Your vote has been added and sent out to {users.Count()} individuals!");

            #endregion Send Votes
        }

        [Command("close")]
        [Alias("end")]
        [RequireContext(ContextType.DM)]
        public async Task EndVoteAsync([Remainder] string voteName)
        {
            if (!_votes.ContainsKey(voteName))
            {
                await ReplyAsync("That vote name doesn't exist! Please check you've spelt it the same way as when it was first created.");
                return;
            }
            if (_votes.TryGetValue(voteName, out Vote vote))
            {
                if (vote.ChairId != Context.User.Id)
                {
                    await ReplyAsync("You are not the chair of this vote. Only the chair may close the vote.");
                    return;
                }
                EmbedBuilder builder = new EmbedBuilder()
                .WithTitle(voteName)
                .WithDescription("This vote has  closed with the following number of votes for each option:\n\u200B");
                foreach (var v in vote.Votes)
                {
                    int votes = v.Value.Count;
                    builder.AddField(v.Key, votes + " " + (votes == 1 ? "vote" : "votes"));
                }
                await ReplyAsync("**Vote Results**", false, builder.Build());
                if (!_votes.TryRemove(voteName, out Vote vo))
                {
                    await ReplyAsync("The vote has ended, however there's been an issue deleting it from my list of votes. " +
                        "Please reset my program at a convenient opportunity.");
                }
            }
        }

        [Command("info")]
        [RequireContext(ContextType.DM)]
        public async Task VoteInfoAsync([Remainder] string voteName)
        {
            if (!_votes.ContainsKey(voteName))
            {
                await ReplyAsync("That vote name doesn't exist! Please check you've spelt it the same way as when it was first created.");
                return;
            }
            if (_votes.TryGetValue(voteName, out Vote vote))
            {
                if (vote.ChairId != Context.User.Id)
                {
                    await ReplyAsync("You are not the chair of this vote. Only the chair may close the vote.");
                    return;
                }

                List<ulong> voteUsers = new List<ulong>();
                foreach (var v in vote.Votes)
                {
                    voteUsers.AddRange(v.Value);
                }
                await ReplyAsync($"This vote has had {voteUsers.Count} votes cast by {voteUsers.Distinct()} people.");
            }
        }

        #endregion Commands

        #region ReactHandlers

        public static async Task AddVote(IUserMessage msg, SocketReaction reaction)
        {
            Embed embed = msg.Embeds.ElementAt(0) as Embed;
            string voteName = embed.Footer.ToString().Split(new[] { '-' }, 2)[1].Trim();
            //Console.WriteLine($"Vote added for {voteName}.");

            EmbedBuilder builder = embed.ToEmbedBuilder();

            if (!_votes.TryGetValue(voteName, out Vote vote))
            {
                builder.Fields[builder.Fields.Count - 1].Value = "Unfortunately this vote has now closed!";
                builder.WithFooter("Closed Vote");
                await msg.ModifyAsync(m => m.Embed = builder.Build());
                return;
            }

            if (reaction.UserId == vote.ChairId)
            {
                builder.Fields[builder.Fields.Count - 1].Name = "Error: You are the vote chair!";
                builder.Fields[builder.Fields.Count - 1].Value = "Please not that the chair of a vote may only vote when there is a tie, " +
                    "which must be done after the vote is closed.";
                await msg.ModifyAsync(m => m.Embed = builder.Build());
                return;
            }

            // get the option name from the react
            string optionNumberString = reaction.Emote.ToString().Trim('?');
            if (!(optionNumberString.Length != 1)) return;// they've probably reacted with something else
            char optionNumber = optionNumberString[0];

            var fieldBuilder = builder.Fields.Where(f => f.Name[0] == optionNumber).FirstOrDefault();
            if (fieldBuilder == null) return; // probably means they've reacted with something else or a number that isn't an option
            string optionName = fieldBuilder.Build().Name.Split(new[] { '-' }, 2)[1].Trim(' ');

            // check if they've already voted for this, return if they have
            if (!vote.Votes.ContainsKey(optionName)) return; // probably some error with another react being used
            if (vote.Votes[optionName].Contains(reaction.UserId)) return;

            // see how many options this user has already voted for
            int numVotes = vote.Votes.Values.Where(v => v.Contains(reaction.UserId)).Count();
            if (numVotes >= vote.MaxVotes)
            {
                // tell them they can't do this, and need to remove previous first
                builder.Fields[builder.Fields.Count - 1].Name = "Error: You've already used the maximum number of votes! " +
                    "Please remove other votes to set the one you just chose!";
                await msg.ModifyAsync(m => m.Embed = builder.Build());
            }
            else
            {
                // add their vote
                vote.Votes[optionName].Add(reaction.UserId);
                // update the vote info
                string maxVotes = vote.MaxVotes + " choice" + (vote.MaxVotes > 1 ? "s" : "");
                builder.Fields[builder.Fields.Count - 1].Name = $"\u200B\nYou have used {numVotes + 1} / {maxVotes}. Your Votes:";
                string voteInfo = "";
                foreach (var v in vote.Votes)
                {
                    voteInfo += $"**{v.Key}**: {(v.Value.Contains(reaction.UserId) ? tick.ToString() : cross.ToString())}\n";
                }
                builder.Fields[builder.Fields.Count - 1].Value = voteInfo;
                // update embed
                await msg.ModifyAsync(m => m.Embed = builder.Build());
            }
        }

        public static async Task DelVote(IUserMessage msg, SocketReaction reaction)
        {
            Embed embed = msg.Embeds.ElementAt(0) as Embed;
            string voteName = embed.Footer.ToString().Split(new[] { '-' })[1].Trim();
            //Console.WriteLine($"Vote removed for {voteName}.");

            EmbedBuilder builder = embed.ToEmbedBuilder();

            if (!_votes.TryGetValue(voteName, out Vote vote))
            {
                builder.Fields[builder.Fields.Count - 1].Value = "Unfortunately this vote has now closed!";
                await (msg as RestUserMessage).ModifyAsync(m => m.Embed = builder.Build());
                return;
            }

            if (reaction.UserId == vote.ChairId) return; // no point alerting them, if they've reacted all ready then it will already have a warning there

            // get the option name from the react
            string optionNumberString = reaction.Emote.ToString().Trim('?');
            if (!(optionNumberString.Length != 1)) return; // they've probably reacted with something else
            char optionNumber = optionNumberString[0];

            var fieldBuilder = builder.Fields.Where(f => f.Name[0] == optionNumber).FirstOrDefault();
            if (fieldBuilder == null) return; // probably means they've reacted with something else or a number that isn't an option
            string optionName = fieldBuilder.Build().Name.Split(new[] { '-' }, 2)[1].Trim(' ');

            // check if they've already voted for this, return if they haven't (thpugh not sure how this case could occur)
            if (!vote.Votes.ContainsKey(optionName)) return; // probably some error with another react being used
            if (!vote.Votes[optionName].Contains(reaction.UserId)) return;

            // remove their vote
            vote.Votes[optionName].Remove(reaction.UserId);

            // check if there are other reacts clicked and whether they're in the list of votes
            int numVotes = vote.Votes.Values.Where(v => v.Contains(reaction.UserId)).Count();
            var existingReacts = msg.Reactions;

            int numExistingReacts = existingReacts.Where(r => r.Value.ReactionCount == 2).Count();

            foreach (var r in existingReacts)
            {
                numVotes = vote.Votes.Values.Where(v => v.Contains(reaction.UserId)).Count();
                if (!(numVotes < vote.MaxVotes)) break;
                if (r.Value.ReactionCount == 2)
                {
                    char existingOption = r.Key.ToString().Trim('?')[0];
                    var existingFieldBuilder = builder.Fields.Where(f => f.Name[0] == existingOption).FirstOrDefault();
                    if (existingFieldBuilder == null) continue; // probably means they've reacted with something else or a number that isn't an option
                    string existingOptionName = existingFieldBuilder.Build().Name.Split(new[] { '-' }, 2)[1].Trim(' ');

                    if (vote.Votes[existingOptionName].Contains(reaction.UserId)) continue;
                    vote.Votes[existingOptionName].Add(reaction.UserId);
                }
            }

            // update vote info
            string voteInfo = "";
            if (numVotes > vote.MaxVotes)
            {
                builder.Fields[builder.Fields.Count - 1].Name = "Error: You've already used the maximum number of votes! " +
                    "Please remove other votes to set the one you just chose!";
            }
            else
            {
                string maxVotes = vote.MaxVotes + " choice" + (vote.MaxVotes > 1 ? "s" : "");
                builder.Fields[builder.Fields.Count - 1].Name = $"\u200B\nYou have used {numVotes} / {maxVotes}. Your Votes:";
            }
            foreach (var v in vote.Votes)
            {
                voteInfo += $"**{v.Key}**: {(v.Value.Contains(reaction.UserId) ? tick.ToString() : cross.ToString())}\n";
            }
            builder.Fields[builder.Fields.Count - 1].Value = voteInfo;

            // update embed
            await msg.ModifyAsync(m => m.Embed = builder.Build());
        }

        #endregion ReactHandlers

        #region Containers

        private static readonly Dictionary<int, Emoji> _emojis = new Dictionary<int, Emoji>()
        {
            {0, new Emoji("\u0030\u20e3") },
            {1, new Emoji("\u0031\u20e3") },
            {2, new Emoji("\u0032\u20e3") },
            {3, new Emoji("\u0033\u20e3") },
            {4, new Emoji("\u0034\u20e3") },
            {5, new Emoji("\u0035\u20e3") },
            {6, new Emoji("\u0036\u20e3") },
            {7, new Emoji("\u0037\u20e3") },
            {8, new Emoji("\u0038\u20e3") },
            {9, new Emoji("\u0039\u20e3") }
        };

        static private readonly Emoji tick = new Emoji("\u2713");
        static private readonly Emoji cross = new Emoji("\u2717");

        protected struct OptionInfo
        {
            public string name;
            public string description;
        }

        protected struct Vote
        {
            public int MaxVotes;
            public ulong ChairId;

            //name of option will be got from the embed based on react clicked
            public Dictionary<string, List<ulong>> Votes;
        }

        #endregion Containers
    }
}