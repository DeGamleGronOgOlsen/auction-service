using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using auctionServiceAPI.Model;
using System.Net.Http.Json;

namespace auctionServiceAPI.Pages.ShowEffects
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

        public Auction Auction { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                using HttpClient client = _clientFactory.CreateClient("gateway");
                // Make sure the URL is properly formatted with a leading slash if needed
                Auction = await client.GetFromJsonAsync<Auction>($"auction/GetAuctionById?auctionId={id}");

                if (Auction == null)
                {
                    HasError = true;
                    ErrorMessage = "Kunne ikke finde auktionen";
                    return Page();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af auktion");
                HasError = true;
                ErrorMessage = "Der opstod en fejl ved hentning af auktionsdata";
                return Page();
            }
        }
    }
}