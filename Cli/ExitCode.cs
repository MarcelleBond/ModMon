namespace ModMon.Cli;

public enum ExitCode
{
	Success = 0,
	GeneralFailure = 1,
	InvalidArguments = 2,
	ResourceAlreadyExists = 3,
	ResourceNotFound = 4,
	ValidationFailed = 5,
	ExternalToolError = 6,
	FileSystemError = 7,
	NetworkError = 8
}
