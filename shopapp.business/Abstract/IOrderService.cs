using System.Collections.Generic;
using shopapp.entity;

namespace shopapp.business.Abstract
{
    public interface IOrderService
    {
        void Create(Order entity);
        void Update(Order entity);
        List<Order> GetOrders(string userId);
    }
}