using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace auctionServiceAPI.Model
{
    public enum AuctionCategory
    {
        Møbler,
        Porcelæn,
        Smykker,
        Kunst,
        Sølvtøj,
        Antikviteter
    }

    public enum AuctionStatus
    {
        OnGoing,
        Finished,
        Cancelled
    }

    public class Auction
    {
        [BsonId]
        public Guid AuctionId { get; set; }
        public string AuctionTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string? Image { get; set; }
        public AuctionCategory Category { get; set; }
        public AuctionStatus AuctionStatus { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal StartingPrice { get; set; }
        public List<Bid> Bids { get; set; } = new List<Bid>();
        public Guid EffectId { get; set; }
        public Guid UserId { get; set; }
        public Guid AppraisalId { get; set; }
    }
}