using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFCCTools.OCAPI.ShopAPI.Resources;

namespace SFCCTools.OCAPI.SharedTypes
{
    public class SearchResult<T> : IAsyncEnumerable<T> where T : class
    {
        private readonly RawSearchResult<T> _rawSearchResult;
        private readonly SearchResultEnumerator<T> _enumerator;

        public int Total => _rawSearchResult.Total;

        public SearchResult(SearchRequest request, RawSearchResult<T> rawSearchResult,
            Func<SearchRequest, Task<RawSearchResult<T>>> requestMethod)
        {
            _rawSearchResult = rawSearchResult;
            _enumerator = new SearchResultEnumerator<T>(request, rawSearchResult, requestMethod);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return _enumerator;
        }
    }
    
    public class SearchResultEnumerator<T> : IAsyncEnumerator<T> where T : class
    {
        private RawSearchResult<T> _rawSearchResult;
        private int _position = -1;
        private readonly SearchRequest _request;
        private readonly Func<SearchRequest, Task<RawSearchResult<T>>> _requestMethod;

        public SearchResultEnumerator(SearchRequest request, RawSearchResult<T> rawSearchResult,
            Func<SearchRequest, Task<RawSearchResult<T>>> requestMethod)
        {
            _requestMethod = requestMethod;
            _rawSearchResult = rawSearchResult;
            _request = request;
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            _position = ++_position;
            if (_position >= _rawSearchResult.Total)
            {
                return false;
            }

            if (_position >= (_rawSearchResult.Start + _rawSearchResult.Count))
            {
                // get next page
                _request.Start = (_position / _request.Count) * _request.Count;
                _rawSearchResult = await _requestMethod(_request);
            }
            else if (_position < _rawSearchResult.Start)
            {
                // get previous page
                _request.Start = (_position / _request.Count) * _request.Count;
                _rawSearchResult = await _requestMethod(_request);
            }

            return true;
        }

        public T Current
        {
            get
            {
                var realPosition = _position - _rawSearchResult.Start;
                return _rawSearchResult.Hits[realPosition];
            }
        }
    }
    
    public class RawSearchResult<T>
    {
        public List<T> Hits;
        public int Total;
        public int Count;
        public int Start;
    }

    public class SearchHit<T>
    {
        public double Relevance;
        public T Data;
    }
}