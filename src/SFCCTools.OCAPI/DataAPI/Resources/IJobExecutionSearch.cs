using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.DataAPI.Types;
using SFCCTools.OCAPI.SharedTypes;

namespace SFCCTools.OCAPI.DataAPI.Resources
{
    public static class JobExecutionSearchExtensions
    {
        public static async Task<SearchResult<JobExecution>> SearchJobsBetween(this IJobExecutionSearch service, DateTime start, DateTime end)
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
                                Field = "start_time",
                                // OCAPI BUG: Ocapi is really picky about the format for THIS resource (normal "o" iso8601
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
                    new Sort() {Field = "start_time", SortOrder = SortOrder.Asc}
                }
            };
            var result = await service.Search(request);
            return new SearchResult<JobExecution>(request, result, service.Search);
        }
    }
    public interface IJobExecutionSearch
    {
        [Post("/job_execution_search")]
        Task<RawSearchResult<JobExecution>> Search([Body] SearchRequest searchRequest);
    }
}