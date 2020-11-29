using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using static DiscordBot.Correction;

namespace DiscordBot
{
    public class Program
    {
        private DiscordSocketClient _client;
        private object settableRolesLock = new object();
        public static Random rand = new Random();

        //Hardcoded strings
        private string helpMessage = "Documentation can be found at: https://master117.github.io/EasyDiscordBotPage/";

        //Correction Class
        private static Correction correction = new Correction();

        //Struct used to store usage info
        private Dictionary<ulong, List<DateTime>> spamList = new Dictionary<ulong, List<DateTime>>();
        private Dictionary<ulong, UserInteraction> usageList = new Dictionary<ulong, UserInteraction>();
        private List<SocketRole> settableRolesList = new List<SocketRole>();
        public struct RolesGuildStruct
        {
            public ulong role;
            public ulong guild;
        }       

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
            _client.GuildAvailable += _client_GuildAvailable;

            string token = File.ReadAllText("Token.txt"); // Remember to keep this private!
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
          
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task _client_GuildAvailable(SocketGuild arg)
        {
            await Task.Run(() => DeSerializeRoleList());
            correction.DeSerializeCorrectionsLists();
        }

        private async Task MessageReceived(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
                return;

            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;

            if (message == null)
                return;

            string command = message.Content.ToLower();

            if (command.StartsWith("sos! help"))
            {
                await HandleHelp(message, "sos! help");
                return;
            }

            if (command.StartsWith("sos! corrections"))
            {
                await HandleCorrections(message, "sos! corrections");
                return;
            }

            if (command.StartsWith("sos! addreplace "))
            {
                await HandleAddReplace(message, "sos! addreplace ");
                return;
            }

            if (command.StartsWith("sos! delreplace "))
            {
                await HandleDelReplace(message, "sos! delreplace ");
                return;
            }

            if (command.StartsWith("sos! addignore "))
            {
                await HandleAddIgnore(message, "sos! addignore ");
                return;
            }

            if (command.StartsWith("sos! delignore "))
            {
                await HandleDelIgnore(message, "sos! delignore ");
                return;
            }

            if (command.StartsWith("sos! addcorrection "))
            {
                await HandleAddCorrection(message, "sos! addcorrection ");
                return;
            }

            if (command.StartsWith("sos! delcorrection "))
            {
                await HandleDelCorrection(message, "sos! delcorrection ");
                return;
            }

            if (command.StartsWith("sos! addrole "))
            {
                await HandleAddRole(message, "sos! addrole ");
                return;
            }

            if (command.StartsWith("sos! delrole "))
            {
                await HandleDelRole(message, "sos! delrole ");
                return;
            }

            if (command.StartsWith("sos! roles"))
            {
                await HandleRoles(message, "sos! roles ");
                return;
            }

            if (command.StartsWith("sos! role "))
            {
                await HandleRole(message, "sos! role ");
                return;
            }

            await HandleGodStringsAsync(message);
        }

        private async Task HandleHelp(SocketUserMessage message, string trimmessage)
        {
            await message.Channel.SendMessageAsync(helpMessage);
        }

        #region HandleCorrections
        private async Task HandleCorrections(SocketUserMessage message, string v)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
            List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
            List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            string returnMessage = "```Strings to be replaced:\n";
            foreach (var text in replaceTheseStrings)
                returnMessage += text + "\n";

            returnMessage += "\nStrings to be ignored:\n";
            foreach (var text in ignoreTheseStrings)
                returnMessage += text + "\n";

            returnMessage += "\nCorrections:\n";
            foreach (var text in correctionsStrings)
                returnMessage += text + "\n";

            returnMessage += "```";

            await message.Channel.SendMessageAsync(returnMessage);
            return;
        }

        private async Task HandleAddReplace(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (replaceTheseStrings.Contains(messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```String is already to be replaced.```");
                return;
            }

            //Add and Serialize
            CorrectionGuildStruct cgs = new CorrectionGuildStruct()
            {
                guild = guild.Id,
                text = messagecontent.ToLower()
            };           
            correction.replaceTheseStrings.Add(cgs);
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Added " + messagecontent + " to replace```");
        }

        private async Task HandleDelReplace(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (!replaceTheseStrings.Contains(messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```String is not in list.```");
                return;
            }

            //Remove and Serialize
            correction.replaceTheseStrings.Remove(correction.replaceTheseStrings.Where(x => x.guild == guild.Id && x.text == messagecontent.ToLower()).First());
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Removed " + messagecontent + " from replace```");
        }

        private async Task HandleAddIgnore(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (ignoreTheseStrings.Contains(messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```String is already to be ignored.```");
                return;
            }

            //Add and Serialize
            CorrectionGuildStruct cgs = new CorrectionGuildStruct()
            {
                guild = guild.Id,
                text = messagecontent.ToLower()
            };
            correction.ignoreTheseStrings.Add(cgs);
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Added " + messagecontent + " to ignore```");
        }

        private async Task HandleDelIgnore(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (!ignoreTheseStrings.Contains(messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```String is not in list.```");
                return;
            }

            //Remove and Serialize
            correction.ignoreTheseStrings.Remove(correction.ignoreTheseStrings.Where(x => x.guild == guild.Id && x.text == messagecontent.ToLower()).First());
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Removed " + messagecontent + " from ignore```");
        }

        private async Task HandleAddCorrection(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (correctionsStrings.Contains(messagecontent))
            {
                await message.Channel.SendMessageAsync("```String is already to be replaced.```");
                return;
            }

            //Add and Serialize
            CorrectionGuildStruct cgs = new CorrectionGuildStruct()
            {
                guild = guild.Id,
                text = messagecontent
            };
            correction.correctionsStrings.Add(cgs);
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Added " + messagecontent + " to corrections```");
        }

        private async Task HandleDelCorrection(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;
            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var guilduser = (message.Author as SocketGuildUser);
            List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage corrections.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            if (!correctionsStrings.Contains(messagecontent))
            {
                await message.Channel.SendMessageAsync("```String is not in list.```");
                return;
            }     

            //Remove and Serialize
            correction.correctionsStrings.Remove(correction.correctionsStrings.Where(x => x.guild == guild.Id && x.text == messagecontent).First());
            correction.SerializeCorrectionsLists();
            await message.Channel.SendMessageAsync("```Removed " + messagecontent + " from corrections```");
        }
        #endregion

        #region HandleRoles
        private async Task HandleAddRole(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;

            var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
            var highestposition = guilduserBot.Roles.Where(x => x.Guild == guild).OrderByDescending(x => x.Position).First().Position;

            var guilduser = (message.Author as SocketGuildUser);

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage roles. You can still use \"sos! role <role>\" to set one.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            //Find matching role
            if(!guild.Roles.Any(x => x.Name.ToLower() == messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```Role " + messagecontent + " does not exist.```");
                return;
            }

            var roleToAdd = guild.Roles.First(x => x.Name.ToLower() == messagecontent.ToLower());

            if (roleToAdd.Position >= highestposition)
            {
                await message.Channel.SendMessageAsync("```This role can not be managed.```");
                return;
            }

            //Add role to settable list
            if (!settableRolesList.Contains(roleToAdd))
            {
                settableRolesList.Add(roleToAdd);
                settableRolesList = settableRolesList.OrderByDescending(x => x.Position).ToList();
                await message.Channel.SendMessageAsync("```Role " + roleToAdd.Name + " has been added to settable List.```");

                SerializeRoleList();
            }
            else
            {
                await message.Channel.SendMessageAsync("```Role " + roleToAdd.Name + " already exists in settable List, did you intend to delete it?```");
            }
        }

        private async Task HandleDelRole(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;

            var guilduser = (message.Author as SocketGuildUser);

            //Check if the user is allowed to manage roles
            if (!guilduser.Roles.Any(x => x.Permissions.ManageRoles))
            {
                await message.Channel.SendMessageAsync("```You don't have permission to manage roles. You can still use \"sos! role <role>\" to set one.```");
                return;
            }

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            //Find matching role
            if (!settableRolesList.Where(x => x.Guild == guild).Any(x => x.Name.ToLower() == messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```Role " + messagecontent + " does not exist in settable list.```");
                return;
            }

            var roleToDel = settableRolesList.Where(x => x.Guild == guild).First(x => x.Name.ToLower() == messagecontent.ToLower());

            settableRolesList.Remove(roleToDel);
            await message.Channel.SendMessageAsync("```Role " + roleToDel.Name + " has been removed from settable List.```");

            SerializeRoleList();
        }

        private async Task HandleRole(SocketUserMessage message, string trimmessage)
        {
            //Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            var roles = guild.Roles;

            var guilduser = (message.Author as SocketGuildUser);

            //Split message into parts
            var messagecontent = message.Content.Substring(trimmessage.Length);

            //Find matching role
            if (!settableRolesList.Where(x => x.Guild == guild).Any(x => x.Name.ToLower() == messagecontent.ToLower()))
            {
                await message.Channel.SendMessageAsync("```Role does not exist in settable list.```");
                return;
            }

            var roleToSet = settableRolesList.Where(x => x.Guild == guild).First(x => x.Name.ToLower() == messagecontent.ToLower());

            if(!guilduser.Roles.Contains(roleToSet))
            {
                await guilduser.AddRoleAsync(roleToSet);
                await message.Channel.SendMessageAsync("```Role " + roleToSet.Name + " added.```");
            }
            else
            {
                await guilduser.RemoveRoleAsync(roleToSet);
                await message.Channel.SendMessageAsync("```Role " + roleToSet.Name + " removed.```");
            }              
        }

        private async Task HandleRoles(SocketUserMessage message, string trimmessage)
        {
            var guild = (message.Channel as SocketGuildChannel)?.Guild;

            string answerString = "```Roles:\n";

            foreach(var role in settableRolesList.Where(x => x.Guild == guild))
            {
                answerString += role.Name + "\n";
            }

            answerString += "```";

            await message.Channel.SendMessageAsync(answerString);
        }
        #endregion

        private async Task HandleGodStringsAsync(SocketMessage message)
        {
            // Get information objects
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            string content = message.Content;
            bool userInteractionBool = false;
            List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
            List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
            List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

            if (replaceTheseStrings.Count == 0 || correctionsStrings.Count == 0)
                return;

            foreach (var godString in replaceTheseStrings)
            {
                if (content.ToLower().Contains(godString))
                {
                    content = GetGodStringReplacement(content, godString, ignoreTheseStrings, correctionsStrings);
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

        //Replace all occurences of a Godstring that aren't a antigodstring in a message with corrections
        private string GetGodStringReplacement(string content, string godString, List<string> ignoreTheseStrings, List<string> correctionsStrings)
        {
            //Create copy
            string text = content;
            //Index from which to check for the next godstring
            int startIndex = text.ToLower().IndexOf(godString, 0, StringComparison.Ordinal);
            //Check if a godstring still exists
            while (startIndex != -1)
            {
                //Check if its an anti god string
                bool antiGodBool = false;
                foreach (var antiGodString in ignoreTheseStrings)
                {
                    if (!(content.Length < startIndex + antiGodString.Length) 
                        && content.ToLower().Substring(startIndex, antiGodString.Length) == antiGodString)
                    {
                        antiGodBool = true;
                        startIndex = text.ToLower().IndexOf(godString, startIndex + antiGodString.Length, StringComparison.Ordinal);
                        break;
                    }
                }

                //Skip if it's an antigodstring
                if (antiGodBool)
                    continue;

                // Replace the current occurence of the godstring
                string rep = correctionsStrings.ElementAt(rand.Next(correctionsStrings.Count));

                if (content.ToUpper() == content)
                    rep = rep.ToUpper();

                text = text.Remove(startIndex, godString.Length);                
                text = text.Insert(startIndex, rep);

                //Check for next godstring
                startIndex = text.ToLower().IndexOf(godString, startIndex + rep.Length, StringComparison.Ordinal);
            }

            //Return completely transformed string
            return text;
        }

        private void SerializeRoleList()
        {
            lock (settableRolesLock)
            {
                List<RolesGuildStruct> rolesGuildList = new List<RolesGuildStruct>();

                foreach(var socketRole in settableRolesList)
                {
                    rolesGuildList.Add(new RolesGuildStruct() { role = socketRole.Id, guild = socketRole.Guild.Id });
                }

                // Create an instance of the XmlSerializer class;
                // specify the type of object to serialize.
                XmlSerializer mySerializer = new XmlSerializer(typeof(List<RolesGuildStruct>));
                // To write to a file, create a StreamWriter object.  
                using (StreamWriter myWriter = new StreamWriter("roles.xml"))
                    mySerializer.Serialize(myWriter, rolesGuildList);
            }
        }

        private void DeSerializeRoleList()
        {
            if (!File.Exists("roles.xml"))
                return;            

            List<RolesGuildStruct> tempList;
            // Construct an instance of the XmlSerializer with the type  
            // of object that is being deserialized.  
            XmlSerializer mySerializer = new XmlSerializer(typeof(List<RolesGuildStruct>));
            // To read the file, create a FileStream.  
            using (FileStream myFileStream = new FileStream("roles.xml", FileMode.Open))
            {
                // Call the Deserialize method and cast to the object type.  
                tempList = (List<RolesGuildStruct>)mySerializer.Deserialize(myFileStream);

                //Clear old settable roles
                settableRolesList.Clear();

                foreach (var rolesGuild in tempList)
                {
                    var socketRole = _client.GetGuild(rolesGuild.guild)?.GetRole(rolesGuild.role);

                    if (socketRole != null)
                        settableRolesList.Add(socketRole);
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
