using JsonMethods;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GW2DownloadJsonData.ConsoleApp
{
    public class NamesDownloader
    {
        private readonly ILogger _logger;
        private IHttpClientFactory _httpFactory { get; set; }
        private DataManager _dataManager;

        public NamesDownloader(ILogger<NamesDownloader> logger,
                               IHttpClientFactory httpFactory,
                               DataManager dataManager)
        {
            _logger = logger;
            _httpFactory = httpFactory;
            _dataManager = dataManager;
        }

        public async Task Run()
        {
            HttpClient httpClient = _httpFactory.CreateClient("GuildWars2");
            DirectoryInfo directory = Directory.CreateDirectory(@"C:\Temp");

            #region Downloading item IDs.

            JArray jArrayIDs = new JArray();

            try
            {
                jArrayIDs = await _dataManager.DownloadItemIDs(httpClient);
            }
            catch (Exception ex)
            {
                _logger.LogError("Something happened: " + ex);
            }

            if (jArrayIDs != null)
            {
                _logger.LogInformation(@"Got IDs. Saved JSON at " + directory);
            }

            #endregion

            #region Downloading item names.

            JArray jArrayNames = new JArray();

            try
            {
                jArrayNames = await _dataManager.DownloadItemNames(httpClient, jArrayIDs);
            }
            catch (Exception ex)
            {
                _logger.LogError("Something happened: " + ex);
            }

            if (jArrayNames != null)
            {
                _logger.LogInformation(@"Got Names.Saved JSON at " + directory);
            }

            #endregion

            #region Adding names and IDs to Dictionary.

            _dataManager.CreateItemDictionary(jArrayIDs, jArrayNames);

            _logger.LogInformation(@"Created Dictionary. Saved at " + directory);

            #endregion
        }
    }
}