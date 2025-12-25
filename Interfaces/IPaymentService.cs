namespace MaltalistApi.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(int listingId, string priceId, string successUrl, string cancelUrl);
    Task<bool> VerifyCheckoutSessionAsync(string sessionId);
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "eur");
    Task<bool> VerifyPaymentIntentAsync(string paymentIntentId, decimal expectedAmount);
}
