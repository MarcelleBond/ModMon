namespace ModMon.Cli;

internal interface ISolutionLocator
{
	SolutionInfo? TryGetSolutionInfo(string startDirectory);
}

internal sealed record SolutionInfo(
	string RepoRoot,
	string ProjectName);
