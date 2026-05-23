namespace ModMon.Cli;

internal sealed record InfoOptions(string? Path)
{
	public static InfoOptions Parse(string[] args)
	{
		var path = TryGetOptionValue(args, "--path");
		return new InfoOptions(path);
	}

	private static string? TryGetOptionValue(string[] args, string key)
	{
		for (var i = 0; i < args.Length; i++)
		{
			if (!string.Equals(args[i], key, StringComparison.Ordinal))
			{
				continue;
			}

			var valueIndex = i + 1;
			return valueIndex < args.Length ? args[valueIndex] : null;
		}

		return null;
	}
}
