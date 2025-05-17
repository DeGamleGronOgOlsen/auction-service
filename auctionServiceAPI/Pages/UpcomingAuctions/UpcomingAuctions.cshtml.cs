using auctionServiceAPI.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace auctionServiceAPI.Pages.UpcomingAuctions
{
    public class UpcomingAuctionsModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<UpcomingAuctionsModel> _logger;

        public UpcomingAuctionsModel(IHttpClientFactory clientFactory, ILogger<UpcomingAuctionsModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public List<Auction> UpcomingAuctions { get; set; } = new List<Auction>();
        public string SearchTerm { get; set; }
        public string CategoryFilter { get; set; }
        public List<string> Categories { get; set; } = new List<string>() { "Alle kategorier" };
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string searchTerm = "", string category = "")
        {
            SearchTerm = searchTerm;
            CategoryFilter = category;

            try
            {
                using HttpClient client = _clientFactory.CreateClient("gateway");
                UpcomingAuctions = await client.GetFromJsonAsync<List<Auction>>("auction") 
                    ?? new List<Auction>();
                
                // Get unique categories from auctions and add to categories list
                var auctionCategories = UpcomingAuctions
                    .Select(a => a.Category.ToString())
                    .Distinct()
                    .ToList();

                
                
                Categories = new List<string>() { "Alle kategorier" };
                Categories.AddRange(auctionCategories);

                // Apply filters
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    UpcomingAuctions = UpcomingAuctions.FindAll(a =>
                        a.AuctionTitle.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        a.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "Alle kategorier")
                {
                    // Use case-insensitive parsing to handle any casing differences
                    if (Enum.TryParse<AuctionCategory>(CategoryFilter, true, out AuctionCategory parsedCategory))
                    {
                        UpcomingAuctions = UpcomingAuctions.FindAll(a => a.Category == parsedCategory);
                    }
                    else
                    {
                        // Log the failed parsing for debugging
                        _logger.LogWarning($"Failed to parse category: {CategoryFilter}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching auctions");
                HasError = true;
                ErrorMessage = "Der opstod en fejl ved hentning af auktionsdata";
            }
        }
    }
}