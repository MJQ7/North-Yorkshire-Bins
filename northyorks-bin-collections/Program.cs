﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using northyorks_bin_collections;
using northyorks_bin_collections.interfaces;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        await MqttTest.ConnectionTest(
            configuration["Mqtt:BrokerHost"] ?? throw new ArgumentNullException("Mqtt:BrokerHost", "MQTT Broker host must be configured in appsettings.json"),
            configuration.GetValue<int?>("Mqtt:BrokerPort") ?? 1883);
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient();

                services.AddSingleton<IBinCollectionService, BinCollectionService>();
                services.AddSingleton<IMqttService, MqttService>();

                services.AddHostedService<BinCollectionBackgroundService>();

                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
}
