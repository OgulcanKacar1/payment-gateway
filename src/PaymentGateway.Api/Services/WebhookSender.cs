using System.Text;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public class WebhookSender : IWebhookSender
{
    private readonly HttpClient _httpClient;
    
    public WebhookSender(HttpClient httpClient) // DI : HttpClient injection
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendAsync(Merchant merchant, WebhookEvent webhookEvent)
    {
        // url veya secret yok ise gönderim yapılmaz
        if (string.IsNullOrWhiteSpace(merchant.WebhookUrl) || string.IsNullOrWhiteSpace(merchant.WebhookSecret))
            return false;
        
        //1. payloadı merchant'ın secret'ı ile imzala
        var signature = HmacHelper.ComputeSignature(webhookEvent.Payload, merchant.WebhookSecret);
        
        //2. Post istegini kurma: gövde = payload, header = signature
        var request = new HttpRequestMessage(HttpMethod.Post, merchant.WebhookUrl)
        {
            Content = new StringContent(webhookEvent.Payload, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("X-Webhook-Signature", signature);
        
        //3. Gönder; ağ hatası olabilir, bu yüzden try-catch ile sarmala

        try
        {
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // ağ hatası, timeout, DNS hatası vs. gibi durumlarda false döndür
        }
    }

    
}