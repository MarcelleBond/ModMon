namespace ModMon.Cli;

internal sealed record GlobalOptions(
	OutputFormat Format,
	bool Verbose,
	string? LogFile);
