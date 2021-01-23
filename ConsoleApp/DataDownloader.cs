using JsonMethods;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GW2DownloadJsonData.ConsoleApp
{
    /// <summary>
    /// The main logic of the Console Application.
    /// </summary>
    public class DataDownloader
    {
        private readonly ILogger _logger;
        private IHttpClientFactory _httpFactory { get; set; }
        private DataDownloaderFunctions _dataDownloaderFunctions;

        public DataDownloader(ILogger<DataDownloader> logger,
                               IHttpClientFactory httpFactory,
                               DataDownloaderFunctions dataDownloaderFunctions)
        {
            _logger = logger;
            _httpFactory = httpFactory;
            _dataDownloaderFunctions = dataDownloaderFunctions;
        }

        /// <summary>
        /// Defines the order of downloading the JSON data from the API:
        /// <list type="bullet">
        /// <item>Calls the <see cref="HttpClient"/> and creates the directory where the files will be saved.</item>
        /// <item>Downloads and saves the items' IDs in a <see cref="JArray"/>.</item>
        /// <item>Downloads and saves the items' names in a <see cref="JArray"/> by using their IDs.</item>
        /// <item>Creates a <see cref="Dictionary{TKey, TValue}"/> with the names as keys and the IDs as values.</item>
        /// </list>
        /// Now we have a database with all the items and we can search for each item by name.<br/>
        /// It is worth mentioning that one name may correspond to an array of IDs (different attributes in game).
        /// </summary>
        public async Task Run()
        {
            HttpClient httpClient = _httpFactory.CreateClient("GuildWars2");
            DirectoryInfo directory = Directory.CreateDirectory(@"C:\Temp");

            #region Downloading item IDs.

            JArray jArrayIDs = new JArray();

            try
            {
                jArrayIDs = await _dataDownloaderFunctions.DownloadItemIDs(httpClient);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something happened: {ex}");
            }

            if (jArrayIDs != null)
            {
                JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsIDsList.json", jArrayIDs.ToString());

                _logger.LogInformation(@$"Got IDs. Saved JSON at {directory}\GW2_ItemsIDsList.json");
            }

            #endregion

            #region Downloading item names.

            JArray jArrayNames = new JArray();

            try
            {
                jArrayNames = await _dataDownloaderFunctions.DownloadItemNamesParallel(httpClient, jArrayIDs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something happened: {ex}");
            }

            if (jArrayNames != null)
            {
                JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemsNamesList.json", jArrayNames.ToString());

                _logger.LogInformation(@$"Got Names. Saved JSON at {directory}\GW2_ItemsNamesList.json");
            }

            #endregion

            #region Adding names and IDs to Dictionary.

            JObject jObjectDict = _dataDownloaderFunctions.CreateItemDictionary(jArrayNames, jArrayIDs);

            JsonFileProcessor.SaveJsonToFolder(@"C:\Temp", "GW2_ItemDictionary.json", jObjectDict.ToString());

            _logger.LogInformation(@$"Created Dictionary. Saved at {directory}\GW2_ItemDictionary.json");

            #endregion
        }
    }
}