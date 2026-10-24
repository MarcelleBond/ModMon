namespace ModMon.Cli;

internal sealed record AgentGuidanceWriteResult(
	List<string> CreatedFiles,
	List<string> SkippedFiles);
