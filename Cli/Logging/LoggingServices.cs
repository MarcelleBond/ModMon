namespace ModMon.Cli;

internal static class LoggingServices
{
	public static IFileSystemWriter Wrap(
		IFileSystemWriter inner,
		ILogger logger)
	{
		return logger is NullLogger
			? inner
			: new LoggingFileSystemWriter(inner, logger);
	}

	public static IDotnetCliRunner Wrap(
		IDotnetCliRunner inner,
		ILogger logger)
	{
		return logger is NullLogger
			? inner
			: new LoggingDotnetCliRunner(inner, logger);
	}
}
