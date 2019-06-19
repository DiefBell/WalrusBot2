using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Microsoft.Extensions.DependencyInjection;

using WalrusBot2.Data;
using WalrusBot2.Services;

namespace WalrusBot2
{
    class Program
    {
        private DiscordSocketClient _client;
        private dbContextWalrus _database = new dbContextWalrus();
        private DriveService _driveService;
        public static UserCredential GoogleCredential;
        public static bool Debug = false;
        #region Main
        static void Main(string[] args)
        {
            #region MySql Server Login
            string server;
            string port;
            string database;
            string user;
            string password = "";

            Dictionary<string, string> parameters = args.Select(a => a.Split('=')).ToDictionary(a => a[0], a => a.Length == 2 ? a[1] : null);
            if (parameters.Keys.Contains("debug")) Debug = parameters["debug"] == "true" ? true : false;
            if (parameters.Keys.Contains("server"))  // assuming you'd type them all in
            {
                server = parameters["server"];
                port = parameters["port"];
                database = parameters["database"];
                user = parameters["user"];
                password = parameters["password"];
            }
            else
            {
                Console.WriteLine("/----- MySQL Database Login -----\\");
                Console.Write("| Server: "); server = Console.ReadLine();
                Console.Write("| Port: "); port = Console.ReadLine();
                Console.Write("| Database: "); database = Console.ReadLine();
                Console.Write("| User ID: "); user = Console.ReadLine();
                Console.Write("| Password:");
                while (true)
                {
                    ConsoleKeyInfo i = Console.ReadKey(true);
                    if (i.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                    else if (i.Key == ConsoleKey.Backspace)
                    {
                        if (password.Length > 0)
                        {
                            password.Remove(password.Length - 1);
                            Console.Write("\b \b");
                        }
                    }
                    else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                    {
                        password += i.KeyChar;
                        Console.Write("*");
                    }
                }
                Console.WriteLine("\n\\--------------------------------/\n\n");
            }

            try
            {
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                builder.Add("server", server);
                builder.Add("port", port);
                builder.Add("database", database);
                builder.Add("user", user);
                builder.Add("password", password);
                builder.Add("persistsecurityinfo", "True");
                dbContextWalrus.SetConnectionString(builder.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed with exception:\n{e.Message}");
                Console.WriteLine("This was most likely a failure to log into the database, so check your connection!");
                Console.Read();
                return;
            }
            #endregion

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            #region Google
            #region OAuth2 Login
            string[] _scopes = {
                GmailService.Scope.GmailSend,
                DriveService.Scope.DriveReadonly,
                DriveService.Scope.DriveMetadataReadonly
            };

            using (var fs = new FileStream(_database["config", "googleCredPath"], FileMode.Open, FileAccess.Read))
            {
                GoogleCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(fs).Secrets, _scopes, "user", CancellationToken.None, new FileDataStore(_database["config", "googleTokenPath"], true)).Result;
            }
            #endregion
            #region Download Email Template
            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential,
                ApplicationName = _database["config", "googleAppName"]
            });
            var expReq = _driveService.Files.Export(_database["config", "emailTemplateId"], "text/plain");
            var stream = new MemoryStream();
            expReq.MediaDownloader.ProgressChanged += (IDownloadProgress progress)
                =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Verification email file has been downloaded!\n");
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine(">> The download of the verification email file has failed! D: Try adding it manually! <<\n");
                            break;
                        }
                }
            };
            await expReq.DownloadAsync(stream);

            using (FileStream fs = new FileStream(_database["config", "emailTemplatePath"], FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.WriteTo(fs);
            }
            #endregion
            #endregion

            #region Discord
            _client = new DiscordSocketClient();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _database["config", Program.Debug ? "botDebugToken" : "botToken"]);
            await _client.StartAsync();
            #endregion

            await Task.Delay(-1);
        }
        #endregion

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton(new InteractiveService(_client))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Add additional services here...
                .BuildServiceProvider();
        }
    }
}
