using ModMon.Cli;

namespace ModMon.Tests;

internal sealed class PhysicalFileSystemWriter : IFileSystemWriter
{
	public void EnsureDirectory(string path)
	{
		Directory.CreateDirectory(path);
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		if (File.Exists(path) && !overwrite)
		{
			return;
		}

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(path, content);
	}

	public void DeleteFile(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public void DeleteDirectory(string path)
	{
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
	}

	public bool FileExists(string path)
	{
		return File.Exists(path);
	}

	public string ReadFile(string path)
	{
		return File.ReadAllText(path);
	}

	public bool DirectoryExists(string path)
	{
		return Directory.Exists(path);
	}
}
