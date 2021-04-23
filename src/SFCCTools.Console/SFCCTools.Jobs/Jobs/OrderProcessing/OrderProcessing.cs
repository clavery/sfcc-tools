using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Configuration;
using SFCCTools.Jobs;
using SFCCTools.OCAPI.ShopAPI.Resources;
using SFCCTools.OCAPI.ShopAPI.Types;
using Order = SFCCTools.Jobs.Order;

namespace SFCCTools.Jobs
{
    public class OrderProcessing : IJob
    {
        private readonly ILogger<OrderProcessing> _logger;
        private IServiceScopeFactory _scopeFactory;
        private OrderProcessingConfig _initialConfig;
        private SFCCEnvironment _environment;
        private IOrderSearch _searchClient;

        public OrderProcessing(ILogger<OrderProcessing> logger, IServiceScopeFactory scopeFactory,
            IOptions<OrderProcessingConfig> configOpts, IOptions<SFCCEnvironment> envOpts,
            IOrderSearch searchClient)
        {
            _searchClient = searchClient;
            _environment = envOpts.Value;
            _initialConfig = configOpts.Value;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<BIDatabaseContext>();
                var lastRunConfig = await dbContext.RuntimeConfigs.FindAsync("OrderProcessingLastRun");
                var lastRunDate = _initialConfig.InitialReferenceDate;
                if (lastRunConfig != null)
                {
                    lastRunDate = lastRunConfig.DateConfig;
                }
                else
                {
                    lastRunConfig = new RuntimeConfig
                    {
                        RuntimeConfigId = "OrderProcessingLastRun", DateConfig = _initialConfig.InitialReferenceDate
                    };
                    _logger.LogInformation("Starting from default start date {StartDate}",
                        _initialConfig.InitialReferenceDate);
                    dbContext.RuntimeConfigs.Add(lastRunConfig);
                }

                // subtract 10 minute jitter
                var from = lastRunDate.Subtract(TimeSpan.FromMinutes(10));
                var to = from.Add(_initialConfig.MaximumTimeSpan);
                _logger.LogInformation("Searching orders from {From} to {To}", from, to);
                var orders =
                    await _searchClient.SearchOrdersBetween(from, to);

                List<SFCCTools.Jobs.Order> orderEntities = new List<Order>();

                await foreach (var searchHit in orders)
                {
                    var order = searchHit.Data;
                    try
                    {
                        orderEntities.Add(new Order()
                        {
                            OrderId = order.OrderNo,
                            CustomerNo = order.CustomerInfo.CustomerNo,
                            Status = order.Status,
                            CreationDate = order.CreationDate,

                            ProductTotal = order.ProductTotal,
                            TaxTotal = order.TaxTotal,
                            OrderTotal = order.OrderTotal,
                            ShippingTotal = order.ShippingTotal,

                            ShippingMethod = order.Shipments.Count > 0 ? order.Shipments[0].ShippingMethod.Id : null,

                            BillingCountryCode = order.BillingAddress.CountryCode,
                            BillingStateCode = order.BillingAddress.StateCode,
                            ShippingCountryCode = order.Shipments.Count > 0
                                ? order.Shipments[0].ShippingAddress.CountryCode
                                : null,
                            ShippingStateCode = order.Shipments.Count > 0
                                ? order.Shipments[0].ShippingAddress.StateCode
                                : null,

                            Products = order.ProductItems.Select<ProductItem, ProductLineItem>((pi, i) =>
                            {
                                return new ProductLineItem()
                                {
                                    Index = i,
                                    OrderId = order.OrderNo,
                                    ProductId = pi.ProductId
                                };
                            }).ToList(),

                            PaymentMethods = order.PaymentInstruments.Select<PaymentInstrument, PaymentMethod>(
                                (pi, i) =>
                                {
                                    return new PaymentMethod()
                                    {
                                        Index = i,
                                        OrderId = order.OrderNo,
                                        Method = pi.PaymentMethodId
                                    };
                                }).ToList(),
                        });
                    }
                    catch (System.ArgumentNullException e)
                    {
                        _logger.LogError(e, "Invalid order {OrderNo} received, skipping", order.OrderNo);
                    }
                }

                _logger.LogInformation("Saving {NumOrders} orders to database", orderEntities.Count);
                foreach (var orderEntity in orderEntities)
                {
                    var found = await dbContext.Orders.FindAsync(orderEntity.OrderId);
                    if (found == null)
                    {
                        dbContext.Orders.Add(orderEntity);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                
                var mostRecentOrder = await dbContext.Orders.OrderByDescending(o => o.CreationDate).FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (mostRecentOrder != null && mostRecentOrder.CreationDate <= to)
                {
                    lastRunConfig.DateConfig = mostRecentOrder.CreationDate;
                }
                else
                {
                    lastRunConfig.DateConfig = to;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}