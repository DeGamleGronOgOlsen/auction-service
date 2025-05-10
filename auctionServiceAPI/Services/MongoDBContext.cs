using auctionServiceAPI.Model;
using auctionServiceAPI.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using auctionServiceAPI.Model;

namespace auctionServiceAPI.Services;

/// <summary>
/// MongoDB database context class.
/// </summary>
public class MongoDBContext
{
    public IMongoDatabase Database { get; set; }
    public IMongoCollection<Auction> Collection { get; set; }

    /// <summary>
    /// Create an instance of the context class.
    /// </summary>
    /// <param name="logger">Global logging facility.</param>
    /// <param name="config">System configuration instance.</param>
    public MongoDBContext(ILogger<AuctionMongoDBService> logger, IConfiguration config)
    {        
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var client = new MongoClient(config["MongoConnectionString"]);
        Database = client.GetDatabase(config["AuctionDatabase"]);
        Collection = Database.GetCollection<Auction>(config["AuctionCollection"]);

        logger.LogInformation($"Connected to database {config["AuctionDatabase"]}");
        logger.LogInformation($"Using collection {config["AuctionCollection"]}");
    }

}