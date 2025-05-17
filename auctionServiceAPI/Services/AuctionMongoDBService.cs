using auctionServiceAPI.Model;
using MongoDB.Bson;
using MongoDB.Driver;

namespace auctionServiceAPI.Services;

public interface IAuctionService
{
    Task<IEnumerable<Auction>> GetAllAuctionsAsync();
    Task<Auction> GetAuctionAsync(Guid id);
    Task<Guid> CreateAuctionAsync(Auction auction);
    Task<bool> UpdateAuctionAsync(Auction auction);
    Task<bool> DeleteAuctionAsync(Guid id);
}

public class AuctionMongoDBService : IAuctionService
{
    private readonly ILogger<AuctionMongoDBService> _logger;
    private readonly IMongoCollection<Auction> _collection;

    public AuctionMongoDBService(ILogger<AuctionMongoDBService> logger, MongoDBContext dbcontext)
    {
        _logger = logger;
        _collection = dbcontext.Collection;
    }

    public async Task<IEnumerable<Auction>> GetAllAuctionsAsync()
    {
        _logger.LogInformation("Getting all auctions from database");
        var filter = Builders<Auction>.Filter.Empty;

        try
        {
            _logger.LogInformation($"Database: {_collection.Database.DatabaseNamespace.DatabaseName}");
            _logger.LogInformation($"Collection: {_collection.CollectionNamespace.CollectionName}");

            var count = await _collection.CountDocumentsAsync(filter);
            _logger.LogInformation($"Found {count} documents in collection");

            var auctions = await _collection.Find(filter).ToListAsync();
            _logger.LogInformation($"Retrieved {auctions.Count} auctions");
            return auctions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve auctions");
            return new List<Auction>();
        }
    }

    public async Task<Auction> GetAuctionAsync(Guid id)
    {
        var filter = Builders<Auction>.Filter.Eq(x => x.AuctionId, id);

        try
        {
            return await _collection.Find(filter).SingleOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to retrieve auction with ID {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<Guid> CreateAuctionAsync(Auction auction)
    {
        try
        {
            if (auction.AuctionId == Guid.Empty)
            {
                auction.AuctionId = Guid.NewGuid();
            }
            
            await _collection.InsertOneAsync(auction);
            _logger.LogInformation($"Created auction with ID {auction.AuctionId}");
            return auction.AuctionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create auction: {ex.Message}");
            return Guid.Empty;
        }
    }

    public async Task<bool> UpdateAuctionAsync(Auction auction)
    {
        var filter = Builders<Auction>.Filter.Eq(x => x.AuctionId, auction.AuctionId);

        try
        {
            var result = await _collection.ReplaceOneAsync(filter, auction);
            _logger.LogInformation($"Updated auction with ID {auction.AuctionId}. Modified: {result.ModifiedCount}");
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to update auction with ID {auction.AuctionId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAuctionAsync(Guid id)
    {
        var filter = Builders<Auction>.Filter.Eq(x => x.AuctionId, id);

        try
        {
            var result = await _collection.DeleteOneAsync(filter);
            _logger.LogInformation($"Deleted auction with ID {id}. Deleted: {result.DeletedCount}");
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to delete auction with ID {id}: {ex.Message}");
            return false;
        }
    }
}