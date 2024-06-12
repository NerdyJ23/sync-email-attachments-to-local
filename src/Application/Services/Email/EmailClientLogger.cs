using MailKit;

namespace SyncEmailAttachments.Application.Services.Emails;

public sealed class EmailClientLogger : IProtocolLogger
{
	private readonly ILogger<EmailClientLogger> _logger;
	public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; } = default!;

	public EmailClientLogger(ILogger<EmailClientLogger> logger)
	{
		_logger = logger;
	}


	public void Dispose() { }

	public void LogClient(byte[] buffer, int offset, int count)
	{
		// _logger.LogDebug("{buffer}", buffer);
		// _logger.LogDebug("{buffer} from {offset} to {count}", buffer[offset..(count - 1)], offset, count);
	}

	public void LogConnect(Uri uri)
	{
		_logger.LogDebug("Connecting to {uri}", uri.AbsoluteUri);
	}

	public void LogServer(byte[] buffer, int offset, int count)
	{
		// _logger.LogDebug("{buffer}", buffer);
		// _logger.LogDebug("{buffer} from {offset} to {count}", buffer[offset..(count - 1)], offset, count);
	}
}