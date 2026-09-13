namespace SmartMonitoring.Shared.Dtos;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}

public static class Pagination
{
    public const int DefaultPageSize = 25;

    public const int MaxPageSize = 200;

    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = MaxPageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1
            ? DefaultPageSize
            : pageSize > maxPageSize
                ? maxPageSize
                : pageSize;

        return (normalizedPage, normalizedPageSize);
    }
}
