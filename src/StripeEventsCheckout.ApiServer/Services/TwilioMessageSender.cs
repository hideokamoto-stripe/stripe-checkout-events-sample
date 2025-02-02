using Microsoft.Extensions.Options;
using StripeEventsCheckout.ApiServer.Models.Config;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;

namespace StripeEventsCheckout.ApiServer.Services;

public class TwilioMessageSender : IMessageSender
{
    private readonly ITwilioRestClient _twilioRestClient;
    private readonly IOptions<TwilioOptions> _twilioOptions;

    public TwilioMessageSender(ITwilioRestClient twilioRestClient, IOptions<TwilioOptions> twilioOptions)
    {
        _twilioRestClient = twilioRestClient;
        _twilioOptions = twilioOptions;
    }
    public async Task SendMessageAsync(string message, string receiver)
    {
        var sentMessage = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_twilioOptions.Value.PhoneNumber),
            to: new Twilio.Types.PhoneNumber(receiver),
            client: _twilioRestClient
        );
        
    }
}

public interface IMessageSender
{
    Task SendMessageAsync(string message, string receiver);
}