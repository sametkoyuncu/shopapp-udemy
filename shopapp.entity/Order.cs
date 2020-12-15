using System;
using System.Collections.Generic;

namespace shopapp.entity
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; } 
        public string City { get; set; }    
        public string Phone { get; set; }
        public string Mail { get; set; }
        public string Note { get; set; }
        public string PaymentId { get; set; }
        public string ConversationId { get; set; }
        public EnumOrderState OrderState { get; set; }
        public EnumPaymentType PaymentType { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }
    public enum EnumOrderState
    {
        waiting=0,
        unpaid=1,
        completed=2
    }

    public enum EnumPaymentType
    {
        CreditCart=0,
        Eft=1
    }
}