namespace ModMon.Cli;

internal sealed record ModuleWriteOutcome(
	bool DiAggregatorUpdated,
	bool DockerfileUpdated,
	int MigrationExitCode);
