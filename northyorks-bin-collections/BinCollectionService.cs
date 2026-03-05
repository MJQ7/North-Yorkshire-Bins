using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using northyorks_bin_collections.interfaces;
using System.Net.Http;

namespace northyorks_bin_collections;

public class BinCollectionService : IBinCollectionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BinCollectionService> _logger;
    private readonly string _url;
    private readonly string _referrerUrl;

    public BinCollectionService(IHttpClientFactory httpClientFactory, ILogger<BinCollectionService> logger, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _url = configuration["BinCollection:Url"] ?? throw new ArgumentNullException("BinCollection:Url", "Bin collection URL must be configured in appsettings.json");
        _referrerUrl = configuration["BinCollection:ReferrerUrl"] ?? throw new ArgumentNullException("BinCollection:ReferrerUrl", "Referrer URL must be configured in appsettings.json");
    }

    public async Task<List<BinCollection>> GetBinCollectionsAsync()
    {
        try
        {
            Console.WriteLine("Fetching bin collections from North Yorkshire Council");
            
            var httpClient = _httpClientFactory.CreateClient();
            
            var request = new HttpRequestMessage(HttpMethod.Post, _url);

            request.Headers.Add("User-Agent", "Mozilla/5.0");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Referrer = new Uri(_referrerUrl);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("js", "true"),
                new KeyValuePair<string, string>("_drupal_ajax", "1"),
                new KeyValuePair<string, string>("ajax_page_state[theme]", "northyorks_base"),
                new KeyValuePair<string, string>("ajax_page_state[theme_token]", ""),
                new KeyValuePair<string, string>("ajax_page_state[libraries]", "eJx1jNEOgyAMRX8I5XP2SFroGLNQU1Dn3w_nizHZS3PuPTfFVJwHphJALV6CwX_GsUAwXpRs0GUGHuENH_OU0mCjKpnshceN8IjVRJHI5Hwle2KnBzTSDDoZlv4_yuoQ-gCVIHhdMtabiSwIfCtniDS01JgG7GYyRbS9dtGpnoMMqZi610bZHoVZE23V_u6YJSxMXxyFZ88")
            });

            var response = await httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            var jsonArray = JArray.Parse(responseString);
            var htmlFragment = jsonArray[0]["data"]?.ToString() ?? string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlFragment);

            var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'row-highlight')]/tbody/tr");

            var binCollections = new List<BinCollection>();

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null && cells.Count == 3)
                    {
                        string date = cells[0].InnerText.Trim();
                        string day = cells[1].InnerText.Trim();
                        string binType = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());

                        if (DateTime.TryParse(date, out DateTime collectionDate))
                        {
                            binCollections.Add(new BinCollection
                            {
                                Date = date,
                                Day = day,
                                BinType = binType,
                                CollectionDate = collectionDate
                            });
                        }
                        else
                        {
                            binCollections.Add(new BinCollection
                            {
                                Date = date,
                                Day = day,
                                BinType = binType
                            });
                        }
                    }
                }
            }

            Console.WriteLine($"Found {binCollections.Count} bin collections");
            return binCollections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bin collections");
            throw;
        }
    }

    public async Task<string> GetNextBinTypeAsync()
        => (await GetBinCollectionsAsync())
                .Where(x => x.CollectionDate.HasValue)
                .OrderBy(x => x.CollectionDate)
                .FirstOrDefault()?.BinType ?? "Unknown";

    public async Task<string> GetNextCollectionDateAsync()
        => (await GetBinCollectionsAsync())
                .Where(x => x.CollectionDate.HasValue)
                .OrderBy(x => x.CollectionDate)
                .FirstOrDefault()?.CollectionDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "Unknown";

    public async Task<string> GetFutureBinTypeAsync()
        => (await GetBinCollectionsAsync())
                .Where(x => x.CollectionDate.HasValue)
                .OrderBy(x => x.CollectionDate)
                .Skip(1)
                .FirstOrDefault()?.BinType ?? "Unknown";
                
}
