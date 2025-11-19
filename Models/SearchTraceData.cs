namespace RetailMonolith.Models
{
    /// <summary>
    /// Contains detailed trace information for a search result
    /// </summary>
    public class SearchResultWithTrace
    {
        public Product Product { get; set; } = null!;
        public SearchTraceData TraceData { get; set; } = null!;
    }

    /// <summary>
    /// Detailed tracing information for semantic search debugging
    /// </summary>
    public class SearchTraceData
    {
        /// <summary>
        /// Overall search score for this result
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Reranker score if applicable
        /// </summary>
        public double? RerankerScore { get; set; }

        /// <summary>
        /// Fields that matched the search query
        /// </summary>
        public List<MatchedField> MatchedFields { get; set; } = new();

        /// <summary>
        /// Document ID in the search index
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// Vector similarity score
        /// </summary>
        public double? VectorSimilarityScore { get; set; }
    }

    /// <summary>
    /// Information about a field that matched the search query
    /// </summary>
    public class MatchedField
    {
        /// <summary>
        /// Name of the field (e.g., "Name", "Description", "Category")
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Matched content from this field
        /// </summary>
        public string? MatchedContent { get; set; }

        /// <summary>
        /// Score contribution from this field
        /// </summary>
        public double? FieldScore { get; set; }
    }

    /// <summary>
    /// Complete search response with optional tracing
    /// </summary>
    public class SearchResponse
    {
        public List<SearchResultWithTrace> Results { get; set; } = new();
        public string OriginalQuery { get; set; } = string.Empty;
        public List<string> ProcessedTokens { get; set; } = new();
        public int TotalResults { get; set; }
    }
}
