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
			PrintHelp(_console);
			return 1;
		}

		var globalOptions = ParseGlobalOptions(args, out var remainingArgs);
		var output = CreateOutputWriter(globalOptions);
		var logger = CreateLogger(globalOptions);
		var console = CreateProgressConsole(globalOptions);
		var dotnet = ReferenceEquals(console, _console)
			? _dotnet
			: new DotnetCliRunner(console);

		var command = remainingArgs.Length > 0
			? remainingArgs[0].ToLowerInvariant()
			: string.Empty;
		var remainder = remainingArgs.Skip(1).ToArray();
		logger.LogInfo($"command: {(command.Length == 0 ? "<none>" : command)}");

		int exitCode;
		try
		{
			exitCode = command switch
			{
				"init" => await RunInitAsync(
					remainder, output, console, dotnet, logger),
				"module" => await RunModuleAsync(
					remainder, output, console, dotnet, logger),
				"info" => RunInfo(remainder, output),
				"validate" => await RunValidateAsync(remainder, output),
				"--help" or "-h" => PrintHelpAndReturnSuccess(),
				_ => PrintUnknownAndReturnFailure(command, console)
			};
		}
		catch (Exception exception)
		{
			logger.LogError($"unhandled exception: {exception}");
			output.WriteError($"Unexpected error: {exception.Message}");
			output.Flush();
			return (int)ExitCode.GeneralFailure;
		}

		logger.LogInfo($"exit code: {exitCode}");
		output.Flush();
		return exitCode;
	}

	private int PrintHelpAndReturnSuccess()
	{
		PrintHelp(_console);
		return 0;
	}

	private int PrintUnknownAndReturnFailure(string command, IConsole console)
	{
		console.WriteError($"Unknown command: {command}");
		PrintHelp(console);
		return 1;
	}

	private void PrintHelp(IConsole console)
	{
		console.WriteLine("Usage:");
		console.WriteLine(
			"  modmon init --name <ProjectName> [--output <Path>] " +
			"[--format json] [--dry-run]");
		console.WriteLine(
			"  modmon module add --name <ModuleName> [--output <Path>] " +
			"[--format json] [--dry-run]");
		console.WriteLine(
			"  modmon info [--path <Path>] [--format json]");
		console.WriteLine(
			"  modmon validate [--path <Path>] [--format json]");
		console.WriteLine(
			"  modmon module list [--path <Path>] [--format json]");
		console.WriteLine(string.Empty);
		console.WriteLine("Global Options:");
		console.WriteLine(
			"  --format <human|json>  Output format (default: human)");
		console.WriteLine("  --verbose              Enable verbose logging");
		console.WriteLine("  --log-file <path>      Write logs to file");
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

	private IConsole CreateProgressConsole(GlobalOptions options)
	{
		return options.Format == OutputFormat.Json
			? new StderrConsole(_console)
			: _console;
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

	private async Task<int> RunInitAsync(
		string[] args,
		IOutputWriter output,
		IConsole console,
		IDotnetCliRunner dotnet,
		ILogger logger)
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
			console,
			dotnet,
			_fileSystem,
			output,
			logger);
		return await handler.HandleAsync(options);
	}

	private async Task<int> RunModuleAsync(
		string[] args,
		IOutputWriter output,
		IConsole console,
		IDotnetCliRunner dotnet,
		ILogger logger)
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
			PrintHelp(console);
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
			: PathResolver.TryGetFullPath(options.OutputPath);
		if (startDirectory == null)
		{
			var invalid = new ModuleAddResult(
				Success: false,
				Message: $"Invalid --output path '{options.OutputPath}'.",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					$"Invalid --output path '{options.OutputPath}'."
				});
			output.WriteError(invalid.Message, invalid);
			return (int)invalid.ExitCode;
		}

		var lookup = _solutionLocator.GetSolutionInfo(startDirectory);
		if (lookup.Info == null)
		{
			var message = lookup.Error
				?? "Unable to infer project name from a single *.sln in repo root.";
			var exitCode = lookup.Error != null
				? ExitCode.ValidationFailed
				: ExitCode.ResourceNotFound;
			var errorResult = new ModuleAddResult(
				Success: false,
				Message: message,
				ExitCode: exitCode,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string> { message });
			output.WriteError(errorResult.Message, errorResult);
			return (int)exitCode;
		}

		if (!NameValidator.IsValid(lookup.Info.ProjectName))
		{
			var nameError = new ModuleAddResult(
				Success: false,
				Message: $"Solution name '{lookup.Info.ProjectName}' " +
					"is not a valid project name.",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				Errors: new List<string>
				{
					$"Solution name '{lookup.Info.ProjectName}' is not a " +
					"valid project name. Rename the .sln or run init."
				});
			output.WriteError(nameError.Message, nameError);
			return (int)nameError.ExitCode;
		}

		IDryRunTracker tracker = options.DryRun
			? new DryRunTracker(true)
			: new NullDryRunTracker();

		var handler = new ModuleAddCommandHandler(
			console,
			dotnet,
			_fileSystem,
			output,
			logger);

		return await handler.HandleAsync(
			new ModuleAddRequest(
				lookup.Info.RepoRoot,
				lookup.Info.ProjectName,
				options.ModuleName),
			tracker);
	}

	private int RunInfo(string[] args, IOutputWriter output)
	{
		var options = InfoOptions.Parse(args);
		var handler = new InfoCommandHandler(_fileSystem, output);
		return handler.Handle(options.Path);
	}

	private async Task<int> RunValidateAsync(
		string[] args,
		IOutputWriter output)
	{
		var options = InfoOptions.Parse(args);
		var handler = new ValidateCommandHandler(_fileSystem, output);
		return await handler.HandleAsync(options.Path);
	}
}
