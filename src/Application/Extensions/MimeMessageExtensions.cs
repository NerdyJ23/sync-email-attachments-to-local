using MimeKit;

namespace SyncEmailAttachments.Application.Extensions;

public static class MimeMessageExtensions
{
	private static readonly string[] EmailsToTreatAsAttachments = ["billing@linode.com"];

	public static bool TreatAsAttachment(this MimeMessage message) =>
		EmailsToTreatAsAttachments.Any(x => ((System.Net.Mail.MailAddressCollection)message.From).Any(a => a.Address == x));
}