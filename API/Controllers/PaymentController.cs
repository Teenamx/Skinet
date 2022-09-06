using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Errors;
using System.IO;
using Stripe;
using Core.Entities.OrderAggregate;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    public class PaymentController : BaseApiController
    {
        private readonly IPaymentService paymentService;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<PaymentController> logger;
        private const string WhSecret = "";
        public PaymentController(IPaymentService paymentService, UserManager<AppUser> userManager,ILogger<PaymentController> logger)
        {
            this.paymentService = paymentService;
            this.userManager = userManager;
            this.logger = logger;
        }

        [Authorize]
        [HttpPost("{basketId}")]
        public async Task<ActionResult<CustomerBasket>> CreateOrUpdatePaymentIntent(string basketId)
        {

            var user = await userManager.FindUserByClaimsPrincipalWithAddressAsync(User);
            var basket= await paymentService.CreateOrUpdatePaymentIntent(basketId,user);
            if (basket == null)
                return BadRequest(new ApiResponse(400, "Problem with your basket"));
            return basket;

        }

        [HttpPost("webhook")]
        public async Task<ActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], WhSecret);
            PaymentIntent intent;
            Order order;
            switch(stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    intent = (PaymentIntent)stripeEvent.Data.Object;
                    logger.LogInformation("Payment Succeeded:", intent.Id);
                    //Todo :update order with new status
                    break;

                case "payment_intent.payment_failed":
                    intent = (PaymentIntent)stripeEvent.Data.Object;
                    logger.LogInformation("Payment Failed:", intent.Id);
                    //Todo :update order with new status
                    break;
            }

            return new EmptyResult();

        }

    }
}
