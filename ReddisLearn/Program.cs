using Microsoft.OpenApi.Models;
using ReddisLearn;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register Redis ConnectionMultiplexer
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

// Register Redis lock service
builder.Services.AddSingleton<IRedLockService>(provider =>
{
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    return new RedLockService(builder.Configuration, loggerFactory);
});

// Register caching service
builder.Services.AddSingleton<ICachingService, CachingService>(provider =>
{
    return new CachingService(builder.Configuration, provider.GetRequiredService<IRedLockService>());
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Redis Caching API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();
// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Redis Caching API V1");
});

app.UseAuthorization();
app.MapControllers();

app.Run();