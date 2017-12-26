using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;

        //Contains strings to be replaced
        private List<string> replaceTheseStrings = new List<string>() { "GODDESS", "GOD"};
        private List<string> ignoreTheseStrings = new List<string>() { "GOD IS DEAD", "GOD KNOWS" };
        private const string replacementString = "Haruhi";


        //Struct used to store usage info
        private Dictionary<ulong, List<DateTime>> spamList = new Dictionary<ulong, List<DateTime>>();
        private Dictionary<ulong, UserInteraction> usageList = new Dictionary<ulong, UserInteraction>();
        private struct UserInteraction
        {
            public string user;
            public int counter;
            public DateTime dateTime;
        }

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();


        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            string token = File.ReadAllText("Token.txt"); // Remember to keep this private!
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            string content = message.Content;
            bool userInteractionBool = false;
            foreach (var godString in replaceTheseStrings)
            {
                if (content.ToUpper().Contains(godString))
                {
                    content = GetGodStringReplacement(content, godString);
                    userInteractionBool = true;
                }
            }

            //Only reply to godstrings and only if the message has changed.
            if (userInteractionBool)
            {
                int spam = AddUserInterAction(message.Author.Id, message.Author.Username);

                if (spam == -1)
                    return;

                string answer = "*The Heretic* ***" + message.Author.Username
                                + "*** *wanted to write:* **" + content + "**";

                if (spam == 3)
                    answer += "\n This is your last correction for today";

                if (message.Content != content)
                    await message.Channel.SendMessageAsync(answer);
            }
        }

        //Returns the number of spam posts a user made
        private int AddUserInterAction(ulong id, string userName)
        {
            //Check usageList
            //Check for entry existance
            if (!usageList.ContainsKey(id))
            {
                UserInteraction userInteraction = new UserInteraction() { user = userName };
                usageList.Add(id, userInteraction);
            }

            var interaction = usageList[id];
            interaction.counter++;
            interaction.dateTime = DateTime.Now;

            //Check SpamList
            //Check for entry existance
            if (!spamList.ContainsKey(id))
            {
                spamList.Add(id, new List<DateTime>());
                spamList[id].Add(DateTime.Now);
                return 1;
            }

            //Delete old Dates from spamlist
            for (int i = 0; i < spamList[id].Count; i++)
                if (spamList[id][i].AddHours(24) < DateTime.Now)
                    spamList[id].RemoveAt(i);

            //Check if user is spamming
            if (spamList[id].Count >= 3)
                //User is spamming
                return -1;

            //User is in good standing
            spamList[id].Add(DateTime.Now);
            return spamList[id].Count;
        }

        //Replace all occurences of a Godstring that aren't a antigodstring in a message with Haruhi
        private string GetGodStringReplacement(string content, string godString)
        {
            //Create copy
            string text = content;
            //Index from which to check for the next godstring
            int startIndex = text.ToUpper().IndexOf(godString, 0, StringComparison.Ordinal);
            //Check if a godstring still exists
            while (startIndex != -1)
            {
                //Check if its an anti god string
                bool antiGodBool = false;
                foreach (var antiGodString in ignoreTheseStrings)
                {
                    if (!(content.Length < startIndex + antiGodString.Length) 
                        && content.ToUpper().Substring(startIndex, antiGodString.Length) == antiGodString)
                    {
                        antiGodBool = true;
                        startIndex = text.ToUpper().IndexOf(godString, startIndex + antiGodString.Length, StringComparison.Ordinal);
                        break;
                    }
                }

                //Skip if it's an antigodstring
                if (antiGodBool)
                    continue;

                // Replace the current occurence of the godstring
                text = text.Remove(startIndex, godString.Length);
                text = text.Insert(startIndex, replacementString);

                //Check for next godstring
                startIndex = text.ToUpper().IndexOf(godString, startIndex + replacementString.Length, StringComparison.Ordinal);
            }

            //Return completely transformed string
            return text;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
