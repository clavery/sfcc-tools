using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Refit;
using SFCCTools.OCAPI.SharedTypes;
using SFCCTools.OCAPI.ShopAPI.Types;

namespace SFCCTools.OCAPI.ShopAPI.Resources
{
    public static class OrderSearchExtensions
    {
        public static async Task<Order> SearchOrderByOrderNo(this IOrderSearch service, string orderNo)
        {
            var result = await service.SearchOrders(new SearchRequest()
            {
                Query = new TermQuery()
                {
                    QueryBody = new TermQueryBody()
                    {
                        Fields = new List<string>() {"order_no"},
                        Operator = Operator.Is,
                        Values = new List<string>() {orderNo}
                    }
                },
                Count = 1
            });

            return result.Count > 0 ? result.Hits[0].Data : null;
        }

        public static async Task<SearchResult<OrderSearchHit>> SearchOrdersBetween(this IOrderSearch service, DateTime start,
            DateTime end)
        {
            var request = new SearchRequest
            {
                Query = new FilteredQuery()
                {
                    Body =
                    {
                        Query = new MatchAllQuery(),
                        Filter = new RangeFilter()
                        {
                            RangeFilterBody = new RangeFilterBody()
                            {
                                Field = "creation_date",
                                // OCAPI BUG: Ocapi is really picky about the format for the jobs resource (normal "o" iso8601
                                // conversion works fine for order search but causes a 400 for job search), so we explicitly
                                // convert to UTC and use the Z constant
                                From = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                To = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                            }
                        }
                    }
                },
                Select = "(**)",
                Sorts = new List<Sort>()
                {
                    new Sort() {Field = "creation_date", SortOrder = SortOrder.Asc}
                }
            };
            var result = await service.SearchOrders(request);
            return new SearchResult<OrderSearchHit>(request, result, service.SearchOrders);
        }
    }


    public interface IOrderSearch
    {
        [Post("/order_search")]
        Task<RawSearchResult<OrderSearchHit>> SearchOrders([Body] SearchRequest request);
    }

}