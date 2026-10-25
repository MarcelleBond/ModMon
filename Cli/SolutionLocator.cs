namespace ModMon.Cli;

internal sealed class SolutionLocator : ISolutionLocator
{
	public SolutionLookup GetSolutionInfo(string startDirectory)
	{
		var dir = GetFirstExistingDirectory(startDirectory);
		while (dir != null)
		{
			var slns = Directory.GetFiles(dir.FullName, "*.sln");
			if (slns.Length > 1)
			{
				return new SolutionLookup(
					null,
					$"Multiple solution files found in '{dir.FullName}'. " +
					"Keep a single *.sln per solution directory.");
			}

			if (slns.Length == 1)
			{
				var projectName = Path.GetFileNameWithoutExtension(slns[0]);
				return new SolutionLookup(
					new SolutionInfo(dir.FullName, projectName),
					null);
			}

			dir = dir.Parent;
		}

		return new SolutionLookup(null, null);
	}

	private static DirectoryInfo? GetFirstExistingDirectory(
		string startDirectory)
	{
		var current = startDirectory;
		while (!Directory.Exists(current))
		{
			var parent = Directory.GetParent(current);
			if (parent == null)
			{
				return null;
			}

			current = parent.FullName;
		}

		return new DirectoryInfo(current);
	}
}
