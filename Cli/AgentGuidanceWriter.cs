namespace ModMon.Cli;

internal sealed class AgentGuidanceWriter
{
	private const bool Overwrite = true;
	private readonly IFileSystemWriter _fileSystem;

	public AgentGuidanceWriter(IFileSystemWriter fileSystem)
	{
		_fileSystem = fileSystem;
	}

	public AgentGuidanceWriteResult WriteAll(
		string repoRoot,
		string projectName,
		IDryRunTracker tracker)
	{
		var files = GetFiles(repoRoot, projectName);
		var created = new List<string>();
		var skipped = new List<string>();
		foreach (var file in files)
		{
			WriteFile(file, tracker, created, skipped);
		}

		return new AgentGuidanceWriteResult(created, skipped);
	}

	private void WriteFile(
		GuidanceFile file,
		IDryRunTracker tracker,
		List<string> created,
		List<string> skipped)
	{
		var exists = _fileSystem.FileExists(file.Path);
		if (exists && !IsModMonOwned(file.Path))
		{
			skipped.Add(file.Path);
			tracker.RecordOperation(
				"preserve-agent-guidance",
				new Dictionary<string, object>
				{
					["path"] = file.Path,
					["reason"] = "Existing file is not ModMon-owned"
				});
			return;
		}

		tracker.RecordOperation(
			exists ? "update-agent-guidance" : "create-agent-guidance",
			new Dictionary<string, object>
			{
				["path"] = file.Path,
				["dryRun"] = tracker.IsDryRun
			});
		if (!exists)
		{
			created.Add(file.Path);
		}
		if (!tracker.IsDryRun)
		{
			_fileSystem.WriteFile(file.Path, file.Content, Overwrite);
		}
	}

	private bool IsModMonOwned(string path)
	{
		var content = _fileSystem.ReadFile(path);
		return content.Contains(
			AgentGuidanceTemplates.OwnershipMarker,
			StringComparison.Ordinal);
	}

	private static List<GuidanceFile> GetFiles(
		string repoRoot,
		string projectName)
	{
		var agentRoot = Path.Combine(
			repoRoot, ".agents", "skills", "modmon");
		return new List<GuidanceFile>
		{
			new(
				Path.Combine(repoRoot, "AGENTS.md"),
				AgentGuidanceTemplates.Agents(projectName)),
			new(
				Path.Combine(agentRoot, "SKILL.md"),
				AgentGuidanceTemplates.Skill(projectName)),
			new(
				Path.Combine(agentRoot, "architecture-patterns.md"),
				AgentGuidanceTemplates.Architecture(projectName)),
			new(
				Path.Combine(agentRoot, "quick-reference.md"),
				AgentGuidanceTemplates.QuickReference(projectName))
		};
	}

	private sealed record GuidanceFile(string Path, string Content);
}
