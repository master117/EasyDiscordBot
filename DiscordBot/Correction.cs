using Discord.WebSocket;
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

        //Contains strings to be replaced
        public List<CorrectionGuildStruct> replaceTheseStrings = new List<CorrectionGuildStruct>();
        public List<CorrectionGuildStruct> ignoreTheseStrings = new List<CorrectionGuildStruct>();
        public List<CorrectionGuildStruct> correctionsStrings = new List<CorrectionGuildStruct>();

        public struct CorrectionGuildStruct
        {
            public string text;
            public ulong guild;
        }

        public void SerializeCorrectionsLists()
        {
            lock (correctionsLock)
            {
                // Create an instance of the XmlSerializer class;
                // specify the type of object to serialize.
                XmlSerializer mySerializer = new XmlSerializer(typeof(List<CorrectionGuildStruct>));
                // To write to a file, create a StreamWriter object.  
                using (StreamWriter myWriter = new StreamWriter("replaceThese.xml"))
                    mySerializer.Serialize(myWriter, replaceTheseStrings);

                using (StreamWriter myWriter = new StreamWriter("ignoreThese.xml"))
                    mySerializer.Serialize(myWriter, ignoreTheseStrings);

                using (StreamWriter myWriter = new StreamWriter("corrections.xml"))
                    mySerializer.Serialize(myWriter, correctionsStrings);
            }
        }

        public void DeSerializeCorrectionsLists()
        {
            if (!File.Exists("replaceThese.xml") || !File.Exists("ignoreThese.xml") || !File.Exists("corrections.xml"))
                return;

            // Construct an instance of the XmlSerializer with the type  
            // of object that is being deserialized.  
            XmlSerializer mySerializer = new XmlSerializer(typeof(List<CorrectionGuildStruct>));
            // To read the file, create a FileStream.  
            using (FileStream myFileStream = new FileStream("replaceThese.xml", FileMode.Open))
                // Call the Deserialize method and cast to the object type.  
                replaceTheseStrings = (List<CorrectionGuildStruct>)mySerializer.Deserialize(myFileStream);

            using (FileStream myFileStream = new FileStream("ignoreThese.xml", FileMode.Open))
                ignoreTheseStrings = (List<CorrectionGuildStruct>)mySerializer.Deserialize(myFileStream);

            using (FileStream myFileStream = new FileStream("corrections.xml", FileMode.Open))
                correctionsStrings = (List<CorrectionGuildStruct>)mySerializer.Deserialize(myFileStream);
        }
    }
}
