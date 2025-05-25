using auctionServiceAPI.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NLog;
using NLog.Web;

// Keep the logger setup outside the try block
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("start min service");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();
    builder.Services.AddSingleton<MongoDBContext>();
    builder.Services.AddScoped<IAuctionService, AuctionMongoDBService>();
    builder.Services.AddHostedService<BidConsumerService>();
    
    // Configure CORS policy to allow your frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:8080")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Create globally availabel HttpClient for accesing the gateway.
    var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:4000";
    builder.Services.AddHttpClient("gateway", client =>
    {
        client.BaseAddress = new Uri(gatewayUrl);
        client.DefaultRequestHeaders.Add(
            HeaderNames.Accept, "application/json");
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // CORS middleware must be early in the pipeline
    app.UseCors("AllowFrontend");

    app.UseStaticFiles();

    var imagePath = builder.Configuration["ImagePath"];
    var fileProvider = new PhysicalFileProvider(Path.GetFullPath(imagePath));
    var requestPath = new PathString("/images");
    app.UseStaticFiles(new StaticFileOptions()
    {
        FileProvider = fileProvider,
        RequestPath = requestPath
    });

    // Comment out HTTPS redirect to avoid CORS issues with HTTP
    // app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}