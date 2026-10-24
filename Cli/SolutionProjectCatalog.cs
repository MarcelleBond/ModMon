namespace ModMon.Cli;

internal sealed record SolutionProjectCatalog(
	string? SolutionPath,
	string? RepoRoot,
	string? ProjectName,
	List<SolutionProject> Projects,
	string? Error,
	bool NotFound);
