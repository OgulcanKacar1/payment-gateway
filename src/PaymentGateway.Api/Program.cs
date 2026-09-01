using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Messaging;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ",
            context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

        var response = PaymentGateway.Api.Common.ApiResponse<object>.Fail(message);
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var config = StackExchange.Redis.ConfigurationOptions.Parse(
        builder.Configuration.GetConnectionString("Redis")!);
    config.AbortOnConnectFail = false;   // Redis'e ulaşılamazsa uygulama başlangıçta patlamasın
    return StackExchange.Redis.ConnectionMultiplexer.Connect(config);
});
builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
builder.Services.AddSingleton<IWebhookPublisher, WebhookPublisher>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();   // Render'ın proxy'si varsayılan listede değil → header'a güven
    options.KnownProxies.Clear();
});
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();

builder.Services.AddHttpClient<IWebhookSender, WebhookSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHostedService<WebhookDeliveryService>();
builder.Services.AddHostedService<WebhookConsumer>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // /scalar → OpenAPI JSON'ı şık arayüze çevirir
}

app.UseForwardedHeaders();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
