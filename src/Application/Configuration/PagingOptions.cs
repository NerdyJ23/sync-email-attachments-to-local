namespace SyncEmailAttachments.Application.Configuration;

public sealed record PagingOptions
{
	public int PageSize { get; set; } = 500;
}