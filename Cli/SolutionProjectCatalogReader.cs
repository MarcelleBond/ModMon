using System.Text.RegularExpressions;

namespace ModMon.Cli;

internal sealed class SolutionProjectCatalogReader
{
	private const string ProjectPattern =
		@"^\s*Project\(""\{[^""]+\}""\)\s*=\s*""(?<name>[^""]+)""" +
		@"\s*,\s*""(?<path>[^""]+)""\s*,\s*""\{[^""]+\}""";
	private static readonly Regex ProjectLine = new(
		ProjectPattern,
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public async Task<SolutionProjectCatalog> ReadAsync(
		string startDirectory)
	{
		try
		{
			var solutionPath = FindSolution(startDirectory, out var error);
			return solutionPath == null
				? CreateMissingResult(error)
				: await ReadSolutionAsync(solutionPath);
		}
		catch (Exception exception) when (
			exception is ArgumentException or NotSupportedException or
			PathTooLongException)
		{
			return CreateMissingResult(
				$"Invalid validation path '{startDirectory}': " +
				exception.Message);
		}
	}

	private static SolutionProjectCatalog CreateMissingResult(
		string? error)
	{
		return new SolutionProjectCatalog(
			null,
			null,
			null,
			new List<SolutionProject>(),
			error ?? "No solution file found in directory tree",
			error == null);
	}

	private static string? FindSolution(
		string startDirectory,
		out string? error)
	{
		error = null;
		var directory = GetStartDirectory(startDirectory);
		while (directory != null)
		{
			var solutions = GetSolutionFiles(directory, out error);
			if (error != null || solutions.Length > 1)
			{
				error ??= $"Multiple solution files found in {directory}";
				return null;
			}

			if (solutions.Length == 1)
			{
				return solutions[0];
			}

			directory = Directory.GetParent(directory)?.FullName;
		}

		return null;
	}

	private static string? GetStartDirectory(string startDirectory)
	{
		var current = Path.GetFullPath(startDirectory);
		while (!Directory.Exists(current))
		{
			current = Directory.GetParent(current)?.FullName;
			if (current == null)
			{
				return null;
			}
		}

		return current;
	}

	private static string[] GetSolutionFiles(
		string directory,
		out string? error)
	{
		try
		{
			error = null;
			return Directory.GetFiles(directory)
				.Where(path => Path.GetExtension(path).Equals(
					".sln",
					StringComparison.OrdinalIgnoreCase))
				.ToArray();
		}
		catch (Exception exception) when (
			exception is IOException or UnauthorizedAccessException)
		{
			error = $"Unable to inspect directory '{directory}': " +
				exception.Message;
			return Array.Empty<string>();
		}
	}

	private static async Task<SolutionProjectCatalog> ReadSolutionAsync(
		string solutionPath)
	{
		var root = Path.GetDirectoryName(solutionPath)!;
		var projectName = Path.GetFileNameWithoutExtension(solutionPath);
		var projects = new List<SolutionProject>();
		var error = await ParseSolutionProjectsAsync(
			solutionPath,
			root,
			projectName,
			projects);

		return new SolutionProjectCatalog(
			solutionPath,
			root,
			projectName,
			projects,
			error,
			false);
	}

	private static async Task<string?> ParseSolutionProjectsAsync(
		string solutionPath,
		string root,
		string projectName,
		List<SolutionProject> projects)
	{
		string[] lines;
		try
		{
			lines = await File.ReadAllLinesAsync(solutionPath);
		}
		catch (Exception exception) when (
			exception is IOException or UnauthorizedAccessException)
		{
			return $"Unable to read solution '{solutionPath}': " +
				exception.Message;
		}

		return FindInvalidProjectLine(lines, root, projectName, projects);
	}

	private static string? FindInvalidProjectLine(
		string[] lines,
		string root,
		string projectName,
		List<SolutionProject> projects)
	{
		foreach (var line in lines)
		{
			var error = TryAddProject(line, root, projectName, projects);
			if (error != null)
			{
				return error;
			}
		}

		return null;
	}

	private static string? TryAddProject(
		string line,
		string root,
		string projectName,
		List<SolutionProject> projects)
	{
		if (!line.TrimStart().StartsWith("Project(", StringComparison.Ordinal))
		{
			return null;
		}

		var match = ProjectLine.Match(line);
		if (!match.Success)
		{
			return line.Contains(".csproj", StringComparison.OrdinalIgnoreCase)
				? $"Unrecognized project entry in solution: {line.Trim()}"
				: null;
		}

		var projectPath = match.Groups["path"].Value;
		return projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
			? AddProject(projectPath, root, projectName, projects)
			: null;
	}

	private static string? AddProject(
		string relativePath,
		string root,
		string projectName,
		List<SolutionProject> projects)
	{
		var path = ResolveProjectPath(relativePath, root);
		var name = Path.GetFileNameWithoutExtension(path);
		if (!IsWithinRoot(path, root))
		{
			return $"Project '{name}' is outside the solution root: {path}";
		}

		if (IsDuplicate(path, name, projects))
		{
			return $"Duplicate project entry '{name}' in solution";
		}

		projects.Add(new SolutionProject(
			name,
			path,
			ClassifyProject(name, projectName)));
		return null;
	}

	private static string ResolveProjectPath(
		string relativePath,
		string root)
	{
		var normalized = relativePath
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);
		return Path.GetFullPath(Path.Combine(root, normalized));
	}

	private static bool IsDuplicate(
		string path,
		string name,
		List<SolutionProject> projects)
	{
		var comparer = OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
		return projects.Any(project =>
			comparer.Equals(project.Path, path) ||
			comparer.Equals(project.Name, name));
	}

	private static bool IsWithinRoot(string path, string root)
	{
		var relative = Path.GetRelativePath(root, path);
		return !Path.IsPathRooted(relative) && relative != ".." &&
			!relative.StartsWith($"..{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal);
	}

	private static ProjectRole ClassifyProject(
		string name,
		string projectName)
	{
		if (name == $"{projectName}.Api")
		{
			return ProjectRole.Api;
		}

		if (name == $"{projectName}.SharedKernel")
		{
			return ProjectRole.SharedKernel;
		}

		if (IsTestProject(name))
		{
			return ProjectRole.Other;
		}

		return name.StartsWith(
			$"{projectName}.",
			StringComparison.Ordinal)
			? ProjectRole.Module
			: ProjectRole.Other;
	}

	private static bool IsTestProject(string name)
	{
		var suffixes = new[]
			{ ".Tests", ".Test", ".UnitTests", ".IntegrationTests" };
		return suffixes.Any(suffix => name.EndsWith(
			suffix,
			StringComparison.OrdinalIgnoreCase));
	}
}
