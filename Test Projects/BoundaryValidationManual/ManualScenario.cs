namespace BoundaryValidationManual;

internal sealed record ManualScenario(
	string Name,
	int ExpectedExitCode,
	Func<string, string, string, Task> CreateAsync);
