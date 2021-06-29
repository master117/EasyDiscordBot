using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DiscordBot
{
    public class Correction
    {
        private static object correctionsLock = new object();
        public struct CorrectionGuildStruct
        {
            public string text;
            public ulong guild;
        }

        // Serializer Settings, shorthand to reduce code
        JsonSerializerSettings serSet = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        //Contains strings to be replaced
        public List<CorrectionGuildStruct> replaceTheseStrings = new List<CorrectionGuildStruct>();
        public List<CorrectionGuildStruct> ignoreTheseStrings = new List<CorrectionGuildStruct>();
        public List<CorrectionGuildStruct> correctionsStrings = new List<CorrectionGuildStruct>();

        public void SerializeCorrectionsLists()
        {
            lock (correctionsLock)
            {
                JsonSerializer serializer = JsonSerializer.Create(serSet);
                using (StreamWriter file = File.CreateText("replaceThese.json"))
                    serializer.Serialize(file, replaceTheseStrings);
                using (StreamWriter file = File.CreateText("ignoreThese.json"))
                    serializer.Serialize(file, ignoreTheseStrings);
                using (StreamWriter file = File.CreateText("corrections.json"))
                    serializer.Serialize(file, correctionsStrings);
            }
        }

        public void DeserializeCorrectionsLists()
        {
            if (!File.Exists("replaceThese.json") || !File.Exists("ignoreThese.json") || !File.Exists("corrections.json"))
                return;

            JsonSerializer serializer = JsonSerializer.Create(serSet);

            using (StreamReader file = File.OpenText("replaceThese.json"))
                replaceTheseStrings = (List<CorrectionGuildStruct>)serializer.Deserialize(file, typeof(List<CorrectionGuildStruct>));
            using (StreamReader file = File.OpenText("ignoreThese.json"))
                ignoreTheseStrings = (List<CorrectionGuildStruct>)serializer.Deserialize(file, typeof(List<CorrectionGuildStruct>));
            using (StreamReader file = File.OpenText("corrections.json"))
                correctionsStrings = (List<CorrectionGuildStruct>)serializer.Deserialize(file, typeof(List<CorrectionGuildStruct>));
        }
    }
}
