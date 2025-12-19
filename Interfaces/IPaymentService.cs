namespace MaltalistApi.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "eur");
    Task<bool> VerifyPaymentIntentAsync(string paymentIntentId, decimal expectedAmount);
}
