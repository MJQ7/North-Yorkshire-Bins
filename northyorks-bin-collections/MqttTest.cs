using System;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace northyorks_bin_collections
{
    public class MqttTest
    {
        public static async Task ConnectionTest(string brokerHost, int brokerPort)
        {
            Console.WriteLine("Testing MQTT connection...");
            
            try
            {
                using (var client = new TcpClient())
                {
                    Console.WriteLine($"Attempting to connect to {brokerHost}:{brokerPort}...");
                    
                    var connectTask = client.ConnectAsync(brokerHost, brokerPort);
                    
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        Console.WriteLine($"Connection to {brokerHost}:{brokerPort} timed out");
                        return;
                    }
                    
                    Console.WriteLine($"Successfully connected to {brokerHost}:{brokerPort}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to {brokerHost}:{brokerPort}: {ex.Message}");
            }
        }
    }
}
