using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using StripeEventsCheckout.ApiServer.Models.Config;
using StripeEventsCheckout.ApiServer.Services;

namespace StripeEventsCheckout.ApiServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IStripeClient _stripeClient;
    private readonly IMessageSender _messenger;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOptions<StripeOptions> _stripeConfig;

    public WebhookController(IStripeClient stripeClient, IMessageSender messenger, ILogger<WebhookController> logger, IOptions<StripeOptions> stripeConfig)
    {
        _stripeClient = stripeClient;
        _messenger = messenger;
        _logger = logger;
        _stripeConfig = stripeConfig;
    }

    [HttpPost]
    public async Task<ActionResult> Handler()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload,
                Request.Headers["Stripe-Signature"],
                _stripeConfig.Value.WebhookSecret
            );

            _logger.LogInformation($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");

            // Handle the events
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id}, Status: {checkoutSession.Status}");

                if (checkoutSession.Status == "complete" && checkoutSession.PhoneNumberCollection.Enabled)
                {
                    try
                    {
                        var customerService = new CustomerService(_stripeClient);
                        var customer = await customerService.GetAsync(checkoutSession.CustomerId);

                        var recipient = _messenger switch
                        {
                            TwilioMessageSender => customer.Phone,
                            SendGridMessageSender => customer.Email,
                            _ => throw new ArgumentException("Unsupported Type")
                        };

                        await _messenger.SendMessageAsync("Your order has been successfully processed", recipient);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }

            }

            else if (stripeEvent.Type == Events.CheckoutSessionExpired)
            {
                var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id}");

                // Notify your customer about the cart

            }
            else
            {
                _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest();
        }
    }
}