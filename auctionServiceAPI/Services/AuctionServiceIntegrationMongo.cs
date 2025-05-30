using auctionServiceAPI.Model;
using Mongo2Go;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Bson;

namespace auctionServiceAPI.Services
{
    public class AuctionServiceIntegrationMongo : IDisposable
    {
        private readonly MongoDbRunner _runner;
        private readonly IMongoCollection<Auction> _auctionCollection;

        static AuctionServiceIntegrationMongo()
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }

        public AuctionServiceIntegrationMongo()
        {
            _runner = MongoDbRunner.Start();
            var client = new MongoClient(_runner.ConnectionString);
            var database = client.GetDatabase("AuctionServiceDB");
            _auctionCollection = database.GetCollection<Auction>("Auctions");
        }

        public Auction CreateAuction(Auction auction)
        {
            if (auction.AuctionId == Guid.Empty)
                auction.AuctionId = Guid.NewGuid();

            _auctionCollection.InsertOne(auction);
            return auction;
        }

        public Auction? GetAuction(Guid id)
        {
            return _auctionCollection.Find(a => a.AuctionId == id).FirstOrDefault();
        }

        public IEnumerable<Auction> GetAllAuctions()
        {
            return _auctionCollection.Find(_ => true).ToList();
        }

        public void AddBid(Guid auctionId, Bid bid)
        {
            var update = Builders<Auction>.Update.Push(a => a.Bids, bid);
            _auctionCollection.UpdateOne(a => a.AuctionId == auctionId, update);
        }

        public void DeleteAuction(Guid id)
        {
            _auctionCollection.DeleteOne(a => a.AuctionId == id);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}