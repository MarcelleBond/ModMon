namespace ModMon.Cli;

internal sealed class DryRunFileSystemWriter : IFileSystemWriter
{
	private readonly IFileSystemWriter _inner;
	private readonly IDryRunTracker _tracker;

	public DryRunFileSystemWriter(
		IFileSystemWriter inner,
		IDryRunTracker tracker)
	{
		_inner = inner;
		_tracker = tracker;
	}

	public void EnsureDirectory(string path)
	{
		if (!_inner.DirectoryExists(path))
		{
			Record("create-directory", path);
		}
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		var exists = _inner.FileExists(path);
		if (exists && !overwrite)
		{
			return;
		}

		Record(exists ? "update-file" : "create-file", path);
	}

	public void DeleteFile(string path)
	{
		if (_inner.FileExists(path))
		{
			Record("delete-file", path);
		}
	}

	public void DeleteDirectory(string path)
	{
		if (_inner.DirectoryExists(path))
		{
			Record("delete-directory", path);
		}
	}

	public bool FileExists(string path)
	{
		return _inner.FileExists(path);
	}

	public string ReadFile(string path)
	{
		return _inner.ReadFile(path);
	}

	public bool DirectoryExists(string path)
	{
		return _inner.DirectoryExists(path);
	}

	private void Record(string type, string path)
	{
		_tracker.RecordOperation(
			type,
			new Dictionary<string, object> { ["path"] = path });
	}
}
