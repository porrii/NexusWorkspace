namespace NexusWorkspace.Application.Common;

/// <summary>A page of results plus the total count, for virtualised lists.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int PageCount => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < PageCount;

    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Empty(int pageSize = 50) => new([], 0, 1, pageSize);
}
