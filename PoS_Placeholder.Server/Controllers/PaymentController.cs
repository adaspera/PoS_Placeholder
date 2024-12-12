using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models.Dto;
using Stripe;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public PaymentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("makepayment")]
    [Authorize]
    public async Task<IActionResult> MakePayment([FromBody] PaymentRequestDto paymentRequestDto)
    {
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
        
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(paymentRequestDto.TotalAmount * 100m),
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };
        var service = new PaymentIntentService();
        var paymentIntentResponse = service.Create(options);

        var paymentResponseDto = new PaymentResponseDto
        {
            ClientSecret = paymentIntentResponse.ClientSecret,
            PaymentIntentId = paymentIntentResponse.Id
        };
        
        return Ok(paymentResponseDto);
    }
}