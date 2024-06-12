using System.Text.RegularExpressions;

namespace SyncEmailAttachments.Application.Extensions;

public static class FileNameExtensions
{
	public static string GetFileExtension(string filename) =>
		filename.Split('.')[^1];

	public static string StripInvalidCharacters(string filename)
	{
		var regex = new Regex("[~\"#%&*:<>?/\\{|}]+");
		return regex.Replace(filename, "");
	}
}