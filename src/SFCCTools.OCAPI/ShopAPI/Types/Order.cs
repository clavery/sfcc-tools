using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SFCCTools.OCAPI.ShopAPI.Types
{
    [JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(SnakeCaseNamingStrategy))]
    public enum ExportStatus
    {
        Ready,
        NotExported,
        Failed,
        Exported
    }

    [JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(SnakeCaseNamingStrategy))]
    public enum OrderStatus
    {
        Created,
        New,
        Open,
        Completed,
        Cancelled,
        Replaced,
        Failed
    }
    
    
    public class Order
    {
        public string OrderNo;
        public DateTime CreationDate;
        public string CustomerName;

        public decimal TaxTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal ProductTotal { get; set; }
        public decimal OrderTotal { get; set; }

        public List<ProductItem> ProductItems;
        public List<PaymentInstrument> PaymentInstruments;
        public List<Shipment> Shipments;
        
        public CustomerInfo CustomerInfo;
        
        public ExportStatus ExportStatus;

        public OrderStatus Status;

        public OrderAddress BillingAddress;
        
        [JsonExtensionData] public Dictionary<string, object> Custom { get; set; }
    }

    public class OrderSearchHit
    {
        public double Relevance;
        public Order Data;
    }

    public class Shipment
    {
        public OrderAddress ShippingAddress;
        public ShippingMethod ShippingMethod;
    }

    public class ShippingMethod
    {
        public string Id;
    }

    public class PaymentInstrument
    {
        public string PaymentMethodId;
        [JsonExtensionData] public Dictionary<string, object> Custom { get; set; }
    }

    public class ProductItem
    {
        public string ProductId;
        public int Quantity;
    }

    public class CustomerInfo
    {
        public string CustomerNo;
        public string Email;
    }

    public class OrderAddress
    {
        public string FirstName;
        public string LastName;
        public string Address1;
        public string Address2;
        public string City;
        public string PostalCode;
        public string StateCode;
        public string CountryCode;
        public string Phone;
    }
}