﻿using System;
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
using Newtonsoft.Json;
using static DiscordBot.Correction;

namespace DiscordBot
{
	public class Program
	{
		private DiscordSocketClient _client;
		private object settableRolesLock = new object();
		private object correctionsLock = new object();
		public static Random rand = new Random();

		// Serializer Settings, shorthand to reduce code
		JsonSerializerSettings serSet = new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			Formatting = Formatting.Indented,
		};

		//Hardcoded strings
		private string helpMessage = "Documentation can be found at: https://master117.github.io/EasyDiscordBotPage/";

		//Correction Class
		private static Correction correction = new Correction();

		//Dicts used to store usage info
		private Dictionary<ulong, int> correctionCount = new Dictionary<ulong, int>();
		private int maxCorrectionCount = 1;
		private Dictionary<ulong, List<DateTime>> spamCount = new Dictionary<ulong, List<DateTime>>();
		private int maxSpamCount = 3;
		private List<ulong> settableRolesList = new List<ulong>();

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
			await Task.Run(() => DeserializeRoleList());
			await Task.Run(() => DeserializeCorrectionCount());
			await Task.Run(() => DeserializeMaxCorrectionCount());
			await Task.Run(() => DeserializeMaxSpamCount());
			correction.DeserializeCorrectionsLists();
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

			if (command.StartsWith("sos! maxcorrections "))
			{
				await HandleMaxCorrectionCount(message, "sos! maxcorrections ");
				return;
			}

			if (command.StartsWith("sos! maxspam "))
			{
				await HandleMaxSpamCount(message, "sos! maxspam ");
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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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
			if (!canHandleRolesWithResponse(message).Result)
				return;

			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

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

		private async Task HandleMaxCorrectionCount(SocketUserMessage message, string trimmessage)
		{
			if (!canHandleRolesWithResponse(message).Result)
				return;

			//Split message into parts
			var messagecontent = message.Content.Substring(trimmessage.Length);

			//Update and Serialize
			maxCorrectionCount = int.Parse(messagecontent);
			SerializeMaxCorrectionCount();
			await message.Channel.SendMessageAsync("```Set max corrections to " + messagecontent + "```");
		}

		private async Task HandleMaxSpamCount(SocketUserMessage message, string trimmessage)
		{
			if (!canHandleRolesWithResponse(message).Result)
				return;

			//Split message into parts
			var messagecontent = message.Content.Substring(trimmessage.Length);

			//Update and Serialize
			maxSpamCount = int.Parse(messagecontent);
			SerializeMaxSpamCount();
			await message.Channel.SendMessageAsync("```Set max spam count to " + messagecontent + "```");
		}
		#endregion

		#region HandleRoles
		private async Task HandleAddRole(SocketUserMessage message, string trimmessage)
		{
			if (!canHandleRoles(message))
			{
				await message.Channel.SendMessageAsync("```You don't have permission to manage roles. " +
					"You can use \"sos! role <role>\" to set one to yourself.```");
				return;
			}

			//Get information objects
			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			var roles = guild.Roles;
			var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
			var highestposition = guilduserBot.Roles.Where(x => x.Guild == guild).OrderByDescending(x => x.Position).First().Position;

			//Split message into parts
			var messagecontent = message.Content.Substring(trimmessage.Length);

			//Find matching role
			if (!guild.Roles.Any(x => x.Name.ToLower() == messagecontent.ToLower()))
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
			if (!settableRolesList.Contains(roleToAdd.Id))
			{
				settableRolesList.Add(roleToAdd.Id);
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
			if (!canHandleRoles(message))
			{
				await message.Channel.SendMessageAsync("```You don't have permission to manage roles. " +
					"You can use \"sos! role <role>\" to set remove one from yourself.```");
				return;
			}

			//Get information objects
			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			var roles = guild.Roles;
			var guilduser = (message.Author as SocketGuildUser);

			//Split message into parts
			var messagecontent = message.Content.Substring(trimmessage.Length);

			//Find matching role
			var matchingRoles = guild.Roles.Where(x => x.Name.ToLower() == messagecontent.ToLower());
			var roleToSet = matchingRoles.FirstOrDefault(x => settableRolesList.Contains(x.Id));
			if (roleToSet == null)
			{
				await message.Channel.SendMessageAsync("```Role does not exist in settable list.```");
				return;
			}

			settableRolesList.Remove(roleToSet.Id);
			await message.Channel.SendMessageAsync("```Role " + roleToSet.Name + " has been removed from settable List.```");

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
			var matchingRoles = guild.Roles.Where(x => x.Name.ToLower() == messagecontent.ToLower());
			var roleToSet = matchingRoles.FirstOrDefault(x => settableRolesList.Contains(x.Id));
			if(roleToSet == null)
			{
				await message.Channel.SendMessageAsync("```Role does not exist in settable list.```");
				return;
			}

			if (!guilduser.Roles.Contains(roleToSet))
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
			var roles = guild.Roles;

			string answerString = "```Roles:\n";

			foreach (var role in roles.Where(x => settableRolesList.Contains(x.Id)))
				answerString += role.Name + "\n";

			answerString += "```";

			await message.Channel.SendMessageAsync(answerString);
		}
		#endregion



		private async Task<bool> canHandleRolesWithResponse(SocketUserMessage message)
		{
			bool canHandle = canHandleRoles(message);
			if (!canHandle)
				await message.Channel.SendMessageAsync("```You don't have permission to manage roles. You can still use \"sos! role <role>\" to set one.```");

			return canHandle;
		}

		//Check if the user is allowed to manage roles
		private bool canHandleRoles(SocketUserMessage message)
		{
			//Get information objects
			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			var roles = guild.Roles;
			var guilduserBot = guild.GetUser(_client.CurrentUser.Id);
			var guilduser = (message.Author as SocketGuildUser);

			//Check if the user is allowed to manage roles
			return guilduser.Roles.Any(x => x.Permissions.ManageRoles);
		}

		private async Task HandleGodStringsAsync(SocketMessage message)
		{
			// Get information objects
			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			string content = message.Content;
			List<string> replaceTheseStrings = correction.replaceTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
			List<string> ignoreTheseStrings = correction.ignoreTheseStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();
			List<string> correctionsStrings = correction.correctionsStrings.Where(x => x.guild == guild.Id).Select(x => x.text).ToList();

			if (replaceTheseStrings.Count == 0 || correctionsStrings.Count == 0)
				return;

			foreach (var godString in replaceTheseStrings)
				if (content.ToLower().Contains(godString))
					content = GetGodStringReplacement(content, godString, ignoreTheseStrings, correctionsStrings);

			//Only reply when the message has been changed
			if (message.Content != content)
			{
				// exclude messages over the all time restriction
				int allTime = CheckForCorrections(message.Author.Id);
				if (allTime >= maxCorrectionCount)
					return;

				// exclude messages over the spam resctriction
				int spam = CheckForSpam(message.Author.Id);
				if (spam >= maxSpamCount)
					return;

				AddCorrection(message.Author.Id);
				AddSpam(message.Author.Id);

				string answer = "*The Heretic* ***" + message.Author.Username
								+ "*** *wanted to write:* **" + content + "**";

				if (spam == 2)
					answer += "\n This is your last correction for today.";

				if (allTime == 0)
					answer += "\n This is your last correction ever.";

				if (message.Content != content)
					await message.Channel.SendMessageAsync(answer);
			}
		}

		//Add to the number of spam posts a User made
		private void AddSpam(ulong id)
		{
			//Check SpamList
			//Check for entry existance
			if (!spamCount.ContainsKey(id))
			{
				spamCount.Add(id, new List<DateTime>());
				spamCount[id].Add(DateTime.Now);
				return;
			}

			//Delete old Dates from spamlist
			for (int i = 0; i < spamCount[id].Count; i++)
				if (spamCount[id][i].AddHours(24) < DateTime.Now)
					spamCount[id].RemoveAt(i);

			//Check if user is spamming
			if (spamCount[id].Count >= maxSpamCount)
				//User is already at max spam count, don't add a new entry as his punishment would be even longer
				return;

			//User is in good standing, add one more
			spamCount[id].Add(DateTime.Now);
		}

		//Returns the number of spam posts a user made
		private int CheckForSpam(ulong id)
		{
			//If user is in spamlist and count >= 3 it means he is spamming
			return spamCount.ContainsKey(id) ? spamCount[id].Count : 0;
		}

		//Adds a new correction
		private void AddCorrection(ulong id)
		{
			if (!correctionCount.ContainsKey(id))
				correctionCount.Add(id, 1);
			else
				correctionCount[id]++;

			SerializeCorrectionCount();
		}

		//Returns the number of corrections a user received
		private int CheckForCorrections(ulong id)
		{
			//If user is in spamlist and count >= 3 it means he is spamming
			return correctionCount.ContainsKey(id) ? correctionCount[id] : 0;
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
				using (StreamWriter file = File.CreateText("roles.json"))
				{
					JsonSerializer serializer = JsonSerializer.Create(serSet);
					serializer.Serialize(file, settableRolesList);
				}
			}
		}

		private void DeserializeRoleList()
		{
			if (!File.Exists("roles.json"))
				return;

			using (StreamReader file = File.OpenText("roles.json"))
			{
				JsonSerializer serializer = JsonSerializer.Create(serSet);
				settableRolesList = (List<ulong>)serializer.Deserialize(file, typeof(List<ulong>));
			}
		}

		private void SerializeCorrectionCount()
		{
			lock (correctionsLock)
			{
				using (StreamWriter file = File.CreateText("correctionCount.json"))
				{
					JsonSerializer serializer = JsonSerializer.Create(serSet);
					serializer.Serialize(file, correctionCount);
				}
			}
		}

		private void DeserializeCorrectionCount()
		{
			if (!File.Exists("correctionCount.json"))
				return;

			using (StreamReader file = File.OpenText("correctionCount.json"))
			{
				JsonSerializer serializer = JsonSerializer.Create(serSet);
				correctionCount = (Dictionary<ulong, int>)serializer.Deserialize(file, typeof(Dictionary<ulong, int>));
			}
		}

		private void SerializeMaxCorrectionCount()
		{
			lock (correctionsLock)
			{
				using (StreamWriter file = File.CreateText("maxCorrectionCount.json"))
				{
					JsonSerializer serializer = JsonSerializer.Create(serSet);
					serializer.Serialize(file, maxCorrectionCount);
				}
			}
		}

		private void DeserializeMaxCorrectionCount()
		{
			if (!File.Exists("maxCorrectionCount.json"))
				return;

			using (StreamReader file = File.OpenText("maxCorrectionCount.json"))
			{
				JsonSerializer serializer = JsonSerializer.Create(serSet);
				maxCorrectionCount = (int)serializer.Deserialize(file, typeof(int));
			}
		}

		private void SerializeMaxSpamCount()
		{
			lock (correctionsLock)
			{
				using (StreamWriter file = File.CreateText("maxSpamCount.json"))
				{
					JsonSerializer serializer = JsonSerializer.Create(serSet);
					serializer.Serialize(file, maxCorrectionCount);
				}
			}
		}

		private void DeserializeMaxSpamCount()
		{
			if (!File.Exists("maxSpamCount.json"))
				return;

			using (StreamReader file = File.OpenText("maxSpamCount.json"))
			{
				JsonSerializer serializer = JsonSerializer.Create(serSet);
				maxSpamCount = (int)serializer.Deserialize(file, typeof(int));
			}
		}

		// Write Log including time
		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}
