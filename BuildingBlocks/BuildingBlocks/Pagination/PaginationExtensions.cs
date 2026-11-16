using System.Text.Json;

namespace BuildingBlocks.Pagination;

public static class PaginationExtensions
{
    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);

    /// <summary>Adds a "Pagination" response header and exposes it to browsers (CORS).</summary>
    public static void AddPagination<T>(this HttpResponse response, PagedList<T> list)
    {
        var header = new
        {
            list.CurrentPage,
            ItemsPerPage = list.PageSize,
            TotalItems = list.TotalCount,
            list.TotalPages
        };

        response.Headers.Append("Pagination", JsonSerializer.Serialize(header, CamelCase));
        response.Headers.Append("Access-Control-Expose-Headers", "Pagination");
    }
}
