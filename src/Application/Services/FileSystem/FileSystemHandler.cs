using Microsoft.Extensions.Options;
using PuppeteerSharp;
using SyncEmailAttachments.Application.Configuration.FileSystem;
using SyncEmailAttachments.Application.Extensions;

namespace SyncEmailAttachments.Application.Services.FileSystem;

public sealed class FileSystemHandler
{
	private readonly ILogger<FileSystemHandler> _logger;
	private readonly FileSystemOptions _options;
	private BrowserFetcher _browserFetcher;
	private bool DownloadedBrowser { get; set; }

	public FileSystemHandler(ILogger<FileSystemHandler> logger, IOptions<FileSystemOptions> options, BrowserFetcher browserFetcher)
	{
		_logger = logger;
		_options = options.Value;
		_browserFetcher = browserFetcher;
	}

	public async Task SyncToArchive(MemoryStream file, string filename, string alternateName = "", CancellationToken cancellationToken = default)
	{
		var path = Path.Combine(_options.Path, filename);
		_logger.LogTrace("Path to save is {path}", path);

		if (File.Exists(path))
		{
			_logger.LogTrace("Filename already existed");
			if (string.IsNullOrWhiteSpace(alternateName))
			{
				path = Path.Combine(_options.Path, $"{Guid.NewGuid()}-{filename}");
				_logger.LogDebug("Generated Guid for name, new path is {path}", path);
			}
			else
			{
				var newFileName = $"{FileNameExtensions.StripInvalidCharacters(alternateName)}.{FileNameExtensions.GetFileExtension(filename)}";
				_logger.LogDebug("Alternate filename was provided, using {filename} for new name", newFileName);

				await SyncToArchive(file, newFileName, cancellationToken: cancellationToken);
				return;
			}
		}

		using var stream = File.Create(path);

		if (!stream.CanWrite)
		{
			_logger.LogCritical("Cannot write file to {path}", path);
		}

		file.Position = 0;
		await stream.WriteAsync(file.ToArray(), cancellationToken);
	}

	public async Task SyncHtmlToArchive(string html, string filename, string alternateName = "", CancellationToken cancellationToken = default)
	{

		if (!DownloadedBrowser)
		{
			await _browserFetcher.DownloadAsync();
			DownloadedBrowser = true;
		}

		await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
		await using var page = await browser.NewPageAsync();
		await page.SetContentAsync(html);

		var result = await page.PdfStreamAsync();
		var stream = new MemoryStream();
		await result.CopyToAsync(stream, cancellationToken);
		stream.Position = 0;

		await SyncToArchive(stream, filename, alternateName, cancellationToken);
	}
}