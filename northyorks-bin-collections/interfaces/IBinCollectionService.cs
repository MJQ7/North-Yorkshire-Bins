namespace northyorks_bin_collections.interfaces;

/// <summary>
/// Interface for bin collection service to fetch and process bin collection data
/// </summary>
public interface IBinCollectionService
{
    /// <summary>
    /// Get all bin collections
    /// </summary>
    /// <returns>List of bin collections</returns>
    Task<List<BinCollection>> GetBinCollectionsAsync();
    
    /// <summary>
    /// Get the bin type for the current week
    /// </summary>
    /// <returns>The bin type for the current week</returns>
    Task<string> GetNextBinTypeAsync();

    Task<string> GetNextCollectionDateAsync();

    /// <summary>
    /// Get the bin type for the next week
    /// </summary>
    /// <returns>The bin type for the next week</returns>
    Task<string> GetFutureBinTypeAsync();
}
