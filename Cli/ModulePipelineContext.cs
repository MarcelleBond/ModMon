namespace ModMon.Cli;

internal sealed record ModulePipelineContext(
	string RepoRoot,
	string ModuleProject,
	string ModuleFolder,
	Dictionary<string, string> ModifiedFiles);
