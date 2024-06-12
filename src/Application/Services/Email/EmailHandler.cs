using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using SyncEmailAttachments.Application.Configuration;
using SyncEmailAttachments.Application.Configuration.Email;
using SyncEmailAttachments.Application.Extensions;
using SyncEmailAttachments.Application.Services.FileSystem;
using SyncEmailAttachments.Infrastructure.Data.EmailContext;

namespace SyncEmailAttachments.Application.Services.Emails;

public sealed class EmailHandler
{
	private readonly ILogger<EmailHandler> _logger;
	private readonly EmailOptions _options;
	private readonly PagingOptions _pagingOptions = new();
	private readonly EmailClientLogger _clientLogger;
	private readonly FileSystemHandler _file;
	private readonly IDbContextFactory<EmailContext> _dbFactory;
	private static readonly string[] IgnoredFolders = ["Drafts", "Deleted"];

	private DateTime LastParsed { get; set; } = DateTime.MinValue;

	public EmailHandler(
		ILogger<EmailHandler> logger,
		IOptions<EmailOptions> options,
		EmailClientLogger clientLogger,
		FileSystemHandler file,
		IDbContextFactory<EmailContext> dbFactory
	)
	{
		_logger = logger;
		_options = options.Value;
		_clientLogger = clientLogger;
		_file = file;
		_dbFactory = dbFactory;
	}

	public async Task<ImapClient> CreateClient(CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Attempting to connect to {uri} on port {port}", _options.Uri, _options.Port);
		var client = new ImapClient(_clientLogger);
		client.Connect(_options.Uri, _options.Port, MailKit.Security.SecureSocketOptions.Auto, cancellationToken: cancellationToken);
		await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

		_logger.LogDebug("Connection was {not}successful", client.IsConnected ? "" : "not ");
		return client;
	}

	public async Task<ICollection<IMailFolder>> GetFolders(ImapClient client, CancellationToken cancellationToken = default)
	{
		FolderNamespaceCollection? rootNamespace = client.PersonalNamespaces;
		_logger.LogDebug("Getting folders in root namespace {namespace}", client.PersonalNamespaces.First().Path);
		List<IMailFolder> folders = [];

		foreach (var n in rootNamespace)
		{
			folders.AddRange(await client.GetFoldersAsync(n, cancellationToken: cancellationToken));
		}

		return folders;
	}
	public async Task<int> Sync(CancellationToken cancellationToken = default)
	{
		using var client = await CreateClient(cancellationToken);
		var folders = await GetFolders(client, cancellationToken);
		var syncCount = 0;

		await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
		var lastParsed = await context.Set<EmailHistory>().FirstOrDefaultAsync(x => x.EmailAddress == _options.Username, cancellationToken);

		if (lastParsed is not null)
		{
			LastParsed = lastParsed.LastEmailDate;
		}

		_logger.LogDebug("Last Parsed: {lastParsed}", lastParsed?.LastEmailDate);

		foreach (var folder in folders)
		{
			if (IgnoredFolders.Contains(folder.Name))
			{
				continue;
			}

			_logger.LogDebug("Synching attachments in {folder}", folder.Name);
			syncCount += await SyncFolder(folder, cancellationToken);
		}

		_logger.LogDebug("Synched {count} attachments", syncCount);
		return syncCount;
	}

	public async Task<int> SyncFolder(IMailFolder folder, CancellationToken cancellationToken = default)
	{

		var syncCount = 0;

		await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
		var ids = await folder.SearchAsync(SearchQuery.SentSince(LastParsed), cancellationToken);

		_logger.LogDebug("Folder has {count} items", ids.Count);
		foreach (var id in ids)
		{
			var item = await folder.GetMessageAsync(id, cancellationToken);
			var attachments = item.Attachments.ToList();
			_logger.LogDebug("email with name {name} has {count} attachments", item.Subject, attachments.Count);


			if (attachments.Count > 0)
			{
				foreach (var a in attachments)
				{
					_logger.LogDebug("attaching file to stream");
					var stream = new MemoryStream();

					if (a is not MimePart attachment)
					{
						_logger.LogError("Failed to convert attachment to mimepart");
						continue;
					}

					await attachment.Content.DecodeToAsync(stream, cancellationToken);
					_logger.LogDebug("sending file {filename} of {bytes} bytes to syncArchiver", a.ContentDisposition?.FileName, stream.Length);

					await _file.SyncToArchive(stream, a.ContentDisposition?.FileName ?? Guid.NewGuid().ToString(), item.Subject, cancellationToken);
					syncCount++;
				}
			}

			if (item.TreatAsAttachment())
			{
				var body = "";

				if (!string.IsNullOrWhiteSpace(item.HtmlBody))
				{
					body = item.HtmlBody;
				}
				else
				{
					body = item.TextBody;
				}

				_logger.LogDebug("Treating email {email} as attachment, downloading body", item.Subject);

				await _file.SyncHtmlToArchive(body.ToString() ?? "", FileNameExtensions.StripInvalidCharacters(item.Subject) + ".pdf", cancellationToken: cancellationToken);
				syncCount++;
			}

			LastParsed = item.Date.LocalDateTime;
		}

		await UpdateMostRecent(cancellationToken);
		return syncCount;
	}

	public async Task UpdateMostRecent(CancellationToken cancellationToken = default)
	{
		await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
		var existingTime = await context.Set<EmailHistory>().FirstOrDefaultAsync(x => x.EmailAddress == _options.Username, cancellationToken: cancellationToken);

		if (existingTime is not null)
		{
			_logger.LogDebug("Existing time was not null");
			existingTime.LastEmailDate = LastParsed;
			context.Update(existingTime);
		}
		else
		{
			_logger.LogDebug("Existing time was null, adding item to db");
			context.Add(new EmailHistory()
			{
				EmailAddress = _options.Username,
				LastEmailDate = LastParsed
			});
		}

		var success = await context.SaveChangesAsync(cancellationToken) != 0;

		if (!success)
		{
			_logger.LogWarning("Failed to update last checked time");
		}
	}
}