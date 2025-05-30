using auctionServiceAPI.Model;
using auctionServiceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;


namespace auctionServiceAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuctionController : ControllerBase
    {
        private readonly ILogger<AuctionController> _logger;
        private readonly IAuctionService _dbService;
        private readonly IConfiguration _configuration;
        private readonly string _serviceIp;

        public AuctionController(ILogger<AuctionController> logger, IAuctionService dbService, IConfiguration configuration)
        {
            _logger = logger;
            _dbService = dbService;
            _configuration = configuration;

            // Get and log the service IP address
            var hostName = System.Net.Dns.GetHostName();
            var ips = System.Net.Dns.GetHostAddresses(hostName);
            _serviceIp = ips.First().MapToIPv4().ToString();
            _logger.LogInformation(1, $"Auction Service responding from {_serviceIp}");
        }
        
        [AllowAnonymous]
        [HttpGet("GetAllAuctions")]
        public async Task<ActionResult<IEnumerable<Auction>>> GetAllAuctions()
        {
            _logger.LogInformation($"Getting all auctions from {_serviceIp}");
            var auctions = await _dbService.GetAllAuctionsAsync();
            return Ok(auctions);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Auction>> GetAuction(Guid id)
        {
            _logger.LogInformation($"Getting auction {id} from {_serviceIp}");
            var auction = await _dbService.GetAuctionAsync(id);
            
            if (auction == null)
            {
                _logger.LogWarning($"Auction {id} not found");
                return NotFound();
            }
            
            return Ok(auction);
        }
        [AllowAnonymous]
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Auction>>> GetAuctionsByCategory(AuctionCategory category)
        {
            _logger.LogInformation($"Getting auctions with category {category} from {_serviceIp}");
    
            try
            {
                var auctions = await _dbService.GetAuctionsByCategoryAsync(category);
        
                if (auctions == null || !auctions.Any())
                {
                    return NotFound($"No auctions found in category: {category}");
                }
        
                return Ok(auctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving auctions with category {category}");
                return StatusCode(500, "An error occurred while retrieving auctions by category.");
            }
        }
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<Auction>> CreateAuction([FromForm] Auction auction, IFormFile? imageFile)
        {
            _logger.LogInformation($"Creating new auction from {_serviceIp}");

            try
            {
                // Validate auction data
                if (string.IsNullOrWhiteSpace(auction.AuctionTitle) || auction.StartDate == default ||
                    auction.EndDate == default)
                {
                    return BadRequest("Auction title, start date, and end date are required.");
                }

                if (auction.StartDate >= auction.EndDate)
                {
                    return BadRequest("Start date must be earlier than end date.");
                }

                if (auction.AuctionId == Guid.Empty)
                {
                    auction.AuctionId = Guid.NewGuid();
                }

                // Handle image upload if a file is provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        string imagePath = _configuration["ImagePath"] ?? "/srv/resources/images";
                        Directory.CreateDirectory(imagePath);

                        string fileName = $"{auction.AuctionId}{Path.GetExtension(imageFile.FileName)}";
                        string filePath = Path.Combine(imagePath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        auction.Image = $"/images/{fileName}";
                        _logger.LogInformation($"Image uploaded for auction {auction.AuctionId}: {auction.Image}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image.");
                        return StatusCode(500, "An error occurred while uploading the image.");
                    }
                }

                await _dbService.CreateAuctionAsync(auction);
                return CreatedAtAction(nameof(GetAuction), new { id = auction.AuctionId }, auction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auction.");
                return StatusCode(500, "An error occurred while creating the auction.");
            }
        }
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuction(Guid id, Auction auction)
        {
            _logger.LogInformation($"Updating auction {id} from {_serviceIp}");
            
            if (id != auction.AuctionId)
            {
                return BadRequest();
            }
            
            var existingAuction = await _dbService.GetAuctionAsync(id);
            if (existingAuction == null)
            {
                return NotFound();
            }
            
            await _dbService.UpdateAuctionAsync(auction);
            return NoContent();
        }
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(Guid id)
        {
            _logger.LogInformation($"Deleting auction {id} from {_serviceIp}");
            
            var auction = await _dbService.GetAuctionAsync(id);
            if (auction == null)
            {
                return NotFound();
            }
            
            await _dbService.DeleteAuctionAsync(id);
            return NoContent();
        }
        [Authorize(Roles = "admin, customer")]
        [HttpPost("{id}/bid")]
        public async Task<IActionResult> AddBid(Guid id, Bid bid)
        {
            _logger.LogInformation($"Adding bid to auction {id} from {_serviceIp}");
            
            var auction = await _dbService.GetAuctionAsync(id);
            if (auction == null)
            {
                return NotFound();
            }
            
            if (bid.BidId == Guid.Empty)
            {
                bid.BidId = Guid.NewGuid();
            }
            
            bid.AuctionId = id;
            bid.Timestamp = DateTime.UtcNow;
            
            // Check if bid is valid (above minimum price and highest current bid)
            if (auction.Bids.Count > 0 && bid.Amount <= auction.Bids.Max(b => b.Amount))
            {
                return BadRequest("Bid must be higher than current highest bid");
            }
            
            if (bid.Amount < auction.StartingPrice)
            {
                return BadRequest("Bid must be at least the starting price");
            }
            
            auction.Bids.Add(bid);
            await _dbService.UpdateAuctionAsync(auction);
            
            return Ok(bid);
        }
    }
}