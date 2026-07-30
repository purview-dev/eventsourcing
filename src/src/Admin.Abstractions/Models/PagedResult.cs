namespace Purview.EventSourcing.Admin.Abstractions;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
	public int TotalPages => (int)((TotalCount + PageSize - 1) / PageSize);
	public bool HasNextPage => Page < TotalPages;
	public bool HasPreviousPage => Page > 1;
}
