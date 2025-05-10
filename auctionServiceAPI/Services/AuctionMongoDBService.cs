using auctionServiceAPI.Model;
using MongoDB.Bson;
using MongoDB.Driver;

namespace auctionServiceAPI.Services;

/// <summary>
/// Interface definition for the DB service to access auction data.
/// </summary>
public interface IAuctionService
{
    Task<IEnumerable<Auction>?> GetAllAuctions();
    Task<Auction?> GetAuction(Guid auctionId);
    Task<IEnumerable<Auction>?> GetAuctionsByCategory(string category);
    Task<Guid?> AddAuction(Auction auction);
    Task<long> AddEffectToAuction(Guid auctionId, Effect effect);
}

/// <summary>
/// MongoDB repository service for auctions.
/// </summary>
public class AuctionMongoDBService : IAuctionService
{
    private readonly ILogger<AuctionMongoDBService> _logger;
    private readonly IMongoCollection<Auction> _collection;

    /// <summary>
    /// Creates a new instance of the AuctionMongoDBService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dbcontext">The database context to be used for accessing data.</param>
    public AuctionMongoDBService(ILogger<AuctionMongoDBService> logger, MongoDBContext dbcontext)
    {
        _logger = logger;
        _collection = dbcontext.Collection;
    }
   
    public async Task<IEnumerable<Auction>?> GetAllAuctions()
    {
        _logger.LogInformation("Getting all auctions from database");
        var filter = Builders<Auction>.Filter.Empty;

        try
        {
            // Log database and collection names
            _logger.LogInformation($"Database: {_collection.Database.DatabaseNamespace.DatabaseName}");
            _logger.LogInformation($"Collection: {_collection.CollectionNamespace.CollectionName}");
        
            // Count documents before retrieving
            var count = await _collection.CountDocumentsAsync(filter);
            _logger.LogInformation($"Found {count} documents in collection");
        
            var auctions = await _collection.Find(filter).ToListAsync();
            _logger.LogInformation($"Retrieved {auctions.Count} auctions");
            return auctions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve auctions");
            return new List<Auction>(); // Return empty list instead of null
        }
    }

    /// <summary>
    /// Retrieves an auction by its unique ID.
    /// </summary>
    /// <param name="auctionId">The auction's unique ID.</param>
    /// <returns>The requested auction.</returns>
    public async Task<Auction?> GetAuction(Guid auctionId)
    {
        Auction? auction = null;
        var filter = Builders<Auction>.Filter.Eq(x => x.AuctionId, auctionId);

        try
        {
            auction = await _collection.Find(filter).SingleOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return auction;
    }

    /// <summary>
    /// Retrieves auctions by category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>A list of auctions in the specified category.</returns>
    public async Task<IEnumerable<Auction>?> GetAuctionsByCategory(string category)
    {
        IEnumerable<Auction>? auctions = null;

        try
        {
            if (Enum.TryParse(category, out AuctionCategory parsedCategory))
            {
                var filter = Builders<Auction>.Filter.Eq(x => x.Category, parsedCategory);
                auctions = await _collection.Find(filter).ToListAsync();
            }
            else
            {
                _logger.LogWarning($"Invalid category: {category}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return auctions;
    }

    /// <summary>
    /// Adds a new auction to the database.
    /// </summary>
    /// <param name="auction">The auction to add.</param>
    /// <returns>The ID of the added auction.</returns>
    public async Task<Guid?> AddAuction(Auction auction)
    {
        auction.AuctionId = Guid.NewGuid();
        await _collection.InsertOneAsync(auction);
        return auction.AuctionId;
    }

    /// <summary>
    /// Adds an effect to an auction.
    /// </summary>
    /// <param name="auctionId">The ID of the auction to update.</param>
    /// <param name="effect">The effect to add to the auction.</param>
    /// <returns>The number of documents modified.</returns>
    public async Task<long> AddEffectToAuction(Guid auctionId, Effect effect)
    {
        var filter = Builders<Auction>.Filter.Eq(x => x.AuctionId, auctionId);
        var update = Builders<Auction>.Update.Push(x => x.Effects, effect);

        UpdateResult result;
        try
        {
            result = await _collection.UpdateOneAsync(filter, update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return 0;
        }

        return result.ModifiedCount;
    }
}