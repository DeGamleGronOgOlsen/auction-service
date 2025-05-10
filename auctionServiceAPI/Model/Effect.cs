using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace auctionServiceAPI.Model;

public class Effect
{
    
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid EffectId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public decimal MinimumPrice { get; set; }
    public string Picture { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid BidId { get; set; }
    public Guid AppraisalId { get; set; }
    public decimal StartingPrice { get; set; }
}