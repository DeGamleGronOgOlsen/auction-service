
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace auctionServiceAPI.Model;

public enum AuctionCategory
{
    AlleKategorier,
    Møbler,
    Porcelæn,
    Smykker,
    Kunst,
    Sølvtøj,
    Antikviteter
}

public class Auction
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid AuctionId { get; set; }
    public string AuctionTitle { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public int EffectCount { get; set; }
    public string Image { get; set; }
    public AuctionCategory Category { get; set; }
    public List<Effect> Effects { get; set; } = new List<Effect>();
}