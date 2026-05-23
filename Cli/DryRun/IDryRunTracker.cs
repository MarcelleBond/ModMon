namespace ModMon.Cli;

internal interface IDryRunTracker
{
	bool IsDryRun { get; }
	void RecordOperation(string type, Dictionary<string, object> details);
	List<DryRunOperation> GetOperations();
}
