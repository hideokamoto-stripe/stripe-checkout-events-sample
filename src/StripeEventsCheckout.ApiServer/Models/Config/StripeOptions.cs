namespace StripeEventsCheckout.ApiServer.Models.Config;
public class StripeOptions
{
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}