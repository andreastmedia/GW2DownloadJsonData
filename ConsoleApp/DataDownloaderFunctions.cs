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
    /// <summary>
    /// The functions the <see langword="class"/> <see cref="DataDownloader"/> is using.
    /// </summary>
    public class DataDownloaderFunctions
    {
        private readonly ILogger _logger;

        public DataDownloaderFunctions(ILogger<DataDownloaderFunctions> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Downloads a JSON file from an API.
        /// </summary>
        /// <returns>A <see cref="JArray"/> of items.</returns>
        public async Task<JArray> DownloadItemIDs(HttpClient httpClient)
        {
            string jsonItemIDs = null;
            do
            {
                try
                {
                    jsonItemIDs = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, httpClient.BaseAddress.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Something happened: {ex}");
                    await Task.Delay(1500);
                    jsonItemIDs = null;
                }
            } while (jsonItemIDs == null);

            JArray jArrayIDs = JArray.Parse(jsonItemIDs);

            return jArrayIDs;
        }

        /// <summary>
        /// Downloads multiple <see cref="JValue"/> items based on a <see cref="JArray"/> of IDs.<br/>
        /// NOTE: Serial download.
        /// </summary>
        /// <returns>A <see cref="JArray"/> of items.</returns>
        public async Task<JArray> DownloadItemNames(HttpClient httpClient, JArray jArrayIDs)
        {
            JArray jArrayNames = new JArray();

            for (int i = 0; i < jArrayIDs.Count; i++)
            {
                string jsonItem = await JsonWebProcessor.DownloadJsonFromWeb(httpClient, httpClient.BaseAddress.ToString(), jArrayIDs[i].ToString());
                if (jsonItem == null)
                {
                    i--;
                    await Task.Delay(1500);
                    continue;
                }
                JObject jObjectItem = JObject.Parse(jsonItem);
                jArrayNames.Add(JsonFileProcessor.FindJProperty(jObjectItem, "name"));

                _logger.LogInformation($"{i} from {jArrayIDs.Count}, Name: {jArrayNames[i]}");
            }

            return jArrayNames;
        }

        /// <summary>
        /// Downloads multiple <see cref="JValue"/> items based on a <see cref="JArray"/> of IDs.<br/>
        /// NOTE: Parallel download.
        /// </summary>
        /// <returns>A <see cref="JArray"/> of items.</returns>
        public async Task<JArray> DownloadItemNamesParallel(HttpClient httpClient, JArray jArrayIDs)
        {
            JArray jArrayNames = new JArray();
            Dictionary<int, Task<string>> tasks = new Dictionary<int, Task<string>>();

            int times = 0;
            int percentage = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach<JToken>(jArrayIDs, (id) =>
                {
                    int index = jArrayIDs.IndexOf(id);
                    try
                    {
                        tasks[index] = JsonWebProcessor.DownloadJsonFromWeb(httpClient, httpClient.BaseAddress.ToString(), id.ToString());

                        ++times;
                        percentage = times * 100 / jArrayIDs.Count;
                        _logger.LogInformation($"Percentage: {percentage} %, Times run: {times}/{jArrayIDs.Count}, Current index: {index}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exeption for index {index}: {ex}");
                        return;
                    }
                });
            });

            for (int i = 0; i < jArrayIDs.Count; i++)
            {
                try
                {
                    JObject jObjectItem = JObject.Parse(await tasks[i]);
                    string jsonItemName = JsonFileProcessor.FindJProperty(jObjectItem, "name");
                    jArrayNames.Add(jsonItemName);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exeption when creating the array of names: {ex}");
                }
            }

            _logger.LogInformation($"Names count: {jArrayNames.Count}");

            return jArrayNames;
        }

        /// <summary>
        /// Combines a <see cref="JArray"/> of keys and a <see cref="JArray"/> of values.
        /// </summary>
        /// <returns>The combined <see cref="JObject"/>.</returns>
        public JObject CreateItemDictionary(JArray jArrayKeys, JArray jArrayValues)
        {
            Dictionary<string, List<int>> itemsDictionary = JsonFileProcessor.CombineJArraysToDictionaryOfLists(jArrayKeys, jArrayValues);
            
            return JObject.Parse(JsonConvert.SerializeObject(itemsDictionary));
        }
    }
}
