using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WalrusBot2.Services;
using System.Data.Common;
using WalrusBot2.Data;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using System.Data.Entity;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;

namespace WalrusBot2
{
    class Program
    {
        private DiscordSocketClient _client;
        private dbContextWalrus _database = new dbContextWalrus();
        public static UserCredential GoogleCredential;

        #region Main
        static void Main(string[] args)
        {
            #region MySql Server Login
            string server;
            string port;
            string database;
            string user;
            string password = "";

            if (args.Length > 0)
            {
                Dictionary<string, string> argDict = args.Select(a => a.Split('=')).ToDictionary(a => a[0], a => a.Length == 2 ? a[1] : null);
                server = argDict["server"];
                port = argDict["port"];
                database = argDict["database"];
                user = argDict["user"];
                password = argDict["password"];
            }
            else
            {
                Console.WriteLine("/----- MySQL Database Login -----\\");
                Console.Write("| Server: "); server = Console.ReadLine();
                Console.Write("| Port: "); port = Console.ReadLine();
                Console.Write("| Database: "); database = Console.ReadLine();
                Console.Write("| User ID: "); user = Console.ReadLine();
                Console.Write("| Password:");
                while(true)
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
            
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
            builder.Add("server", server);
            builder.Add("port", port);
            builder.Add("database", database);
            builder.Add("user", user);
            builder.Add("password", password);
            builder.Add("persistsecurityinfo", "True");
            dbContextWalrus.SetConnectionString(builder.ToString());
            #endregion

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            #region Google OAuth2 Login
            string[] _scopes = {
                GmailService.Scope.GmailSend
            };

            using (var fs = new FileStream(_database["config", "googleCredPath"], FileMode.Open, FileAccess.Read))
            {
                GoogleCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(fs).Secrets, _scopes, "user", CancellationToken.None, new FileDataStore(_database["config", "googleTokenPath"], true)).Result;
            }
            #endregion

            #region Discord
            _client = new DiscordSocketClient();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _database["config", "botToken"]);
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
