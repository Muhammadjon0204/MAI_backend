using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace MAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StripeController(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["ApiKeys:Stripe"];
        }

        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentRequest request)
        {
            try
            {
                long amount = request.Tier.ToLower() switch
                {
                    "pro" => 29900,
                    "premium" => 59900,
                    _ => throw new ArgumentException("Invalid tier")
                };

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = "rub",
                    Description = $"MAI {request.Tier} Subscription",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return Ok(new { clientSecret = paymentIntent.ClientSecret });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message });
            }
        }
    }

    public class PaymentRequest
    {
        public string Tier { get; set; } = string.Empty;
    }
}