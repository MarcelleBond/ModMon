namespace ModMon.Cli;

internal sealed class ProjectReferenceBoundaryValidator
{
	private const string BoundaryCheck = "Project reference boundary";
	private readonly ProjectReferenceReader _referenceReader;

	public ProjectReferenceBoundaryValidator(
		ProjectReferenceReader referenceReader)
	{
		_referenceReader = referenceReader;
	}

	public async Task<List<ValidationCheck>> ValidateAsync(
		SolutionProjectCatalog catalog)
	{
		var findings = new List<ValidationCheck>();
		var projectMap = CreateProjectMap(catalog.Projects);
		var graph = CreateGraph(catalog.Projects);
		AddMissingProjectFiles(catalog.Projects, findings);
		foreach (var project in catalog.Projects.Where(IsBoundaryProject))
		{
			if (File.Exists(project.Path))
			{
				await ValidateProjectAsync(
					project,
					catalog,
					projectMap,
					findings,
					graph);
			}
		}

		AddCycleFindings(graph, findings);
		AddPassingCheckIfClean(findings);
		return findings;
	}

	private async Task ValidateProjectAsync(
		SolutionProject project,
		SolutionProjectCatalog catalog,
		Dictionary<string, SolutionProject> projectMap,
		List<ValidationCheck> findings,
		Dictionary<string, HashSet<string>> graph)
	{
		var result = await _referenceReader.ReadAsync(
			project.Path,
			catalog.RepoRoot!);
		AddReadErrors(project, result.Errors, findings);
		ValidateTargets(project, result.Targets, projectMap, findings, graph);
	}

	private static void AddMissingProjectFiles(
		List<SolutionProject> projects,
		List<ValidationCheck> findings)
	{
		foreach (var project in projects.Where(project =>
			!File.Exists(project.Path)))
		{
			AddFailure(
				findings,
				ValidationRuleIds.ProjectFile,
				project.Path,
				$"Project file not found: {project.Path}");
		}
	}

	private static void AddReadErrors(
		SolutionProject project,
		List<string> errors,
		List<ValidationCheck> findings)
	{
		foreach (var error in errors)
		{
			AddFailure(
				findings,
				ValidationRuleIds.ProjectFile,
				project.Path,
				error);
		}
	}

	private static void ValidateTargets(
		SolutionProject source,
		List<string> targetPaths,
		Dictionary<string, SolutionProject> projectMap,
		List<ValidationCheck> findings,
		Dictionary<string, HashSet<string>> graph)
	{
		var seen = new HashSet<string>(GetPathComparer());
		foreach (var targetPath in targetPaths)
		{
			if (!seen.Add(targetPath))
			{
				AddFailure(
					findings,
					ValidationRuleIds.ProjectReference,
					source.Path,
					$"Duplicate project reference: {targetPath}");
				continue;
			}

			ValidateTarget(source, targetPath, projectMap, findings, graph);
		}
	}

	private static void ValidateTarget(
		SolutionProject source,
		string targetPath,
		Dictionary<string, SolutionProject> projectMap,
		List<ValidationCheck> findings,
		Dictionary<string, HashSet<string>> graph)
	{
		if (!projectMap.TryGetValue(targetPath, out var target))
		{
			AddFailure(
				findings,
				ValidationRuleIds.ProjectReference,
				source.Path,
				$"Project reference is not in the selected solution: " +
				targetPath);
			return;
		}

		graph[source.Path].Add(target.Path);
		if (!IsAllowed(source.Role, target.Role))
		{
			AddFailure(
				findings,
				ValidationRuleIds.ProjectBoundary,
				source.Path,
				$"{source.Name} ({source.Role}) cannot reference " +
				$"{target.Name} ({target.Role})");
		}
	}

	private static bool IsAllowed(ProjectRole source, ProjectRole target)
	{
		return source switch
		{
			ProjectRole.Api => target is ProjectRole.Module or
				ProjectRole.SharedKernel,
			ProjectRole.Module => target == ProjectRole.SharedKernel,
			_ => false
		};
	}

	private static bool IsBoundaryProject(SolutionProject project)
	{
		return project.Role is ProjectRole.Api or ProjectRole.Module or
			ProjectRole.SharedKernel;
	}

	private static Dictionary<string, SolutionProject> CreateProjectMap(
		List<SolutionProject> projects)
	{
		return projects.ToDictionary(
			project => project.Path,
			GetPathComparer());
	}

	private static Dictionary<string, HashSet<string>> CreateGraph(
		List<SolutionProject> projects)
	{
		return projects.Where(IsBoundaryProject).ToDictionary(
			project => project.Path,
			_ => new HashSet<string>(GetPathComparer()),
			GetPathComparer());
	}

	private static void AddCycleFindings(
		Dictionary<string, HashSet<string>> graph,
		List<ValidationCheck> findings)
	{
		var indegrees = graph.Keys.ToDictionary(path => path, _ => 0,
			GetPathComparer());
		foreach (var targets in graph.Values)
		{
			foreach (var target in targets.Where(indegrees.ContainsKey))
			{
				indegrees[target]++;
			}
		}

		var ready = new Queue<string>(
			indegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
		ReduceAcyclicNodes(graph, indegrees, ready);
		AddRemainingCycleNodes(indegrees, findings);
	}

	private static void ReduceAcyclicNodes(
		Dictionary<string, HashSet<string>> graph,
		Dictionary<string, int> indegrees,
		Queue<string> ready)
	{
		while (ready.TryDequeue(out var source))
		{
			foreach (var target in graph[source].Where(indegrees.ContainsKey))
			{
				indegrees[target]--;
				if (indegrees[target] == 0)
				{
					ready.Enqueue(target);
				}
			}
			indegrees.Remove(source);
		}
	}

	private static void AddRemainingCycleNodes(
		Dictionary<string, int> indegrees,
		List<ValidationCheck> findings)
	{
		foreach (var path in indegrees.Keys)
		{
			AddFailure(
				findings,
				ValidationRuleIds.ProjectCycle,
				path,
				$"Project remains blocked by a dependency cycle: {path}");
		}
	}

	private static void AddPassingCheckIfClean(
		List<ValidationCheck> findings)
	{
		if (findings.Count == 0)
		{
			findings.Add(new ValidationCheck(
				BoundaryCheck,
				true,
				"Project reference boundaries are valid",
				ValidationRuleIds.ProjectBoundary));
		}
	}

	private static void AddFailure(
		List<ValidationCheck> findings,
		string ruleId,
		string projectPath,
		string message)
	{
		findings.Add(new ValidationCheck(
			BoundaryCheck,
			false,
			message,
			ruleId,
			projectPath));
	}

	private static StringComparer GetPathComparer()
	{
		return OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
	}
}
