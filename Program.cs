using ModMon.Cli;

var app = new ModMonApp(
	console: new SystemConsole(),
	dotnet: new DotnetCliRunner(new SystemConsole()),
	fileSystem: new FileSystemWriter(),
	solutionLocator: new SolutionLocator());

return await app.RunAsync(args);
