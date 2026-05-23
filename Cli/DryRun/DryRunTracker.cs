namespace ModMon.Cli;

internal sealed class DryRunTracker : IDryRunTracker
{
	private readonly List<DryRunOperation> _operations = new();
	private readonly bool _isDryRun;

	public DryRunTracker(bool isDryRun)
	{
		_isDryRun = isDryRun;
	}

	public bool IsDryRun => _isDryRun;

	public void RecordOperation(string type, Dictionary<string, object> details)
	{
		if (_isDryRun)
		{
			_operations.Add(new DryRunOperation(type, details));
		}
	}

	public List<DryRunOperation> GetOperations()
	{
		return _operations;
	}
}
