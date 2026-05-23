namespace ModMon.Cli;

internal sealed record ModuleAddOptions(
	string ModuleName,
	string OutputPath,
	bool DryRun,
	bool IsValid,
	string ErrorMessage)
{
	public static ModuleAddOptions Parse(string[] args)
	{
		var name = TryGetOptionValue(args, "--name");
		if (string.IsNullOrWhiteSpace(name))
		{
			return new ModuleAddOptions(
				ModuleName: string.Empty,
				OutputPath: string.Empty,
				DryRun: false,
				IsValid: false,
				ErrorMessage: "Missing required option: --name <ModuleName>"
			);
		}

		var output = TryGetOptionValue(args, "--output");
		var outputPath = output?.Trim() ?? string.Empty;
		var dryRun = args.Contains("--dry-run");
		return new ModuleAddOptions(name.Trim(), outputPath, dryRun, true, string.Empty);
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
