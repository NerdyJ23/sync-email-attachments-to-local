using MySqlConnector;

namespace SyncEmailAttachments.Application.Extensions;

public static class DateTimeExtensions
{
	public static MySqlDateTime ToMySqlDateTime(this DateTime date) =>
		new(date);
}