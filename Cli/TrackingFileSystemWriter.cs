namespace ModMon.Cli;

internal sealed class TrackingFileSystemWriter : IFileSystemWriter
{
	private readonly IFileSystemWriter _inner;

	public TrackingFileSystemWriter(IFileSystemWriter inner)
	{
		_inner = inner;
	}

	public List<string> Created { get; } = new();
	public List<string> Updated { get; } = new();

	public void EnsureDirectory(string path)
	{
		_inner.EnsureDirectory(path);
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		var exists = _inner.FileExists(path);
		if (exists && !overwrite)
		{
			return;
		}

		(exists ? Updated : Created).Add(path);
		_inner.WriteFile(path, content, overwrite);
	}

	public void DeleteFile(string path)
	{
		_inner.DeleteFile(path);
	}

	public void DeleteDirectory(string path)
	{
		_inner.DeleteDirectory(path);
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
}
