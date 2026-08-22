namespace AppointmentCrm.Application.Common;

public sealed record PageRequest(
    int Page,
    int PageSize,
    string? Search,
    string SortBy,
    bool Descending)
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
