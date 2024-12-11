using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Data;
using Stripe;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/payments")]
public class PaymentController
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public PaymentController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> MakePayment()
    {
        #region Create Payment Intent
        
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
        // will need to get it here correctly from the front-end for that specific order, by that user
        decimal totalAmount = 10.50m;
        
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(totalAmount * 100m),
            Currency = "usd",
            //this wasn't in the video, instead there was PaymentMethodTypes with "card" inside, do we need it?
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            //Do we need this methodTypes? It wasn't in the boiler plate code that I copied from stripe docs, but in video it was present
            // since the video is older pehaps we no longer need this PaymentMethodTypes, if the newest version does not have it?
            // PaymentMethodTypes = new List<string>
            // {
            //     "card"
            // }
        };
        var service = new PaymentIntentService();
        var response = service.Create(options);
        
        #endregion
    }
}