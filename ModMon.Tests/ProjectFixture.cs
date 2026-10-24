using System.Xml.Linq;

namespace ModMon.Tests;

internal sealed class ProjectFixture : IDisposable
{
	private const string ProjectGuid =
		"FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
	private readonly List<(string Name, string RelativePath, string Id)>
		_solutionProjects = new();
	private readonly Dictionary<string, string> _projectPaths = new();

	public ProjectFixture()
	{
		Root = Path.Combine(
			Path.GetTempPath(),
			$"modmon-validation-{Guid.NewGuid():N}");
		Directory.CreateDirectory(Root);
	}

	public string Root { get; }

	public async Task AddProjectAsync(string name)
	{
		var directory = Path.Combine(Root, name);
		Directory.CreateDirectory(directory);
		var projectPath = Path.Combine(directory, $"{name}.csproj");
		_projectPaths.Add(name, projectPath);
		_solutionProjects.Add((
			name,
			$"{name}\\{name}.csproj",
			Guid.NewGuid().ToString("B")));
		await WriteProjectAsync(name);
	}

	public async Task WriteProjectAsync(
		string name,
		params string[] referenceIncludes)
	{
		var references = referenceIncludes.Select(include =>
			new XElement(
				"ProjectReference",
				new XAttribute("Include", include)));
		var project = new XElement(
			"Project",
			new XAttribute("Sdk", "Microsoft.NET.Sdk"),
			referenceIncludes.Length == 0
				? null
				: new XElement("ItemGroup", references));
		var document = new XDocument(project);
		await using var stream = File.Create(_projectPaths[name]);
		await document.SaveAsync(
			stream,
			SaveOptions.None,
			CancellationToken.None);
	}

	public async Task SetReferencesAsync(
		string source,
		params string[] targets)
	{
		var sourceDirectory = Path.GetDirectoryName(_projectPaths[source])!;
		var includes = targets.Select(target =>
		{
			var relativePath = Path.GetRelativePath(
				sourceDirectory,
				_projectPaths[target]);
			return relativePath.Replace(Path.DirectorySeparatorChar, '\\');
		}).ToArray();
		await WriteProjectAsync(source, includes);
	}

	public Task WriteProjectContentAsync(string name, string content)
	{
		return File.WriteAllTextAsync(_projectPaths[name], content);
	}

	public async Task WriteSolutionAsync()
	{
		var lines = new List<string>
		{
			"Microsoft Visual Studio Solution File, Format Version 12.00"
		};
		lines.AddRange(_solutionProjects.Select(project =>
			$"Project(\"{{{ProjectGuid}}}\") = " +
			$"\"{project.Name}\", \"{project.RelativePath}\", " +
			$"\"{project.Id}\"\r\nEndProject"));
		lines.Add("Global");
		lines.Add("EndGlobal");
		await File.WriteAllLinesAsync(
			Path.Combine(Root, "Demo.sln"),
			lines);
	}

	public string ProjectPath(string name)
	{
		return _projectPaths[name];
	}

	public void Dispose()
	{
		Directory.Delete(Root, recursive: true);
	}
}
