using Microsoft.AspNetCore.Mvc.RazorPages;
using auctionServiceAPI.Model;
using System.Net.Http.Json;

namespace MyApp.Namespace
{
    public class AuctionListModel : PageModel
    {
        private readonly IHttpClientFactory? _clientFactory = null;
        public List<Auction>? Auctions { get; set; }

        public AuctionListModel(IHttpClientFactory clientFactory)
            => _clientFactory = clientFactory;

        public void OnGet()
        {
            using HttpClient? client = _clientFactory?.CreateClient("gateway");
            try
            {
                Auctions = client?.GetFromJsonAsync<List<Auction>>(
                    "auction/GetAllAuctions").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}