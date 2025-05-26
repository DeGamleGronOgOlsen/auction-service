using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using auctionServiceAPI.Controllers;
using auctionServiceAPI.Model;
using auctionServiceAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace auctionServiceAPI.Test
{
    [TestFixture]
    public class AuctionControllerTests
    {
        private AuctionController _controller = null!;
        private Mock<ILogger<AuctionController>> _mockLogger = null!;
        private Mock<IAuctionService> _mockDbService = null!;
        private Mock<IConfiguration> _mockConfig = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<AuctionController>>();
            _mockDbService = new Mock<IAuctionService>();
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(c => c["ImagePath"]).Returns("/tmp/images");

            _controller = new AuctionController(
                _mockLogger.Object,
                _mockDbService.Object,
                _mockConfig.Object
            );
        }

        [Test]
        public async Task GetAllAuctions_ShouldReturnOkWithAuctions()
        {
            // Arrange
            var auctions = new List<Auction> { new Auction { AuctionId = Guid.NewGuid(), AuctionTitle = "Test" } };
            _mockDbService.Setup(s => s.GetAllAuctionsAsync()).ReturnsAsync(auctions);

            // Act
            var result = await _controller.GetAllAuctions();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok!.Value, Is.EqualTo(auctions));
        }

        [Test]
        public async Task GetAuction_ShouldReturnNotFound_WhenAuctionDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockDbService.Setup(s => s.GetAuctionAsync(id)).ReturnsAsync((Auction?)null);

            // Act
            var result = await _controller.GetAuction(id);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result.Result);
        }

        [Test]
        public async Task CreateAuction_ShouldReturnCreated_WhenValidAuction()
        {
            // Arrange
            var auction = new Auction
            {
                AuctionTitle = "Ny auktion",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                AuctionId = Guid.NewGuid()
            };

            _mockDbService
                .Setup(s => s.CreateAuctionAsync(It.IsAny<Auction>()))
                .ReturnsAsync(auction.AuctionId); // Brug ReturnsAsync til Task<Guid>

            // Act
            var result = await _controller.CreateAuction(auction, null);

            // Assert
            Assert.IsInstanceOf<CreatedAtActionResult>(result.Result);

            var createdResult = result.Result as CreatedAtActionResult;
            Assert.NotNull(createdResult);
            Assert.AreEqual(nameof(_controller.GetAuction), createdResult!.ActionName);
            Assert.IsInstanceOf<Auction>(createdResult.Value);
            Assert.AreEqual(auction.AuctionId, ((Auction)createdResult.Value!).AuctionId);
        }


        [Test]
        public async Task CreateAuction_ShouldReturnBadRequest_WhenStartAfterEnd()
        {
            var auction = new Auction
            {
                AuctionTitle = "Fejl i dato",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow
            };

            var result = await _controller.CreateAuction(auction, null);

            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
        }

        [Test]
        public async Task AddBid_ShouldReturnBadRequest_WhenBidIsTooLow()
        {
            var auctionId = Guid.NewGuid();
            var auction = new Auction
            {
                AuctionId = auctionId,
                StartingPrice = 100,
                Bids = new List<Bid>
                {
                    new Bid { Amount = 150 }
                }
            };

            var newBid = new Bid { Amount = 140 };

            _mockDbService.Setup(s => s.GetAuctionAsync(auctionId)).ReturnsAsync(auction);

            var result = await _controller.AddBid(auctionId, newBid);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task DeleteAuction_ShouldReturnNotFound_IfAuctionDoesNotExist()
        {
            var id = Guid.NewGuid();
            _mockDbService.Setup(s => s.GetAuctionAsync(id)).ReturnsAsync((Auction?)null);

            var result = await _controller.DeleteAuction(id);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }
    }
}
