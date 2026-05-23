namespace ModMon.Cli;

public record DryRunOperation(
	string Type,
	Dictionary<string, object> Details);
