using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Services;

public class WebhookDeliveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public WebhookDeliveryService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingWebhooksAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingWebhooksAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebhookSender>();

        var now = DateTime.UtcNow;

        // Gönderilmeye hazır (Pending + zamanı gelmiş) event'leri getir
        var pendingEvents = await db.WebhookEvents
            .Where(w => w.Status == WebhookEventStatus.Pending && w.NextAttemptAt <= now)
            .ToListAsync(stoppingToken);

        foreach (var webhookEvent in pendingEvents)
        {
            var merchant = await db.Merchants
                .FirstOrDefaultAsync(m => m.Id == webhookEvent.MerchantId, stoppingToken);

            if (merchant is null)
                continue;

            var success = await sender.SendAsync(merchant, webhookEvent);

            if (success)
            {
                webhookEvent.Status = WebhookEventStatus.Delivered;   // başarılı → teslim edildi
            }
            else
            {
                webhookEvent.AttemptCount++;                          // deneme sayısını artır
                if (webhookEvent.AttemptCount >= 5)
                    webhookEvent.Status = WebhookEventStatus.Failed;  // 5 denemede pes et
                else
                    webhookEvent.NextAttemptAt = now.AddSeconds(Math.Pow(2, webhookEvent.AttemptCount)); // backoff
            }
        }

        await db.SaveChangesAsync(stoppingToken);   // tüm değişiklikleri kaydet
    }
}