namespace SyncEmailAttachments.Infrastructure.Data.EmailContext;

public sealed class EmailHistory
{
	public required string EmailAddress { get; set; }
	public DateTime LastEmailDate { get; set; } //Most recent email parsed
}