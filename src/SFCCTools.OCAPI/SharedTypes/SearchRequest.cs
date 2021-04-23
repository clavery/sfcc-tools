using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SFCCTools.OCAPI.ShopAPI.Resources;

namespace SFCCTools.OCAPI.SharedTypes
{
    public class SearchRequest
    {
        public string Select = "(**)";
        public IQuery Query;
        public List<Sort> Sorts;
        public int Count = 25;
        public int Start { get; set; }
    }
    
    [JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(SnakeCaseNamingStrategy))]
    public enum SortOrder
    {
        Asc,
        Desc
    }

    public class Sort
    {
        public string Field;
        public SortOrder SortOrder;
    }

    public interface IQuery
    {
    }

    public interface IFilter
    {
    }

    public class FilteredQuery : IQuery
    {
        [JsonProperty("filtered_query")] public FilteredQueryBody Body = new FilteredQueryBody();
    }

    public class FilteredQueryBody
    {
        public IQuery Query;
        public IFilter Filter;
    }


    public class MatchAllQuery : IQuery
    {
        // This is always empty
        [JsonProperty("match_all_query")] public readonly MatchAllQueryBody Body = new MatchAllQueryBody();

        public class MatchAllQueryBody
        {
        }
    }

    public class RangeFilter : IFilter
    {
        [JsonProperty("range_filter")] public RangeFilterBody RangeFilterBody;
    }

    public class RangeFilterBody
    {
        public string Field;
        public string From;
        public string To;
        public bool FromInclusive;
        public bool ToInclusive;
    }

    public class TermQuery : IQuery
    {
        [JsonProperty("term_query")] public TermQueryBody QueryBody;
    }

    [JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(SnakeCaseNamingStrategy))]
    public enum Operator
    {
        Is,
        Greater
    }

    public class TermQueryBody
    {
        public List<string> Fields;
        public Operator Operator;
        public List<string> Values;
    }

}