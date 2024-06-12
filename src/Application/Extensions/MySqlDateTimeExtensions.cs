using MySqlConnector;

namespace SyncEmailAttachments.Application.Extensions;

public static class MySqlDateTimeExtensions
{
	public static DateTime ToDateTime(this MySqlDateTime date) =>
		new(year: date.Year, month: date.Month, day: date.Day, hour: date.Hour, minute: date.Minute, second: date.Second);
}