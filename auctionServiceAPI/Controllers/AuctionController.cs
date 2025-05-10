using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using auctionServiceAPI.Model;
using auctionServiceAPI.Services;

namespace auctionServiceAPI.Controllers;

/// <summary>
/// The AuctionController implements the HTTP interface for accessing
/// auctions and their related effects.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger;
    private readonly IAuctionService _dbService;

    /// <summary>
    /// Create an instance of the Auction controller.
    /// </summary>
    /// <param name="logger">Global logging instance</param>
    /// <param name="dbService">Database repository</param>
    public AuctionController(ILogger<AuctionController> logger, IAuctionService dbService)
    {
        _logger = logger;
        _dbService = dbService;
    }

    /// <summary>
    /// Service version endpoint.
    /// Fetches metadata information, through reflection from the service assembly.
    /// </summary>
    /// <returns>All metadata attributes from assembly in text string</returns>
    [HttpGet("version")]
    public Dictionary<string, string> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;

        properties.Add("service", "Auction");
        var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion ?? "Undefined";
        properties.Add("version", ver);

        var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
        var localIPAddr = feature?.LocalIpAddress?.ToString() ?? "N/A";
        properties.Add("local-host-address", localIPAddr);

        return properties;
    }
    
    [HttpGet("GetAllAuctions")]
    public async Task<IEnumerable<Auction>> GetAllAuctions()
    {
        _logger.LogInformation("Request for all auctions");
        return await _dbService.GetAllAuctions();
    }

    [HttpGet("GetAuctionById")]
    public async Task<Auction?> GetAuction(Guid auctionId)
    {
        _logger.LogInformation($"Request for auction with guid: {auctionId}");
        return await _dbService.GetAuction(auctionId);
    }

    [HttpGet("GetAuctionsByCategory")]
    public async Task<IEnumerable<Auction>?> GetAuctionsByCategory(string category)
    {
        _logger.LogInformation($"Request for auctions in category: {category}");
        return await _dbService.GetAuctionsByCategory(category);
    }

    [HttpPost("CreateAuction")]
    public Task<Guid?> CreateAuction(Auction auction)
    {
        return _dbService.AddAuction(auction);
    }

    [HttpPost("AddEffectToAuction")]
    public async Task<IActionResult> AddEffectToAuction(Guid auctionId, Effect effect)
    {
        try
        {
            var modifiedCount = await _dbService.AddEffectToAuction(auctionId, effect);
            if (modifiedCount > 0)
            {
                return Ok("Effect added to auction successfully.");
            }
            return NotFound("Auction not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding effect to auction: {Error}", ex.Message);
            return StatusCode(500, "Internal server error.");
        }
    }
}