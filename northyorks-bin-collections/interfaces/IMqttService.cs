namespace northyorks_bin_collections.interfaces;

/// <summary>
/// Interface for MQTT service to publish bin collection data to Home Assistant
/// </summary>
public interface IMqttService
{
    /// <summary>
    /// Connect to the MQTT broker
    /// </summary>
    Task ConnectAsync();
    
    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <param name="topic">The MQTT topic</param>
    /// <param name="payload">The message payload</param>
    Task PublishAsync(string topic, string payload);
    
    /// <summary>
    /// Disconnect from the MQTT broker
    /// </summary>
    Task DisconnectAsync();
}
