using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Product = Core.Entities.Product;

namespace Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IBasketRepository basketRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IConfiguration config; 
        private readonly UserManager<AppUser> userManager;
        public PaymentService(UserManager<AppUser> userManager,IBasketRepository basketRepository,IUnitOfWork unitOfWork,IConfiguration config)
        {
            this.basketRepository = basketRepository;
            this.unitOfWork = unitOfWork;
            this.config = config;
            this.userManager = userManager;
        }

        public async Task<CustomerBasket> CreateOrUpdatePaymentIntent(string basketId, AppUser user)
        {
            StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];

            var basket = await basketRepository.GetBasketAsync(basketId);

            var shippingPrice = 0m;
            if (basket == null) return null;
            if(basket.DeliveryMethodId.HasValue)
            {
                var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetByIdAsync((int)basket.DeliveryMethodId);
                shippingPrice = deliveryMethod.Price;
            }
            foreach(var item in basket.Items)
            {
                var productItem = await unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                if(item.Price!=productItem.Price)
                {
                    item.Price = productItem.Price;
                }
            }

            var service = new PaymentIntentService();

            PaymentIntent intent;
           

            if (string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {

                    Amount = (long)basket.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
                    Currency="inr",
                    PaymentMethodTypes = new List<string> { "card" },
                     Shipping = new ChargeShippingOptions
                     {
                       
                          Name=user.Address.FirstName+" "+ user.Address.LastName,
                         Address = new AddressOptions
                         {
                            
                             PostalCode = "98140",
                             City = user.Address.City,
                             State = user.Address.State,
                             Country = "US",
                         },
                     },
                    Description = "Software development services",

                };
                intent = await service.CreateAsync(options);
                basket.PaymentIntentId = intent.Id;
                basket.ClientSecret = intent.ClientSecret;
            }

            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)basket.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100
                };
                await service.UpdateAsync(basket.PaymentIntentId, options);
            }

            await basketRepository.UpdateBasketAsync(basket);

            return basket;



        }

        public async Task<Order> UpdateOrderPaymentFailed(string paymentIntentId)
        {
            var spec = new OrderByPaymentIntentIdSpecification(paymentIntentId);
            var order = await unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
            if (order == null) return null;
            order.Status = OrderStatus.PaymentFailed;
            await unitOfWork.Complete();
            return order;
        }
        
        public async  Task<Order> UpdateOrderPaymentSucceeded(string paymentIntentId)
        {
         
            var spec = new OrderByPaymentIntentIdSpecification(paymentIntentId);
            var order = await unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
            if (order == null) return null;
            order.Status = OrderStatus.PaymentReceived;
            unitOfWork.Repository<Order>().Update(order);
            await unitOfWork.Complete();
            return null;

        }
    }
}
