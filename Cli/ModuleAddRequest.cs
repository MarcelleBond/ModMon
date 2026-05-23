namespace ModMon.Cli;

internal sealed record ModuleAddRequest(
	string RepoRoot,
	string ProjectName,
	string ModuleName);
