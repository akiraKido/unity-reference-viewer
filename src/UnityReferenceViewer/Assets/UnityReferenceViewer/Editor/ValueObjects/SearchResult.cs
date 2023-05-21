#nullable enable
using System.Collections.Generic;

namespace UnityReferenceViewer.Editor.ValueObjects
{
    public class SearchResult
    {
        public readonly IReadOnlyList<SearchItem> SearchResults;
        public readonly string? AdditionalInformation;

        public SearchResult(IReadOnlyList<SearchItem> searchResults, string? additionalInformation)
        {
            SearchResults = searchResults;
            AdditionalInformation = additionalInformation;
        }
    }
}
