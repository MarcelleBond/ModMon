namespace ModMon.Cli;

internal interface IFileSystemWriter
{
	void EnsureDirectory(string path);
	void WriteFile(string path, string content, bool overwrite);
	bool FileExists(string path);
	bool DirectoryExists(string path);
}
