namespace ModMon.Cli;

internal interface IFileSystemWriter
{
	void EnsureDirectory(string path);
	void WriteFile(string path, string content, bool overwrite);
	void DeleteFile(string path);
	void DeleteDirectory(string path);
	bool FileExists(string path);
	string ReadFile(string path);
	bool DirectoryExists(string path);
}
