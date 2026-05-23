namespace ModMon.Cli;

internal sealed class FileSystemWriter : IFileSystemWriter
{
	public void EnsureDirectory(string path)
	{
		Directory.CreateDirectory(path);
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			EnsureDirectory(directory);
		}

		if (File.Exists(path) && !overwrite)
		{
			return;
		}

		File.WriteAllText(path, content);
	}

	public bool FileExists(string path)
	{
		return File.Exists(path);
	}

	public bool DirectoryExists(string path)
	{
		return Directory.Exists(path);
	}
}
