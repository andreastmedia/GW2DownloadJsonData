using JsonMethods;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GW2DownloadJsonData.ConsoleApp
{
    public class DataManager
    {
        private readonly ILogger _logger;

        public DataManager(ILogger<DataManager> logger)
        {
            _logger = logger;
        }

        public async Task<JArray> DownloadItemIDs(HttpClient httpClient)
        {
            object jsonItemIDs = null;
            do
            {
                try
                {
                    jsonItemIDs = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, httpClient.BaseAddress.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception: " + ex);
                    await Task.Delay(1500);
                    jsonItemIDs = null;
                }
            } while (jsonItemIDs == null);

            JArray jArrayIDs = JArray.Parse(jsonItemIDs.ToString());

            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsIDsList.json", jArrayIDs.ToString());

            return jArrayIDs;
        }

        public async Task<JArray> DownloadItemNames(HttpClient httpClient, JArray jArrayIDs)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < jArrayIDs.Count; i++)
            {
                object jsonItem = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, httpClient.BaseAddress.ToString(), jArrayIDs[i].ToString());
                if (jsonItem == null)
                {
                    i--;
                    await Task.Delay(1500);
                    continue;
                }
                JObject jObject = JObject.Parse(jsonItem.ToString());
                string jsonItemName = JsonFileProcessor.FindJProperty(jObject, "name");
                names.Add(jsonItemName);

                _logger.LogInformation(i + " from " + jArrayIDs.Count);
            }

            string jsonStringNames = JsonConvert.SerializeObject(names);
            JArray jArrayNames = JsonConvert.DeserializeObject<JArray>(jsonStringNames);

            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsNamesList.json", jArrayNames.ToString());

            return jArrayNames;
        }

        public void CreateItemDictionary(JArray jArrayIDs, JArray jArrayNames)
        {
            Dictionary<string, List<int>> itemsDictionary = JsonFileProcessor.CombineJArraysToDictionaryOfLists(jArrayNames, jArrayIDs);
            JObject jObjectDict = JObject.Parse(JsonConvert.SerializeObject(itemsDictionary));

            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemDictionary", jObjectDict.ToString());
        }
    }
}
