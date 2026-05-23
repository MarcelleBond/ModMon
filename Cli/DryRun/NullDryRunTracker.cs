namespace ModMon.Cli;

internal sealed class NullDryRunTracker : IDryRunTracker
{
	public bool IsDryRun => false;

	public void RecordOperation(string type, Dictionary<string, object> details)
	{
	}

	public List<DryRunOperation> GetOperations()
	{
		return new List<DryRunOperation>();
	}
}
