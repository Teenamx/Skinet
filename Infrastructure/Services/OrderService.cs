using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
      
        private readonly IBasketRepository basketRepo;
        private readonly IUnitOfWork unitOfWork;
        private readonly IPaymentService paymentService;

        public OrderService(IBasketRepository basketRepo,IUnitOfWork unitOfWork,IPaymentService paymentService)
        {
            
            this.basketRepo = basketRepo;
            this.unitOfWork = unitOfWork;
            this.paymentService = paymentService;
        }

        public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, Core.Entities.OrderAggregate.Address shippingAddress)
        {
            //get basket from the repo
            var basket = await basketRepo.GetBasketAsync(basketId);

            //get items from the product repo
            var items = new List<OrderItem>();

            foreach(var item in basket.Items)
            {
                var productItem = await unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);

            }

            //get delivery method from repo

            var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);


            //calc subtotal

            var subtotal = items.Sum(item => item.Price * item.Quantity);

            //check to see if order exists
            var spec = new OrderByPaymentIntentIdSpecification(basket.PaymentIntentId);
            var existingOrder = await unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
             
            if(existingOrder!=null)
            {

                unitOfWork.Repository<Order>().Delete(existingOrder);
                AppUser address = new AppUser
                {
                    Address = new Core.Entities.Identity.Address
                    {
                        FirstName = shippingAddress.FirstName,
                        LastName = shippingAddress.LastName,
                        City = shippingAddress.City,
                        State = shippingAddress.State,
                        Street = shippingAddress.Street,
                        pincode = shippingAddress.pincode

                    }
                };
                await paymentService.CreateOrUpdatePaymentIntent(basket.Id,address);
            }

            //create order

            var order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal,basket.PaymentIntentId);
            
            unitOfWork.Repository<Order>().Add(order);

            //  save to db

            var result = await unitOfWork.Complete();

            if (result < 0)
                return null;
            //delete Basket

            //await basketRepo.DeleteBasketAsync(basketId);


            // return order

            return order;




        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await unitOfWork.Repository<DeliveryMethod>().ListAllAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
        {
            var spec= new OrderWithItemsAndOrderingSpecification(id,buyerEmail);
            return await unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            var spec = new OrderWithItemsAndOrderingSpecification(buyerEmail);
            return await unitOfWork.Repository<Order>().ListAsync(spec);
        }
    }
}
