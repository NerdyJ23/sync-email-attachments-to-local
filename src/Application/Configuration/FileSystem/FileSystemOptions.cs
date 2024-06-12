namespace SyncEmailAttachments.Application.Configuration.FileSystem;

public sealed record FileSystemOptions
{
	public string Path { get; set; } = "";
}