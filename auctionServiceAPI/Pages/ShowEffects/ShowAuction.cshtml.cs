using auctionServiceAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace auctionServiceAPI.Pages.Auctions
{
    public class ShowEffectsModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<ShowEffectsModel> _logger;

        public ShowEffectsModel(IHttpClientFactory clientFactory, ILogger<ShowEffectsModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid AuctionId { get; set; }

        public Auction Auction { get; set; }
        public decimal CurrentBid { get; set; }
        public decimal MinimumBid { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                using HttpClient client = _clientFactory.CreateClient("gateway");
                Auction = await client.GetFromJsonAsync<Auction>($"auction/{AuctionId}");
                
                if (Auction == null)
                {
                    HasError = true;
                    ErrorMessage = "Auktionen kunne ikke findes";
                    return Page();
                }

                // Calculate current bid and minimum next bid
                CurrentBid = Auction.Bids.Count > 0 
                    ? Auction.Bids.Max(b => b.Amount) 
                    : Auction.StartingPrice;
                
                // Set minimum bid to current bid + 100 kr or starting price if no bids
                MinimumBid = CurrentBid + 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching auction details");
                HasError = true;
                ErrorMessage = "Der opstod en fejl ved hentning af auktionsdata";
            }
            
            return Page();
        }
    }
}