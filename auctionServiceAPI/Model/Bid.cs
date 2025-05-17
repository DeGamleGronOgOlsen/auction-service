namespace auctionServiceAPI.Model
{
    public class Bid
    {
        public Guid BidId { get; set; }
        public Guid AuctionId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}