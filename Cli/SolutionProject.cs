namespace ModMon.Cli;

internal sealed record SolutionProject(
	string Name,
	string Path,
	ProjectRole Role);
