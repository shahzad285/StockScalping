using StockTrading.Services;
using StockTrading.IServices;
using StockTrading.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStockTradingData(builder.Configuration);

// Add our services
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddHttpClient<AngelOneService>();
builder.Services.AddTransient<IAngelOneService>(serviceProvider =>
    serviceProvider.GetRequiredService<AngelOneService>());
builder.Services.AddTransient<IBrokerService>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var activeBroker = config["Broker:Active"] ?? "AngelOne";

    return activeBroker.Equals("AngelOne", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<AngelOneService>()
        : throw new InvalidOperationException($"Unsupported broker '{activeBroker}'.");
});
builder.Services.AddHostedService<ScalpingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockTradingDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
