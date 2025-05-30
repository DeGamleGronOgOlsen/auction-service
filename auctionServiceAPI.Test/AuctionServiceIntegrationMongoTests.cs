using auctionServiceAPI.Model;
using auctionServiceAPI.Services;
using NUnit.Framework;

namespace auctionServiceAPI.Test;

[TestFixture]
[Category("MockMongo")]
public class AuctionServiceIntegrationMongoTests
{
    private AuctionServiceIntegrationMongo _service;

    [SetUp]
    public void Setup()
    {
        _service = new AuctionServiceIntegrationMongo();
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public void CreateAuction_ShouldBeRetrievable()
    {
        // Arrange
        var auction = new Auction
        {
            AuctionTitle = "Testauktion",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(3),
            Description = "Beskrivelse",
            Location = "København",
            Category = AuctionCategory.Kunst,
            AuctionStatus = AuctionStatus.OnGoing,
            StartingPrice = 500,
            MinimumPrice = 1000,
            EffectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppraisalId = Guid.NewGuid()
        };

        // Act
        var created = _service.CreateAuction(auction);
        var retrieved = _service.GetAuction(created.AuctionId);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.AuctionTitle, Is.EqualTo("Testauktion"));
    }

    [Test]
    public void AddBid_ShouldAttachBidToAuction()
    {
        // Arrange
        var auction = _service.CreateAuction(new Auction
        {
            AuctionTitle = "Budauktion",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Description = "Med bud",
            Location = "Aarhus",
            Category = AuctionCategory.Møbler,
            AuctionStatus = AuctionStatus.OnGoing,
            StartingPrice = 1000,
            MinimumPrice = 1500,
            EffectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppraisalId = Guid.NewGuid()
        });

        var bid = new Bid
        {
            BidId = Guid.NewGuid(),
            AuctionId = auction.AuctionId,
            UserId = Guid.NewGuid(),
            Amount = 2000,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _service.AddBid(auction.AuctionId, bid);
        var updated = _service.GetAuction(auction.AuctionId);

        // Assert
        Assert.That(updated!.Bids.Count, Is.EqualTo(1));
        Assert.That(updated.Bids[0].Amount, Is.EqualTo(2000));
    }

    [Test]
    public void DeleteAuction_ShouldRemoveIt()
    {
        // Arrange
        var auction = _service.CreateAuction(new Auction
        {
            AuctionTitle = "Slettet auktion",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            Description = "Slet mig",
            Location = "Odense",
            Category = AuctionCategory.Smykker,
            AuctionStatus = AuctionStatus.OnGoing,
            StartingPrice = 300,
            MinimumPrice = 500,
            EffectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppraisalId = Guid.NewGuid()
        });

        // Act
        _service.DeleteAuction(auction.AuctionId);
        var result = _service.GetAuction(auction.AuctionId);

        // Assert
        Assert.That(result, Is.Null);
    }
}
