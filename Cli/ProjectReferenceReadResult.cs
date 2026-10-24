namespace ModMon.Cli;

internal sealed record ProjectReferenceReadResult(
	List<string> Targets,
	List<string> Errors);
