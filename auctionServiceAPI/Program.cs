using auctionServiceAPI.Services;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSingleton <MongoDBContext>();
builder.Services.AddScoped<IAuctionService, AuctionMongoDBService>();

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
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
