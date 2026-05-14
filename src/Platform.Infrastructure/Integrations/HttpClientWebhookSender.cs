using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class HttpClientWebhookSender : IWebhookSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientWebhookSender> _logger;

    public HttpClientWebhookSender(HttpClient httpClient, ILogger<HttpClientWebhookSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WebhookSendResult> SendAsync(
        WebhookSubscription subscription,
        WebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Platform-Event-Type", delivery.EventType);
        request.Headers.Add("X-Platform-Event-Id", delivery.EventId.ToString());
        request.Headers.Add("X-Platform-Signature", CreateSignature(delivery.PayloadJson, subscription.Secret));
        request.Content = new StringContent(delivery.PayloadJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken);

            return new WebhookSendResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Webhook delivery {DeliveryId} send failed.", delivery.Id);
            return new WebhookSendResult(false, null, exception.Message);
        }
    }

    private static string CreateSignature(string payloadJson, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(bytes);
    }
}
