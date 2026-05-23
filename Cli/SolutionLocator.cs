namespace ModMon.Cli;

internal sealed class SolutionLocator : ISolutionLocator
{
	public SolutionInfo? TryGetSolutionInfo(string startDirectory)
	{
		var dir = GetFirstExistingDirectory(startDirectory);
		while (dir != null)
		{
			var slns = Directory.GetFiles(dir.FullName, "*.sln");
			if (slns.Length > 1)
			{
				dir = dir.Parent;
				continue;
			}

			if (slns.Length == 1)
			{
				var projectName = Path.GetFileNameWithoutExtension(slns[0]);
				return new SolutionInfo(dir.FullName, projectName);
			}

			dir = dir.Parent;
		}

		return null;
	}

	private static DirectoryInfo? GetFirstExistingDirectory(string startDirectory)
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
