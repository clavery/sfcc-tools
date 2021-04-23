using System;
using System.Collections.Generic;
using SFCCTools.OCAPI.SharedTypes;
using SFCCTools.OCAPI.ShopAPI.Resources;
using Xunit;

namespace SFCCTools.FunctionalTests.OCAPI
{
    public class TestOCAPIConfiguration
    {
        [Fact]
        public void TestRangeFilterConstruction()
        {
            var start = new DateTime();
            var end = new DateTime();
            var request = new SearchRequest
            {
                Query = new FilteredQuery()
                {
                    Body =
                    {
                        Filter = new RangeFilter
                        {
                            RangeFilterBody = new RangeFilterBody()
                            {
                                Field = "creation_date",
                                From = start.ToString("o"),
                                FromInclusive = true
                            }
                        }
                    }
                },
                Select = "(**)"
            };
            
            request = new SearchRequest
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
                                From = start.ToString("o"),
                                To = end.ToString("o")
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

        }
        
        
    }
}