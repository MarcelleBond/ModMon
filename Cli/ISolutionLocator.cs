namespace ModMon.Cli;

internal interface ISolutionLocator
{
	SolutionLookup GetSolutionInfo(string startDirectory);
}

internal sealed record SolutionInfo(
	string RepoRoot,
	string ProjectName);

internal sealed record SolutionLookup(
	SolutionInfo? Info,
	string? Error);
