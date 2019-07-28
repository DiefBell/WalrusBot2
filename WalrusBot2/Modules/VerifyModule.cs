using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.Addons.Interactive;

using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;

using MimeKit;

using WalrusBot2.Data;
using System.Net.Mail;

namespace WalrusBot2.Modules
{
    [Group("verify")]
    [Name("User Verification")]
    [DontAutoLoad]
    public class VerifyModule : XModule
    {
        private static GmailService _gmailService = null;
        private static Random _random = new Random();

        public VerifyModule()
        {
            if (_gmailService == null)
            {
                _gmailService = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Program.GoogleCredential,
                    ApplicationName = database["config", "googleAppName"]
                });
            }

            //temporarily removed until the update function has been properly tested.]

            /*if (_dailyUpdate != null)
            {
                _dailyUpdate = new DailyUpdate(4); // 4am
                _dailyUpdate.OnTimeTriggered += () =>
                new Task(async () =>
                {
                    await UpdateAsync();
                });
            }*/
        }

        /// <sudo>
        ///     Check the message has been sent in a DM. Delete the message if not.
        ///     Check the sender is in the guild.
        ///     Confirm that it's a valid email addresss.
        ///     Check if that email already exists in the database and if it belongs to someones else. Ask the user to contact a committee member.
        ///     Check if this user is verified. If they are verified with that email, tell them then return.
        ///     If they are verified but want are trying to use a different email address
        ///     {
        ///         get them to confirm they want to change
        ///         remove verified status
        ///     }
        ///     Generate a code for this user, double check that it doesn't exist in the database already (pretty unlikely), save info to database.
        ///     Send email to user with their code.
        /// </sudo>
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
            if (!IsValidEmail(email))
            {
                await ReplyAsync(database["string", "errEmailInvalid"]);
                return;
            }
            WalrusUserInfo userInfo = await database.WalrusUserInfoes.FindAsync(Context.User.Id.ToString());
            if (userInfo == null)
            {
                userInfo = new WalrusUserInfo
                {
                    UserId = Context.User.Id.ToString(),
                    Verified = false,
                    Username = Context.User.Username,
                    Email = email,
                    Code = RandomString(8)
                };
                if (await SendEmailAsync(email, userInfo.Code))
                {
                    database.WalrusUserInfoes.Add(userInfo);
                    await database.SaveChangesAsync();
                }
            }
            else
            {
                if (userInfo.Email == email)
                {
                    if (userInfo.Verified)
                        await ReplyAsync("You're already verified with that email! If you're missing student or membership roles then wait for an update, or ask a committee member to update it for you!");
                    else
                        await SendEmailAsync(email, userInfo.Code);
                }
                else
                {
                    userInfo.Email = email;
                    if (userInfo.Verified)
                    {
                        userInfo.Verified = false;
                        await ReplyAsync("Please note that you've already verified with a different email and you may lose access to your roles until you've verified this one!");
                        await ReplyAndDeleteAsync("Please type \"confirm\" if this is correct and you wish to change your email (30 second timeout)");
                        var response = await NextMessageAsync(new EnsureFromUserCriterion(Context.User.Id), timeout: TimeSpan.FromSeconds(31));
                        if (!(response.Content.ToLower() == "confirm"))
                        {
                            await ReplyAsync("You didn't confirm your email change within the time limit. If you still wish to change your email then please rerun the command.");
                            return;
                        }
                    }
                    userInfo.Code = RandomString(8);
                    if (await SendEmailAsync(email, userInfo.Code))
                    {
                        await database.SaveChangesAsync();
                    }
                }
            }
        }

        /// <sudo>
        /// Go through all members in the guild, check they're not a bot, whether their userId is in the database and if they're verified.
        /// If they aren't, send them a DM.
        /// </sudo>
        [Command("spam")]
        [Summary("Send a message to all non-verified persons in the server asking them to do so.")]
        [RequireUserRole(new string[] { "commitee", "tester" })]
        public async Task MessageNonVerifiedAsync()
        {
            await ReplyAsync("Command not yet written...");
        }

        /// <sudo>
        /// Check that the user is in the database
        /// Confirm the code matches their code
        /// Set their status to verified
        /// Run an update for that user.
        /// </sudo>
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

            await ReplyAsync($"Your code is **`{code}`**. Dief hasn't written the rest of this yet ;)");
        }

        [Command("update")]
        [Name("Update")]
        [Summary("Update role and membership information for all members")]
        /// <sudo>
        /// Foreach member on server:
        ///     Are they a bot? continue;
        ///     Do they already exist in the database?
        ///         Apply any custom roles e.g. alumni
        ///         Is their email verified?
        ///             If it ends with @soton.ac.uk etc then give the student role
        ///             Cross-reference with SUSU membership list, give roles
        ///         else: Remove student role and membership roles
        /// </sudo>
        [RequireUserRole(new string[] { "committee", "tester" })]
        public async Task UpdateAsync()
        {
        }

        [RequireUserRole(new string[] { "committee", "tester" })]
        public async Task UpdateAsync(ulong userId)
        {
            //double check they're on the server
        }

        [Command("reset")]
        [Name("Reset")]
        [Summary("Resets the verifications and roles for all members except for their custom roles (e.g. committee or alumni roles).")]
        [RequireUserRole(new string[] { "committee", "tester" })]
        /// <sudo>
        /// Foreach member on server:
        ///     Are they a bot? continue;
        ///     Check if they are in the database and have any custom roles
        ///     Remove every role that isn't a custom role
        /// </sudo>
        public async Task ResetUsersAsync()
        {
        }

        #region Static Members

        /// <sudo>
        /// Ensure they aren't a bot.
        /// Check we can send a message to this user.
        /// Send message to user.
        /// </sudo>
        public static async Task SpamOnJoinAsync()
        {
        }

        private static DailyUpdate _dailyUpdate = null;

        #endregion Static Members

        #region Utility Functions

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
                await ReplyAsync("Verification email sent! Once you've got your code, send it to me with *svge!verify code* ***[your-code-here]***.");
                return true;
            }
            catch (Exception e)
            {
                await ReplyAsync("There was an issue with sending your email! Try again in a few minutes, and if the problem persists then please contact a committee member.");
                Console.WriteLine($"Exception when sending an email: {e.ToString()}");
                return false;
            }
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