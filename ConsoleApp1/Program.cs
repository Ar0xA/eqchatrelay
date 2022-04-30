using System;
using System.IO;
using Discord;
using Discord.WebSocket;
using System.Configuration;

namespace ParseLog
{
    class Program
    {

        static int TotalLines(string filePath)
        {
            using (StreamReader sr = new(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                int i = 0;
                while (sr.ReadLine() != null) { i++; }
                return i;
            }
        }

        static string[] readNewLines(string filePath, int previousLines, int linesToRead)
        {
            List<string> newLines = new List<string>();
            using (StreamReader sr = new(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                int lineCounter = 0;
                string line = "";
                //read what we dont need
                while (lineCounter < previousLines)
                {
                    line = sr.ReadLine();
                    lineCounter++;
                }

                //now read the new lines
                do
                {
                    line = sr.ReadLine();
                    if (line != null)
                    {
                        newLines.Add(line); //then use .add to list
                    }
                } while (line != null);
                Console.WriteLine($"New lines read: {newLines.Count}");
            }
            return newLines.ToArray();
        }

        static string[] parseLinestorelevant(string[] newLines, string wantedchatgroup)
        {
            List<string> wantedLines = new List<string>();
            foreach (string line in newLines)
            {
                //logformat is \[time\] [user] tells the [wantedchatchannel], '[message]'[eol]
                if (line.Contains($"tells the {wantedchatgroup}, '"))
                {
                    Console.WriteLine($"Found matched line: {line}");
                    wantedLines.Add(line);
                }

            }
            return wantedLines.ToArray();
        }
        private async Task MainLogParse()
        {
            int DelayTimer = 1000;
            string filetowatch = ConfigurationManager.AppSettings["filetowatch"];
            string chatgrouptowatch = ConfigurationManager.AppSettings["chatgrouptowatch"]; //guild, fellowship, group, etc

            if (File.Exists(filetowatch))
            {
                Console.WriteLine($"File {filetowatch} exists! Monitoring for new lines in {chatgrouptowatch}.");
            }
            else
            {
                Console.WriteLine("Error, file does not exist!");
                System.Environment.Exit(1);
            }

            //now that the file exists, lets see how many lines there are
            int previousLines = TotalLines(filetowatch);
            Console.WriteLine($"Logfile contains: {previousLines} lines");

            //wait to see if logfile changes
            int newLines = 0;
            int newCount = 0;

            while (true)
            {
                newLines = TotalLines(filetowatch);
                Task.Delay(DelayTimer);
                if (newLines > previousLines)
                {
                    Console.WriteLine("New lines in logfile");
                    newCount = newLines - previousLines;
                    //read the new lines and return them
                    string[] theNewLines = readNewLines(filetowatch, previousLines, newCount);

                    //ok we got the new lines now we parse them in format
                    // "[user] tells the [chatgroup], '[message]'[eol]
                    //where we only want the messages that are in the chatgroup we want
                    string[] getRelevantLines = parseLinestorelevant(theNewLines, chatgrouptowatch);
                    foreach (string line in getRelevantLines)
                    {
                       await WriteMessage(line);
                    }
                    previousLines = newLines;
                } else
                {
                    Task.Delay(DelayTimer);
                }
            }
        }

        //bot logging
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task<Task> OnClientReady()
        {
            Console.WriteLine(_client.CurrentUser);
            Console.WriteLine(_client.Guilds);
            foreach (SocketGuild guild in _client.Guilds)
            {
                await guild.DefaultChannel.SendMessageAsync("bot started and connected");
            }
            return Task.CompletedTask;
        }

        private async Task WriteMessage(string message)
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                await guild.DefaultChannel.SendMessageAsync(message);
            }
        }


        private DiscordSocketClient? _client;
        public async Task MainAsync()
        {
            //lets get the config stuff going

            var token = ConfigurationManager.AppSettings["token"];

            _client = new DiscordSocketClient();

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Ready += OnClientReady;
            // Block this task until the program is closed.
            MainLogParse();
            await Task.Delay(-1);
        }

        //async for bot stuff
        public static Task Main(string[] args) => new Program().MainAsync();

    }
}
