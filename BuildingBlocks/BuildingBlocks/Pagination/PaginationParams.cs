namespace BuildingBlocks.Pagination;

/// <summary>Query-string paging and search parameters (bind with [AsParameters]). All are optional.</summary>
public class PaginationParams
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }

    /// <summary>Free-text search; a Guid searches by id.</summary>
    public string? FindValues { get; set; }

    public int Page => Math.Max(1, PageNumber ?? 1);
    public int Size => Math.Clamp(PageSize ?? DefaultPageSize, 1, MaxPageSize);
}
