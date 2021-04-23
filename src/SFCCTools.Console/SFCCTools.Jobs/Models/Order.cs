using System;
using System.Collections.Generic;
using System.Net;
using SFCCTools.OCAPI.ShopAPI.Types;

namespace SFCCTools.Jobs
{
    public class ProductLineItem
    {
        public int Index { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }
    }

    public class PaymentMethod
    {
        public int Index { get; set; }
        public string OrderId { get; set; }
        public string Method { get; set; }
    }

    public class Order
    {
        public string OrderId { get; set; }

        public string CustomerNo { get; set; }
        public OrderStatus Status { get; set; }

        public DateTime CreationDate { get; set; }

        public IPAddress RemoteHost { get; set; }
        
        public decimal TaxTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal ProductTotal { get; set; }
        public decimal OrderTotal { get; set; }
        
        public string BillingStateCode { get; set; }
        public string ShippingStateCode { get; set; }

        public string BillingCountryCode { get; set; }
        public string ShippingCountryCode { get; set; }
        
        public virtual IList<ProductLineItem> Products { get; set; }
        public virtual IList<PaymentMethod> PaymentMethods { get; set; }
        
        public string ShippingMethod { get; set; }
    }
}