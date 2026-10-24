using System.Xml;
using System.Xml.Linq;

namespace ModMon.Cli;

internal sealed class ProjectReferenceReader
{
	private static readonly string[] ImplicitImports =
		{ "Directory.Build.props", "Directory.Build.targets" };

	public async Task<ProjectReferenceReadResult> ReadAsync(
		string projectPath,
		string repoRoot)
	{
		var context = new ProjectReferenceReadContext(projectPath, repoRoot);
		await ReadFileAsync(projectPath, context);
		await ReadImplicitImportsAsync(context);
		return new ProjectReferenceReadResult(context.Targets, context.Errors);
	}

	private static async Task ReadImplicitImportsAsync(
		ProjectReferenceReadContext context)
	{
		foreach (var importName in ImplicitImports)
		{
			var importPath = FindImplicitImport(importName, context);
			if (importPath != null)
			{
				await ReadFileAsync(importPath, context);
			}
		}
	}

	private static string? FindImplicitImport(
		string importName,
		ProjectReferenceReadContext context)
	{
		var directory = Path.GetDirectoryName(context.ProjectPath);
		while (IsWithinRoot(directory, context.RepoRoot))
		{
			var path = Path.Combine(directory!, importName);
			if (File.Exists(path))
			{
				return path;
			}

			if (PathsEqual(directory!, context.RepoRoot))
			{
				break;
			}

			directory = Directory.GetParent(directory!)?.FullName;
		}

		return null;
	}

	private static async Task ReadFileAsync(
		string filePath,
		ProjectReferenceReadContext context)
	{
		var canonicalPath = GetCanonicalPath(filePath, context);
		if (canonicalPath == null || !context.VisitedFiles.Add(canonicalPath))
		{
			return;
		}

		if (!File.Exists(filePath))
		{
			context.Errors.Add($"Imported project file not found: {filePath}");
			return;
		}

		await ReadXmlAsync(filePath, context);
	}

	private static string? GetCanonicalPath(
		string path,
		ProjectReferenceReadContext context)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception exception) when (
			exception is ArgumentException or NotSupportedException or
			PathTooLongException)
		{
			context.Errors.Add($"Invalid imported path '{path}': " +
				exception.Message);
			return null;
		}
	}

	private static async Task ReadXmlAsync(
		string filePath,
		ProjectReferenceReadContext context)
	{
		string content;
		try
		{
			content = await File.ReadAllTextAsync(filePath);
		}
		catch (Exception exception) when (
			exception is IOException or UnauthorizedAccessException)
		{
			context.Errors.Add($"Unable to read '{filePath}': " +
				exception.Message);
			return;
		}

		await ProcessContentAsync(content, filePath, context);
	}

	private static async Task ProcessContentAsync(
		string content,
		string filePath,
		ProjectReferenceReadContext context)
	{
		XDocument document;
		try
		{
			document = XDocument.Parse(content);
		}
		catch (XmlException exception)
		{
			context.Errors.Add($"Unable to parse '{filePath}': " +
				exception.Message);
			return;
		}

		await ProcessElementsAsync(document.Descendants(), filePath, context);
	}

	private static async Task ProcessElementsAsync(
		IEnumerable<XElement> elements,
		string filePath,
		ProjectReferenceReadContext context)
	{
		foreach (var element in elements)
		{
			await ProcessElementAsync(element, filePath, context);
		}
	}

	private static Task ProcessElementAsync(
		XElement element,
		string filePath,
		ProjectReferenceReadContext context)
	{
		var name = element.Name.LocalName;
		if (name == "ProjectReference")
		{
			ProcessReference(element, filePath, context);
			return Task.CompletedTask;
		}

		if (name == "Import")
		{
			return ProcessImportAsync(element, filePath, context);
		}

		ReportUnsupportedProperty(name, context);
		return Task.CompletedTask;
	}

	private static void ProcessReference(
		XElement element,
		string filePath,
		ProjectReferenceReadContext context)
	{
		if (HasUnsupportedMetadata(element))
		{
			ReportUnsupportedReference(filePath, context);
			return;
		}

		AddIncludedReferences(
			element.Attribute("Include")?.Value,
			filePath,
			context);
	}

	private static void ReportUnsupportedReference(
		string filePath,
		ProjectReferenceReadContext context)
	{
		context.Errors.Add(
			$"ProjectReference Update/Remove/Exclude is unsupported " +
			$"in '{filePath}'");
	}

	private static void AddIncludedReferences(
		string? include,
		string filePath,
		ProjectReferenceReadContext context)
	{
		if (string.IsNullOrWhiteSpace(include))
		{
			context.Errors.Add($"Empty ProjectReference Include in '{filePath}'");
			return;
		}

		var values = include.Split(
			';',
			StringSplitOptions.RemoveEmptyEntries |
			StringSplitOptions.TrimEntries);
		AddReferences(values, filePath, context);
	}

	private static void AddReferences(
		string[] values,
		string filePath,
		ProjectReferenceReadContext context)
	{
		foreach (var value in values)
		{
			AddReference(value, filePath, context);
		}
	}

	private static bool HasUnsupportedMetadata(XElement element)
	{
		return element.Attribute("Update") != null ||
			element.Attribute("Remove") != null ||
			element.Attribute("Exclude") != null;
	}

	private static void AddReference(
		string value,
		string filePath,
		ProjectReferenceReadContext context)
	{
		if (ContainsMsBuildExpression(value) || ContainsWildcard(value))
		{
			context.Errors.Add(
				$"Unresolved ProjectReference '{value}' in '{filePath}'");
			return;
		}

		TryAddReference(value, filePath, context);
	}

	private static void TryAddReference(
		string value,
		string filePath,
		ProjectReferenceReadContext context)
	{
		try
		{
			var target = ResolveReferencePath(value, filePath);
			if (target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
			{
				context.Targets.Add(target);
				return;
			}

			context.Errors.Add($"ProjectReference is not a .csproj: {target}");
		}
		catch (Exception exception) when (
			exception is ArgumentException or NotSupportedException or
			PathTooLongException)
		{
			context.Errors.Add(
				$"Invalid ProjectReference '{value}' in " +
				$"'{context.ProjectPath}': {exception.Message}");
		}
	}

	private static string ResolveReferencePath(string value, string filePath)
	{
		var baseDirectory = Path.GetDirectoryName(filePath)!;
		var normalized = NormalizeSeparators(value);
		return Path.GetFullPath(Path.Combine(baseDirectory, normalized));
	}

	private static async Task ProcessImportAsync(
		XElement element,
		string filePath,
		ProjectReferenceReadContext context)
	{
		var import = element.Attribute("Project")?.Value;
		if (string.IsNullOrWhiteSpace(import) ||
			ContainsMsBuildExpression(import) || ContainsWildcard(import))
		{
			context.Errors.Add($"Unresolved project import in '{filePath}'");
			return;
		}

		await TryReadImportAsync(import, filePath, context);
	}

	private static async Task TryReadImportAsync(
		string import,
		string filePath,
		ProjectReferenceReadContext context)
	{
		try
		{
			var importPath = ResolveImportPath(import, filePath);
			await ReadFileAsync(importPath, context);
		}
		catch (Exception exception) when (
			exception is ArgumentException or NotSupportedException or
			PathTooLongException)
		{
			context.Errors.Add($"Invalid project import in '{filePath}': " +
				exception.Message);
		}
	}

	private static string ResolveImportPath(string import, string filePath)
	{
		var directory = Path.GetDirectoryName(filePath)!;
		var normalized = NormalizeSeparators(import);
		return Path.GetFullPath(Path.Combine(directory, normalized));
	}

	private static void ReportUnsupportedProperty(
		string name,
		ProjectReferenceReadContext context)
	{
		if (name is "DirectoryBuildPropsPath" or "DirectoryBuildTargetsPath")
		{
			context.Errors.Add($"Custom {name} is not supported in " +
				$"'{context.ProjectPath}'");
		}
	}

	private static bool ContainsMsBuildExpression(string value)
	{
		return value.Contains("$(", StringComparison.Ordinal) ||
			value.Contains("@(", StringComparison.Ordinal) ||
			value.Contains("%(", StringComparison.Ordinal);
	}

	private static bool ContainsWildcard(string value)
	{
		return value.Contains('*') || value.Contains('?') ||
			value.Contains('%');
	}

	private static string NormalizeSeparators(string path)
	{
		return path.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);
	}

	private static bool IsWithinRoot(string? path, string root)
	{
		if (path == null)
		{
			return false;
		}

		var relative = Path.GetRelativePath(root, path);
		return !Path.IsPathRooted(relative) && relative != ".." &&
			!relative.StartsWith($"..{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal);
	}

	private static bool PathsEqual(string left, string right)
	{
		return GetPathComparer().Equals(
			Path.GetFullPath(left),
			Path.GetFullPath(right));
	}

	private static StringComparer GetPathComparer()
	{
		return OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
	}
}
