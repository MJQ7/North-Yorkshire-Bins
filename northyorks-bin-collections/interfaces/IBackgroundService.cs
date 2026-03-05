namespace northyorks_bin_collections.interfaces;

public interface IBackgroundService
{
    Task StartAsync(CancellationToken cancellationToken);
    
    Task StopAsync(CancellationToken cancellationToken);
}
