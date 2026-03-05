using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using northyorks_bin_collections.interfaces;
using System.Text.Json;
using NCrontab;

namespace northyorks_bin_collections;

public class BinCollectionBackgroundService : BackgroundService, IBackgroundService
{
    private readonly IBinCollectionService _binCollectionService;
    private readonly IMqttService _mqttService;
    private readonly ILogger<BinCollectionBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;
    private readonly string _deviceId;
    private readonly string _nextBinTypeTopic;
    private readonly string _nextBinCollectionTimestampTopic;
    private readonly string _futureBinTypeTopic;

    public BinCollectionBackgroundService(
        IBinCollectionService binCollectionService,
        IMqttService mqttService,
        ILogger<BinCollectionBackgroundService> logger,
        IConfiguration configuration)
    {
        _binCollectionService = binCollectionService;
        _mqttService = mqttService;
        _logger = logger;
        _configuration = configuration;
        
        _deviceId = _configuration["HomeAssistant:DeviceId"] ?? "northyorks_bin_collections";
        var sensorPrefix = _configuration["HomeAssistant:SensorPrefix"] ?? "homeassistant/sensor";
        _nextBinTypeTopic = $"{sensorPrefix}/{_deviceId}/next_bin_type";
        _nextBinCollectionTimestampTopic = $"{sensorPrefix}/{_deviceId}/next_bin_collection_timestamp";
        _futureBinTypeTopic = $"{sensorPrefix}/{_deviceId}/future_bin_type";
        
        var cronExpression = _configuration["Schedule:CronExpression"] ?? "1 0 * * *";
        Console.WriteLine($"Loaded cron schedule from config: {cronExpression}");
        
        _schedule = CrontabSchedule.Parse(cronExpression);
        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
        Console.WriteLine($"Next scheduled execution at: {_nextRun.ToString("yyyy-MM-dd HH:mm:ss")}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now > _nextRun)
            {
                Console.WriteLine("Bin Collection Background Service is starting.");

                try
                {
                    await _mqttService.ConnectAsync();
                    
                    await PublishDiscoveryConfigurations();
            
                    Console.WriteLine("Updating bin collection data...");
                    
                    try
                    {
                        await UpdateBinCollectionData();
                        Console.WriteLine("Bin collection data updated successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating bin collection data");
                    }
                    
                    _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                    Console.WriteLine($"Next scheduled execution at: {_nextRun}");
                
                    var delay = _nextRun - DateTime.Now;
                    if (delay <= TimeSpan.Zero)
                    {
                        delay = TimeSpan.FromSeconds(1);
                    }
                
                    // wait until the next check
                    var delayTime = Math.Min(delay.TotalMilliseconds, TimeSpan.FromMinutes(1).TotalMilliseconds);
                    await Task.Delay(TimeSpan.FromMilliseconds(delayTime), stoppingToken);
                
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fatal error in Bin Collection Background Service");
                }
                finally
                {
                    await _mqttService.DisconnectAsync();
                    Console.WriteLine("Bin Collection Background Service task completed.");
                }
            }
        }
    }

    private async Task UpdateBinCollectionData()
    {
        try
        {
            var collections = await _binCollectionService.GetBinCollectionsAsync();
            var ordered = collections
                .Where(x => x.CollectionDate.HasValue)
                .OrderBy(x => x.CollectionDate)
                .ToList();

            var nextBinType = ordered.FirstOrDefault()?.BinType ?? "Unknown";
            var nextCollectionDate = ordered.FirstOrDefault()?.CollectionDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "Unknown";
            var futureBinType = ordered.Skip(1).FirstOrDefault()?.BinType ?? "Unknown";

            Console.WriteLine($"Next week bin type: {nextBinType}");
            Console.WriteLine($"Next collection date: {nextCollectionDate}");
            Console.WriteLine($"Future bin type: {futureBinType}");
            
            await _mqttService.ConnectAsync();
            
            Console.WriteLine("Publishing to MQTT topics");
            await _mqttService.PublishAsync(_nextBinTypeTopic + "/state", nextBinType);
            await _mqttService.PublishAsync(_nextBinCollectionTimestampTopic + "/state", nextCollectionDate);            
            await _mqttService.PublishAsync(_futureBinTypeTopic + "/state", futureBinType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bin collection data");
            throw;
        }
    }
    
    private async Task PublishDiscoveryConfigurations()
    {
        var device = new {
            identifiers = new[] { _deviceId },
            name = "North Yorkshire Bin Collections",
            manufacturer = "North Yorkshire Council",
            model = "Bin Collection API",
            sw_version = "1.0"
        };

        var nextBinTypeJson = JsonSerializer.Serialize(new {
            name = "Next Bin Type",
            unique_id = "next_bin_type",
            object_id = "next_bin_type",
            state_topic = _nextBinTypeTopic + "/state",
            icon = "mdi:trash-can",
            device
        });

        var nextCollectionDateJson = JsonSerializer.Serialize(new {
            name = "Next Collection Timestamp",
            unique_id = "next_bin_collection_timestamp",
            object_id = "next_bin_collection_timestamp",
            state_topic = _nextBinCollectionTimestampTopic + "/state",
            icon = "mdi:calendar",
            device_class = "timestamp",
            device
        });

        var futureBinTypeJson = JsonSerializer.Serialize(new {
            name = "Future Bin Type",
            unique_id = "future_bin_type",
            object_id = "future_bin_type",
            state_topic = _futureBinTypeTopic + "/state",
            icon = "mdi:delete-clock",
            device
        });
        
        await _mqttService.PublishAsync(_nextBinTypeTopic + "/config", nextBinTypeJson);
        await _mqttService.PublishAsync(_nextBinCollectionTimestampTopic + "/config", nextCollectionDateJson);
        await _mqttService.PublishAsync(_futureBinTypeTopic + "/config", futureBinTypeJson);
        
        Console.WriteLine("Published discovery configurations for Home Assistant");
    }
}
