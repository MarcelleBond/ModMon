using ModMon.Cli;

namespace ModMon.Tests;

internal sealed class FakeFileSystemWriter : IFileSystemWriter
{
	private readonly HashSet<string> _directories = new(
		StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> _files = new(
		StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, string> Files => _files;

	public void EnsureDirectory(string path)
	{
		_directories.Add(Path.GetFullPath(path));
	}

	public void WriteFile(string path, string content, bool overwrite)
	{
		if (_files.ContainsKey(path) && !overwrite)
		{
			return;
		}

		_files[path] = content;
	}

	public void DeleteFile(string path)
	{
		_files.Remove(path);
	}

	public void DeleteDirectory(string path)
	{
		var full = Path.GetFullPath(path);
		_directories.Remove(full);
		foreach (var file in _files.Keys
			.Where(f => Path.GetFullPath(f)
				.StartsWith(full + Path.DirectorySeparatorChar,
					StringComparison.OrdinalIgnoreCase))
			.ToList())
		{
			_files.Remove(file);
		}
	}

	public bool FileExists(string path)
	{
		return _files.ContainsKey(path);
	}

	public string ReadFile(string path)
	{
		return _files[path];
	}

	public bool DirectoryExists(string path)
	{
		return _directories.Contains(Path.GetFullPath(path));
	}
}
