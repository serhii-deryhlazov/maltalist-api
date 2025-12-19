using MaltalistApi.Interfaces;
using Stripe;

namespace MaltalistApi.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "eur")
    {
        try
        {
            // Configure Stripe API key
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            
            // Create payment intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };
            
            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Created payment intent {PaymentIntentId} for amount {Amount} {Currency}", 
                paymentIntent.Id, amount, currency);

            return paymentIntent.ClientSecret!;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating payment intent for amount {Amount}", amount);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for amount {Amount}", amount);
            throw;
        }
    }

    public async Task<bool> VerifyPaymentIntentAsync(string paymentIntentId, decimal expectedAmount)
    {
        try
        {
            // Configure Stripe API key
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            
            // Get payment intent from Stripe
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            // Verify payment status is succeeded
            if (paymentIntent.Status != "succeeded")
            {
                _logger.LogWarning("Payment intent {PaymentIntentId} has status {Status}", 
                    paymentIntentId, paymentIntent.Status);
                return false;
            }

            // Verify amount matches expected (Stripe uses smallest currency unit - cents)
            var expectedAmountInCents = (long)(expectedAmount * 100);
            if (paymentIntent.Amount != expectedAmountInCents)
            {
                _logger.LogWarning("Payment intent {PaymentIntentId} amount mismatch. Expected: {Expected}, Got: {Actual}", 
                    paymentIntentId, expectedAmount, paymentIntent.Amount / 100m);
                return false;
            }

            _logger.LogInformation("Payment intent {PaymentIntentId} verified successfully for amount {Amount}", 
                paymentIntentId, expectedAmount);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error verifying payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }
}
