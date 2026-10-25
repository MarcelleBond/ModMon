namespace ModMon.Cli;

internal static class PathResolver
{
	public static string? TryGetFullPath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
