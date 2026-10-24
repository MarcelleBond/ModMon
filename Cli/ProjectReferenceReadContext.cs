namespace ModMon.Cli;

internal sealed class ProjectReferenceReadContext
{
	public ProjectReferenceReadContext(string projectPath, string repoRoot)
	{
		ProjectPath = projectPath;
		RepoRoot = repoRoot;
		VisitedFiles = new HashSet<string>(GetPathComparer());
	}

	public string ProjectPath { get; }
	public string RepoRoot { get; }
	public HashSet<string> VisitedFiles { get; }
	public List<string> Targets { get; } = new();
	public List<string> Errors { get; } = new();

	private static StringComparer GetPathComparer()
	{
		return OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
	}
}
