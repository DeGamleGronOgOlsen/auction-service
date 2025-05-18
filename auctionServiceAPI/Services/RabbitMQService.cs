using System.Text;
using System.Text.Json;
using auctionServiceAPI.Model;
using auctionServiceAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace auctionServiceAPI.Services
{
    public class BidConsumerService : BackgroundService
    {
        private readonly ILogger<BidConsumerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string QueueName = "bid-placed";

        public BidConsumerService(
            ILogger<BidConsumerService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };
            
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation($"Received bid message: {message}");
                    
                    var bid = JsonSerializer.Deserialize<Bid>(message);
                    if (bid != null)
                    {
                        await ProcessBid(bid);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bid message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            
            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            
            return Task.CompletedTask;
        }

        private async Task ProcessBid(Bid bid)
        {
            // Create a new scope for each bid processing
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                // Get the scoped service within this scope
                var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
                
                var auction = await auctionService.GetAuctionAsync(bid.AuctionId);
                if (auction == null)
                {
                    _logger.LogWarning($"Auction {bid.AuctionId} not found for bid");
                    return;
                }
                
                if (bid.BidId == Guid.Empty)
                {
                    bid.BidId = Guid.NewGuid();
                }
                
                bid.Timestamp = DateTime.UtcNow;
                
                // Check if bid is valid
                if (auction.Bids.Count > 0 && bid.Amount <= auction.Bids.Max(b => b.Amount))
                {
                    _logger.LogWarning($"Bid rejected: not higher than current highest bid");
                    return;
                }
                
                if (bid.Amount < auction.StartingPrice)
                {
                    _logger.LogWarning($"Bid rejected: lower than starting price");
                    return;
                }
                
                auction.Bids.Add(bid);
                await auctionService.UpdateAuctionAsync(auction);
                
                _logger.LogInformation($"Successfully processed bid {bid.BidId} for auction {bid.AuctionId}");
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}