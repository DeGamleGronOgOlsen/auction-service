using System.Text;
using auctionServiceAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NLog;
using NLog.Web;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

// Keep the logger setup outside the try block
var nlogLogger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
nlogLogger.Debug("start min service");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
logger.LogInformation("AuctionService: Attempting to configure Vault...");

// Get Vault connection details from environment variables (set by docker-compose)
string vaultAddress = builder.Configuration["Vault:Address"] ?? "https://vaulthost:8201";
string vaultToken = builder.Configuration["Vault:Token"];

if (string.IsNullOrEmpty(vaultToken))
{
    logger.LogError("AuctionService: Vault:Token is NOT configured in environment variables. Cannot authenticate with Vault. Ensure Vault__Token is set in docker-compose.yml.");
    throw new InvalidOperationException("Vault token is not configured. Application cannot start.");
}
logger.LogInformation($"AuctionService: Using Vault Address: {vaultAddress}");
logger.LogInformation("AuctionService: Using Vault Token (length): {VaultTokenLength}", vaultToken.Length);


var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback =
    (message, cert, chain, sslPolicyErrors) =>
    {
        logger.LogWarning("AuctionService: Bypassing Vault SSL certificate validation. [Development ONLY]");
        return true;
    };

IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultToken);
var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod)
{
    Namespace = "",
    MyHttpClientProviderFunc = handler => new HttpClient(httpClientHandler) { BaseAddress = new Uri(vaultAddress) }
};
IVaultClient vaultClient = new VaultClient(vaultClientSettings);

try
{
    logger.LogInformation("AuctionService: Fetching JWT parameters from Vault path 'Secrets'...");
    Secret<SecretData> jwtParamsSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
        path: "Secrets",
        mountPoint: "secret"
    );
    string? jwtSecretKey = jwtParamsSecret.Data.Data["Secret"]?.ToString();
    string? jwtIssuer = jwtParamsSecret.Data.Data["Issuer"]?.ToString();
    string? jwtAudience = jwtParamsSecret.Data.Data["Audience"]?.ToString();

    if (string.IsNullOrEmpty(jwtSecretKey))
        throw new InvalidOperationException("JWT Secret not found in Vault at secret/Secrets.");
    if (string.IsNullOrEmpty(jwtIssuer))
        throw new InvalidOperationException("JWT Issuer not found in Vault at secret/Secrets.");
    if (string.IsNullOrEmpty(jwtAudience))
        throw new InvalidOperationException("JWT Audience not found in Vault at secret/Secrets.");

    builder.Configuration["JwtSettings:Secret"] = jwtSecretKey;
    builder.Configuration["JwtSettings:Issuer"] = jwtIssuer;
    builder.Configuration["JwtSettings:Audience"] = jwtAudience;
    logger.LogInformation("AuctionService: JWT parameters loaded from Vault.");

    logger.LogInformation("AuctionService: Fetching Connection parameters from Vault path 'Connections'...");
    Secret<SecretData> connectionParamsSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
        path: "Connections",
        mountPoint: "secret"
    );
    string? mongoConnectionString = connectionParamsSecret.Data.Data["mongoConnectionString"]?.ToString();
    string? mongoDbName = connectionParamsSecret.Data.Data["MongoDbDatabaseName"]?.ToString();
    string? authServiceUrl = connectionParamsSecret.Data.Data["AuthServiceUrl"]?.ToString();

    if (string.IsNullOrEmpty(mongoConnectionString))
        throw new InvalidOperationException("mongoConnectionString not found in Vault at secret/Connections.");
    if (string.IsNullOrEmpty(mongoDbName))
        throw new InvalidOperationException("MongoDbDatabaseName not found in Vault at secret/Connections.");
    if (string.IsNullOrEmpty(authServiceUrl))
        throw new InvalidOperationException("AuthServiceUrl not found in Vault at secret/Connections.");

    builder.Configuration["MongoDb:ConnectionString"] = mongoConnectionString;
    builder.Configuration["MongoDb:DatabaseName"] = mongoDbName;
    builder.Configuration["AuthServiceUrl"] = authServiceUrl;
    logger.LogInformation("AuctionService: Connection parameters (MongoDB, AuthServiceUrl) loaded from Vault.");

}
catch (Exception ex)
{
    logger.LogCritical(ex,
        "AuctionService: CRITICAL ERROR fetching secrets from Vault. Application cannot start properly.");
    throw;
}

// Configure JWT Authentication (UserService validates tokens issued by AuthService)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])) // Read from config
        };
    });
logger.LogInformation("AuctionService: JWT Authentication services configured.");

builder.Services.AddAuthorization();

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

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    nlogLogger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}