namespace ModMon.Cli;

internal sealed class ModMonApp
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;
	private readonly ISolutionLocator _solutionLocator;

	public ModMonApp(
		IConsole console,
		IDotnetCliRunner dotnet,
		IFileSystemWriter fileSystem,
		ISolutionLocator solutionLocator)
	{
		_console = console;
		_dotnet = dotnet;
		_fileSystem = fileSystem;
		_solutionLocator = solutionLocator;
	}

	public async Task<int> RunAsync(string[] args)
	{
		if (args.Length == 0)
		{
			PrintHelp();
			return 1;
		}

		var globalOptions = ParseGlobalOptions(args, out var remainingArgs);
		var output = CreateOutputWriter(globalOptions);

		var command = remainingArgs.Length > 0
			? remainingArgs[0].ToLowerInvariant()
			: string.Empty;
		var remainder = remainingArgs.Skip(1).ToArray();

		int exitCode = command switch
		{
			"init" => await RunInitAsync(remainder, output),
			"module" => await RunModuleAsync(remainder, output),
			"info" => RunInfo(remainder, output),
			"validate" => RunValidate(remainder, output),
			"--help" or "-h" => PrintHelpAndReturnSuccess(),
			_ => PrintUnknownAndReturnFailure(command)
		};

		output.Flush();
		return exitCode;
	}

	private int PrintHelpAndReturnSuccess()
	{
		PrintHelp();
		return 0;
	}

	private int PrintUnknownAndReturnFailure(string command)
	{
		_console.WriteError($"Unknown command: {command}");
		PrintHelp();
		return 1;
	}

	private void PrintHelp()
	{
		_console.WriteLine("Usage:");
		_console.WriteLine(
			"  modmon init --name <ProjectName> [--output <Path>] " +
			"[--format json] [--dry-run]");
		_console.WriteLine(
			"  modmon module add --name <ModuleName> [--output <Path>] " +
			"[--format json] [--dry-run]");
		_console.WriteLine(
			"  modmon info [--path <Path>] [--format json]");
		_console.WriteLine(
			"  modmon validate [--path <Path>] [--format json]");
		_console.WriteLine(
			"  modmon module list [--path <Path>] [--format json]");
		_console.WriteLine(string.Empty);
		_console.WriteLine("Global Options:");
		_console.WriteLine("  --format <human|json>  Output format (default: human)");
		_console.WriteLine("  --verbose              Enable verbose logging");
		_console.WriteLine("  --log-file <path>      Write logs to file");
	}

	private GlobalOptions ParseGlobalOptions(
		string[] args,
		out string[] remainingArgs)
	{
		var format = OutputFormat.Human;
		var verbose = false;
		string? logFile = null;
		var remaining = new List<string>();

		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--format" && i + 1 < args.Length)
			{
				format = args[i + 1].ToLowerInvariant() == "json"
					? OutputFormat.Json
					: OutputFormat.Human;
				i++;
			}
			else if (args[i] == "--verbose")
			{
				verbose = true;
			}
			else if (args[i] == "--log-file" && i + 1 < args.Length)
			{
				logFile = args[i + 1];
				i++;
			}
			else
			{
				remaining.Add(args[i]);
			}
		}

		remainingArgs = remaining.ToArray();
		return new GlobalOptions(format, verbose, logFile);
	}

	private IOutputWriter CreateOutputWriter(GlobalOptions options)
	{
		return options.Format == OutputFormat.Json
			? new JsonOutputWriter()
			: new HumanOutputWriter(_console);
	}

	private ILogger CreateLogger(GlobalOptions options)
	{
		var loggers = new List<ILogger>();

		if (options.Verbose)
		{
			loggers.Add(new ConsoleLogger(_console, true));
		}

		if (!string.IsNullOrWhiteSpace(options.LogFile))
		{
			loggers.Add(new FileLogger(options.LogFile));
		}

		return loggers.Count switch
		{
			0 => new NullLogger(),
			1 => loggers[0],
			_ => new CompositeLogger(loggers.ToArray())
		};
	}

	private async Task<int> RunInitAsync(string[] args, IOutputWriter output)
	{
		var options = InitOptions.Parse(args);
		if (!options.IsValid)
		{
			var errorResult = new InitResult(
				Success: false,
				Message: options.ErrorMessage,
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { options.ErrorMessage });
			output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.InvalidArguments;
		}

		var handler = new InitCommandHandler(
			_console,
			_dotnet,
			_fileSystem,
			output);
		return await handler.HandleAsync(options);
	}

	private async Task<int> RunModuleAsync(string[] args, IOutputWriter output)
	{
		if (args.Length == 0)
		{
			var errorResult = new ModuleAddResult(
				Success: false,
				Message: "Missing module subcommand",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					"Missing module subcommand. Use 'module add' or 'module list'"
				});
			output.WriteError(errorResult.Message, errorResult);
			PrintHelp();
			return (int)ExitCode.InvalidArguments;
		}

		var sub = args[0].ToLowerInvariant();
		if (sub == "list")
		{
			var listOptions = InfoOptions.Parse(args.Skip(1).ToArray());
			var listHandler = new ModuleListCommandHandler(_fileSystem, output);
			return listHandler.Handle(listOptions.Path);
		}

		if (sub != "add")
		{
			var errorResult = new ModuleAddResult(
				Success: false,
				Message: $"Unknown module subcommand: {sub}",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					$"Unknown module subcommand: {sub}. Use 'module add' or 'module list'"
				});
			output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.InvalidArguments;
		}

		var options = ModuleAddOptions.Parse(args.Skip(1).ToArray());
		if (!options.IsValid)
		{
			var errorResult = new ModuleAddResult(
				Success: false,
				Message: options.ErrorMessage,
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { options.ErrorMessage });
			output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.InvalidArguments;
		}
		var startDirectory = string.IsNullOrWhiteSpace(options.OutputPath)
			? Directory.GetCurrentDirectory()
			: Path.GetFullPath(options.OutputPath);
		var solutionInfo = _solutionLocator.TryGetSolutionInfo(startDirectory);
		if (solutionInfo == null)
		{
			var errorResult = new ModuleAddResult(
				Success: false,
				Message: "Unable to infer project name from solution file",
				ExitCode: ExitCode.ResourceNotFound,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					"Unable to infer project name from a single *.sln in repo root."
				});
			output.WriteError(errorResult.Message, errorResult);
			return (int)ExitCode.ResourceNotFound;
		}

		IDryRunTracker tracker = options.DryRun
			? new DryRunTracker(true)
			: new NullDryRunTracker();

		var handler = new ModuleAddCommandHandler(
			_console,
			_dotnet,
			_fileSystem,
			output);

		return await handler.HandleAsync(
			new ModuleAddRequest(
				solutionInfo.RepoRoot,
				solutionInfo.ProjectName,
				options.ModuleName),
			tracker);
	}

	private int RunInfo(string[] args, IOutputWriter output)
	{
		var options = InfoOptions.Parse(args);
		var handler = new InfoCommandHandler(_fileSystem, output);
		return handler.Handle(options.Path);
	}

	private int RunValidate(string[] args, IOutputWriter output)
	{
		var options = InfoOptions.Parse(args);
		var handler = new ValidateCommandHandler(_fileSystem, output);
		return handler.Handle(options.Path);
	}
}
