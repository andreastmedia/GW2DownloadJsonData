using JsonMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GW2DownloadJsonData
{
    /// <summary>
    /// Uses the JsonMethods namespace that you can find at "https://github.com/andreastmedia/Json_Processing".
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();
            DirectoryInfo directory = Directory.CreateDirectory(@"C:\Temp");

            JArray jArrayIDs = await DownloadItemIDs(httpClient);

            if (jArrayIDs != null)
            {
                Console.WriteLine(@"Got IDs. Saved JSON at " + directory);
            }

            JArray jArrayNames = await DownloadItemNames(httpClient, jArrayIDs);

            if (jArrayNames != null)
            {
                Console.WriteLine(@"Got Names.Saved JSON at " + directory);
            }

            CreateItemDictionary(jArrayIDs, jArrayNames);

            Console.WriteLine(@"Created Dictionary. Saved at " + directory);
        }

        private static async Task<JArray> DownloadItemIDs(HttpClient httpClient)
        {
            object jsonItemIDs = null;
            do
            {
                try
                {
                    jsonItemIDs = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, "https://api.guildwars2.com/v2/items");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex);
                    jsonItemIDs = null;
                }
            } while (jsonItemIDs == null);
            
            JArray jArrayIDs = JArray.Parse(jsonItemIDs.ToString());
            
            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsIDsList.json", jArrayIDs.ToString());

            return jArrayIDs;
        }

        private static async Task<JArray> DownloadItemNames(HttpClient httpClient, JArray jArrayIDs)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < jArrayIDs.Count; i++)
            {
                object jsonItem = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, "https://api.guildwars2.com/v2/items", jArrayIDs[i].ToString());
                if (jsonItem == null)
                {
                    i--;
                    continue;
                }
                JObject jObject = JObject.Parse(jsonItem.ToString());
                string jsonItemName = JsonFileProcessor.FindJProperty(jObject, "name");
                names.Add(jsonItemName);

                Console.WriteLine(i + " from " + jArrayIDs.Count);
            }

            string jsonStringNames = JsonConvert.SerializeObject(names);
            JArray jArrayNames = JsonConvert.DeserializeObject<JArray>(jsonStringNames);
            
            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsNamesList.json", jArrayNames.ToString());

            return jArrayNames;
        }

        private static void CreateItemDictionary(JArray jArrayIDs, JArray jArrayNames)
        {
            Dictionary<string, List<int>> itemsDictionary = JsonFileProcessor.CombineJArraysToDictionaryOfLists(jArrayNames, jArrayIDs);
            JObject jObjectDict = JObject.Parse(JsonConvert.SerializeObject(itemsDictionary));

            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemDictionary", jObjectDict.ToString());
        }
    }
}