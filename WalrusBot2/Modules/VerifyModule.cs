using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord.Commands;
using Discord.Addons.Interactive;

using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;

using MimeKit;

using WalrusBot2.Data;
using System.Net.Mail;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace WalrusBot2.Modules
{
    [Group("verify")]
    [Name("User Verification")]
    public class VerifyModule : XModule
    {
        private static GmailService _gmailService = null;
        private static Random _random = new Random();
        private static TimerService _timerService = null;

        public VerifyModule(TimerService timerService, DiscordSocketClient client)
        {
            if (_timerService == null)
            {
                _timerService = timerService;
                _timerService.AddTimer(new TimerService.TimerInfo(
                    new Timer(
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                        async _ =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                        {
                            if (UInt64.TryParse(database["config", Program.Debug ? "guildDebugId" : "guildId"], out ulong guildId))
                            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                Task.Run(async () =>
                                {
                                    var guild = client.GetGuild(guildId);
                                    await UpdateAsync(guild);
                                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            }
                        },
                        null,
                        DateTime.Now
                            .AddMinutes(60 - DateTime.Now.Minute)
                            .AddHours((24 - DateTime.Now.Hour) + 4)
                            - DateTime.Now,
                        TimeSpan.FromDays(1)
                    ),
                    () =>
                    {
                        var TriggerHour = 4; // 4am

                        TimeSpan startDelay = DateTime.Now
                        .AddMinutes(60 - DateTime.Now.Minute)
                        .AddHours((24 - DateTime.Now.Hour) + TriggerHour)
                        - DateTime.Now;

                        return startDelay;
                    },
                    () =>
                    {
                        return TimeSpan.FromDays(1);
                    }
                ));
            }

            _gmailService = _gmailService ?? new GmailService(new BaseClientService.Initializer()

            {
                HttpClientInitializer = Program.GoogleCredential,
                ApplicationName = database["config", "googleAppName"]
            });
        }

        #region Send Email

        [Command("email", RunMode = RunMode.Async)]
        [Summary("Send a verification email to you with your unique identification code.")]
        [Name("email (DM only)")]
        public async Task EmailAsync([Remainder]string email)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync(Context.User.Mention + " " + database["string", "errReqDm"]);
                await Context.Message.DeleteAsync();
                return;
            }
            if (!Context.User.MutualGuilds.Any(x => x.Id.ToString() == database["config", Program.Debug ? "guildDebugId" : "guildId"]))
            {
                await ReplyAsync("You aren't a member of our Discord server! If you're a student as the University of Southampton or are a SUSU member," +
                    $"you can join our Discord here: {database["config", "guildInvite"]}");
            }
            if (!IsValidEmail(email))
            {
                await ReplyAsync(database["string", "errEmailInvalid"]);
                return;
            }
            WalrusUserInfo userInfo = await database.WalrusUserInfoes.FindAsync(Context.User.Id.ToString());

            if (database.WalrusUserInfoes.Any(x => x.Email == email))  // see if someone's already verified/attempted to verify with that email
            {
                if (email == (userInfo.Email ?? ""))
                {
                    if (userInfo.Verified)
                    {
                        await ReplyAsync("You're already verified with that email! If you're missing roles then wait for a role updated (run daily)" +
                            "or contact a member of the committee if you've still not got your role.");
                    }
                    else
                    {
                        await ReplyAndDeleteAsync("You've aleady attempted to verify with that email. Do you want me to send you another email?" +
                            " Type \"yes\" to confirm or \"no\" to cancel.", timeout: TimeSpan.FromSeconds(31));
                        var response = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(30));
                        if (response == null) return;
                        if (response.Content.ToLower() != "yes")
                        {
                            await ReplyAsync("I won't send you another email :)");
                            return;
                        }
                        else
                        {
                            await SendEmailAsync(email, userInfo.Code);
                            await ReplyAsync("Okay, I've sent you a new email :)");
                        }
                    }
                }
                else // someone has already verified with this email
                {
                    await ReplyAsync("Someone has already verified with this email! If you believe this to be a mistake then please contact " +
                        "a member of the committee ASAP!!!");
                }
            }
            else
            {
                if (userInfo == null)
                {
                    var newUser = new WalrusUserInfo
                    {
                        UserId = Context.User.Id.ToString(),
                        Verified = false,
                        Username = Context.User.Username + "#" + Context.User.Discriminator,
                        Email = email.ToLower(),
                        Code = RandomString(8)
                    };
                    if (await SendEmailAsync(email, newUser.Code))
                    {
                        database.WalrusUserInfoes.Add(newUser);
                        await database.SaveChangesAsync();
                        await ReplyAsync("Email sent! Once you've got your code, DM me with `svge!verify code <your code>` :)");
                    }
                }
                else
                {
                    if (userInfo.Verified)
                    {
                        await ReplyAndDeleteAsync($"You're already verified with the email {userInfo.Email}. Are you sure you want to unverify and use" +
                            "this email instead? Type \"yes\" to confirm or \"no\" to cancel.", timeout: TimeSpan.FromSeconds(31));
                        var response = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(30));
                        if (response == null) return;
                        if (response.Content.ToLower() != "yes")
                        {
                            await ReplyAsync("Okay, I won't reset your verification :)");
                            return;
                        }
                        else
                        {
                            userInfo.Email = email.ToLower();
                            userInfo.Verified = false;
                            userInfo.Code = RandomString(8);
                            if (await SendEmailAsync(email, userInfo.Code)) await database.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        // they exist in the database but they've not not verified yet or added their email
                        userInfo.Email = email.ToLower();
                        userInfo.Code = RandomString(8); ;
                        if (await SendEmailAsync(email, userInfo.Code))
                        {
                            await database.SaveChangesAsync();
                            await ReplyAsync("Email sent! Once you've got your code, DM me with `svge!verify code <your code>` :)");
                        }
                    }
                }
            }
        }

        #endregion Send Email

        #region Spam

        [RequireUserRole(new string[] { "committee", "tester" })]
        [Command("spam")]
        [Summary("Send a message to all non-verified persons in the server asking them to do so.")]
        [RequireContext(ContextType.Guild)]
        public async Task MessageNonVerifiedAsync()
        {
            foreach (SocketUser user in Context.Guild.Users)
            {
                if (user.IsBot) continue;
                if (user == Context.User) continue;
                if (user == Context.Guild.Owner) continue;

                Console.WriteLine($"Spamming {user.Username}...");

                string s = user.Id.ToString();
                try
                {
                    WalrusUserInfo userInfo = database.WalrusUserInfoes.Where(x => x.UserId == s).FirstOrDefault();
                    if (userInfo == null ? true : !userInfo.Verified)
                    {
                        IDMChannel c = await user.GetOrCreateDMChannelAsync();
                        await c.SendMessageAsync("Hi there! I noticed that you haven't yet verified your email address with us on our server. " +
                            "You can do that by sending me `svge!verify email <your email>`, then send me the code you recieve with `svge!verify code <code>`. " +
                            "You should do it soon so you don't get kicked from the server and can get access to more server channels! ");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        #endregion Spam

        #region Code

        [Command("code")]
        [Name("code (DM only)")]
        [Summary("Enter the code sent to your email to verify your email address!s")]
        public async Task CodeAsync(string code)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync(Context.User.Mention + " " + database["string", "errReqDm"]);
                await Context.Message.DeleteAsync();
                return;
            }

            string userId = Context.User.Id.ToString();
            WalrusUserInfo userInfo = database.WalrusUserInfoes.Where(x => x.UserId == userId).FirstOrDefault();
            if (userInfo == null)
            {
                await ReplyAsync("It appears you haven't yet registered your email with us! " +
                    "Send me `svge!verify email <your_email> to start the verification process :)");
            }
            else
            {
                SocketGuild guild = Context.User.MutualGuilds.Where(x => x.Id.ToString() == database["config", Program.Debug ? "guildDebugId" : "guildId"]).First();
                if (userInfo.Code == code)
                {
                    userInfo.Verified = true;
                    await database.SaveChangesAsync();
                    await VerifyModule.UpdateAsync(guild.Users.Where(x => x.Id == Context.User.Id).Single(), guild, database);
                    await ReplyAsync("You are now verified and your roles have been updated! Welcome to SVGE :D");
                }
                else
                {
                    await ReplyAsync("That code appears to be invalid! If you've changed your email recently then you will have been sent " +
                        "a new code to use, so please double check your email :)");
                }
            }
        }

        #endregion Code

        #region Update

        [RequireUserRole(new string[] { "commitee", "tester" })]
        [Command("update", RunMode = RunMode.Async)]
        [Name("Update")]
        [Summary("Update role and membership information for all members")]
        [RequireContext(ContextType.Guild)]
        public async Task UpdateAsync()
        {
            foreach (IGuildUser user in Context.Guild.Users) await UpdateAsync(user as SocketGuildUser, Context.Guild, database);
            await ReplyAsync("Guild user data updated...");
        }

        [RequireUserRole(new string[] { "committee", "tester" })]
        [Command("update", RunMode = RunMode.Async)]
        [Name("Update")]
        [Summary("Update role and membership information for a given member")]
        [RequireContext(ContextType.Guild)]
        public async Task UpdateAsync(ulong userId)
        {
            SocketGuildUser user = Context.Guild.GetUser(userId);
            await ReplyAsync($"About to update information for {user.Nickname ?? user.Username}...");
            await UpdateAsync(user, Context.Guild, database);
            await ReplyAsync($"User information and roles updated for user with ID: {userId}.");
        }

        // only called by daily function
        private static async Task UpdateAsync(IGuild guild)
        {
            foreach (SocketGuildUser user in (await guild.GetUsersAsync())) await UpdateAsync(user, guild as SocketGuild, new dbWalrusContext());
        }

        // called by daily function (via above static function) or by the code command
        private static async Task UpdateAsync(SocketGuildUser user, SocketGuild guild, dbWalrusContext database)  // not callable as a Discord command
        {
            Console.WriteLine("Updating...");
            Console.WriteLine($"Updating user information for {user.Nickname ?? user.Username}...");

            if (user.IsBot) return;
            string userId = user.Id.ToString();
            WalrusUserInfo userInfo = database.WalrusUserInfoes.Where(x => x.UserId == userId).FirstOrDefault();
            if (userInfo == null)
            {
                Console.WriteLine($"User {user.Nickname ?? user.Username} does not exist in the database!");
                List<IRole> roles = (user as IGuildUser).RoleIds.ToList().ConvertAll(x => guild.GetRole(x) as IRole);
                await RemoveFilteredRolesAsync(user as IGuildUser, roles);
                return;
            }

            Console.WriteLine("Getting custom roles");
            List<ulong> customRoleIds = GetAdditionalRoleIds(userInfo, guild as SocketGuild, database);

            if (!userInfo.Verified)
            {
                Console.WriteLine($"User {user.Nickname ?? user.Username} is not verified!");
                // remove everything that isn't a custom role
                List<IRole> roles = (user as IGuildUser).RoleIds.Except(customRoleIds).ToList().ConvertAll(x => guild.GetRole(x) as IRole);
                await RemoveFilteredRolesAsync(user, roles);
                return;
            }

            IRole studentRole = ParseRole(database, "student", guild as SocketGuild);
            if (studentRole != null)
            {
                if (userInfo.Email.Split('@')[1] == database["config", "studentEmailDomain"])
                {
                    await user.AddRoleAsync(studentRole);
                }
                else
                {
                    await user.RemoveRoleAsync(studentRole);
                }
            }

            //get additional roles only returns IDs for roles that exist in the guild so no need to check again
            await user.AddRolesAsync(customRoleIds.ConvertAll(id => guild.GetRole(id)));

            WalrusMembershipList membership = database.WalrusMembershipLists.Where(x => x.Email == userInfo.Email).FirstOrDefault();
            IRole communityMembershipRole = ParseRole(database, "communityMember", guild as SocketGuild);
            IRole dlcMembershipRole = ParseRole(database, "dlcMember", guild as SocketGuild);
            IRole gotyMembershipRole = ParseRole(database, "gotyMember", guild as SocketGuild);

            if (membership != null)
            {
                /* if they're in the membership list then they at least have the community membership level */

                if (communityMembershipRole != null) await user.AddRoleAsync(communityMembershipRole);

                if (membership.Membership.Replace("\"", "").Split(',').Contains("DLC Bundle Membership") && dlcMembershipRole != null)
                    await user.AddRoleAsync(dlcMembershipRole);

                if (membership.Membership.Replace("\"", "").Split(',').Contains("Game of the Year Membership") && gotyMembershipRole != null)
                    await user.AddRoleAsync(gotyMembershipRole);
            }
            else
            {
                if (communityMembershipRole != null) await user.RemoveRoleAsync(communityMembershipRole);
                if (dlcMembershipRole != null) await user.RemoveRoleAsync(dlcMembershipRole);
                if (gotyMembershipRole != null) await user.RemoveRoleAsync(gotyMembershipRole);
            }
        }

        #endregion Update

        #region Reset

        [Command("reset", RunMode = RunMode.Async)]
        [Name("Reset")]
        [Summary("Resets the verifications and roles for all members except for their custom roles (e.g. committee or alumni roles).")]
        [RequireUserRole(new string[] { "committee", "tester" })]
        [RequireContext(ContextType.Guild)]
        public async Task ResetUsersAsync()
        {
            int num = 1;
            foreach (IGuildUser user in Context.Guild.Users)
            {
                try
                {
                    Console.WriteLine($"[{num++}] Resetting roles for {user.Username}...");
                    if (user.IsBot) continue;
                    if (user.Id == Context.Guild.OwnerId) continue;
                    if (user == Context.User) continue;

                    string userId = user.Id.ToString();
                    WalrusUserInfo userInfo = database.WalrusUserInfoes.Where(x => x.UserId == userId).FirstOrDefault();

                    if (userInfo == null)  // not verified, email not even in db
                    {
                        List<IRole> roles = user.RoleIds.ToList().ConvertAll(x => Context.Guild.GetRole(x)).Except(
                            new List<IRole> { Context.Guild.EveryoneRole }).ToList();
                        await RemoveFilteredRolesAsync(user, roles);
                        continue;
                    }

                    if (userInfo.AdditionalRolesJSON == null)  // no custom roles
                    {
                        List<IRole> roles = user.RoleIds.ToList().ConvertAll(x => Context.Guild.GetRole(x)).Except(
                            new List<IRole> { Context.Guild.EveryoneRole }).ToList();
                        await RemoveFilteredRolesAsync(user, roles);

                        database.WalrusUserInfoes.Remove(userInfo);
                    }
                    else
                    {
                        // get custom role IDs
                        List<ulong> customRoleIds = GetAdditionalRoleIds(userInfo, Context.Guild, database);

                        // cast to IRole list, excluding custom roles, and remove them from user
                        List<IRole> roles = user.RoleIds.Except(customRoleIds).ToList().ConvertAll(x => Context.Guild.GetRole(x)).Except(
                            new List<IRole> { Context.Guild.EveryoneRole }).ToList();
                        await RemoveFilteredRolesAsync(user, roles);

                        // reset their database entry
                        userInfo.Verified = false;
                        userInfo.Email = null;
                        userInfo.Code = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            await database.SaveChangesAsync();
            await ReplyAsync("User information for this server has been reset...");
        }

        #endregion Reset

        #region Static Members

        public static async Task SpamOnJoinAsync(SocketUser user)
        {
            if (user.IsBot) return;

            IDMChannel c = await user.GetOrCreateDMChannelAsync();
            await c.SendMessageAsync("Hi there! Welcome to the Southampton Video Games and Esports Society Discord server! My name is Maisie, " +
                "and I'm our Discord bot.\n\nTo get access to more channels in the Discord, please verify your email with my by sending me " +
                "`svge!verify email <your_email>` here in these priavte DMs. You should use your Southampton University email if you're a student, " +
                "or the email you joined SUSU with if you're an alumnus or have some other kind of SUSU membership. Once you've sent me back the code " +
                "you receive in your email I can get you verified and give you access to the rest of our server! Happy gaming :)");
        }

        private static IRole ParseRole(dbWalrusContext database, string roleName, SocketGuild guild)
        {
            IRole role = null;
            if (UInt64.TryParse(database["role", roleName], out ulong roleId))
                role = guild.GetRole(roleId);
            return role;
        }

        #endregion Static Members

        #region Utility Functions

        private static async Task RemoveFilteredRolesAsync(IGuildUser user, List<IRole> roles)
        {
            IList<IRole> removeRoles = new List<IRole>();
            IList<ulong> forbiddenRoleIds = new List<ulong> { 585551455998443525, 519226054439731200 };
            foreach (IRole role in roles)
            {
                if (!role.Name.Contains("CPS") && !forbiddenRoleIds.Contains(role.Id)) removeRoles.Add(role);
            }
            await user.RemoveRolesAsync(roles);
        }

        /// <summary>
        /// Sends an email to the given email address, substituting the given code into the email template.
        /// </summary>
        /// <param name="emailAddr"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private async Task<bool> SendEmailAsync(string emailAddr, string code)
        {
            if (!IsValidEmail(emailAddr)) return false;  // running it here again just to be safe, probably unncessary...
            MimeMessage message = new MimeMessage();
            message.To.Add(new MailboxAddress(Context.User.Username.ToString(), emailAddr));
            message.From.Add(new MailboxAddress(database["config", "gmailFromName"], database["config", "gmailFromAddr"]));
            message.Subject = "SVGE Discord Verification Email!";
            // HTML body of email
            var body = new BodyBuilder();
            string htmlString = await File.OpenText(database["config", "emailTemplatePath"]).ReadToEndAsync();
            body.HtmlBody = htmlString.Replace("xXxCODEHERExXx", code);
            message.Body = body.ToMessageBody();

            var gMessage = new Message() { Raw = MimeToGmail(message.ToString()) };
            try
            {
                await _gmailService.Users.Messages.Send(gMessage, "me").ExecuteAsync();
                //await ReplyAsync("Verification email sent! Once you've got your code, send it to me with *svge!verify code* ***[your-code-here]***.");
                return true;
            }
            catch (Exception e)
            {
                //await ReplyAsync("There was an issue with sending your email! Try again in a few minutes, and if the problem persists then please contact a committee member.");
                Console.WriteLine($"Exception when sending an email: {e.ToString()}");
                return false;
            }
        }

        private static List<ulong> GetAdditionalRoleIds(WalrusUserInfo userInfo, SocketGuild guild, dbWalrusContext database)
        {
            List<ulong> customRoles = new List<ulong>();
            if (userInfo.AdditionalRolesJSON != null)
            {
                List<string> customRoleStrings = userInfo.AdditionalRolesJSON.Split(',').ToList().ConvertAll(x => x.Trim(' '));

                foreach (string s in customRoleStrings)
                {
                    string idString = database["role", s];
                    if (idString != null)
                    {
                        if (UInt64.TryParse(idString, out ulong id))
                        {
                            if (guild.Roles.Any(r => r.Id == id)) customRoles.Add(id);
                        }
                    }
                }
            }
            return customRoles;
        }

        /// <summary>
        /// Returns a string of random characters with length "length".
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private string MimeToGmail(string msg)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);

            return System.Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        /// <summary>
        /// Confirms whether the supplied string is a valid email address.
        /// </summary>
        /// <param name="emailAddr"></param>
        /// <returns></returns>
        private bool IsValidEmail(string emailAddr)
        {
            try
            {
                MailAddress m = new MailAddress(emailAddr);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        #endregion Utility Functions
    }
}