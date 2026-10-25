namespace ModMon.Cli;

internal sealed class LoggingFileSystemWriter : IFileSystemWriter
{
	private readonly IFileSystemWriter _inner;
	private readonly ILogger _logger;

	public LoggingFileSystemWriter(
		IFileSystemWriter inner,
		ILogger logger)
	{
		_inner = inner;
		_logger = logger;
	}

	public void EnsureDirectory(string path)
	{
		_logger.LogDebug($"ensure directory {path}");
		_inner.EnsureDirectory(path);
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		_logger.LogDebug($"write file {path}");
		_inner.WriteFile(path, content, overwrite);
	}

	public void DeleteFile(string path)
	{
		_logger.LogDebug($"delete file {path}");
		_inner.DeleteFile(path);
	}

	public void DeleteDirectory(string path)
	{
		_logger.LogDebug($"delete directory {path}");
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
