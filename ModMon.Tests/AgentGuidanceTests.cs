using ModMon.Cli;

namespace ModMon.Tests;

public sealed class AgentGuidanceTests
{
	[Fact]
	public void WritesProjectSpecificGuidanceFiles()
	{
		var root = CreateRoot();
		try
		{
			var result = WriteGuidance(root, "Demo");

			Assert.Equal(4, result.CreatedFiles.Count);
			Assert.Empty(result.SkippedFiles);
			Assert.Contains("Demo", File.ReadAllText(
				Path.Combine(root, "AGENTS.md")));
			Assert.True(File.Exists(Path.Combine(
				root,
				".agent",
				"modmon",
				"SKILL.md")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	[Fact]
	public void PreservesUnownedFilesAndUpdatesOwnedFiles()
	{
		var root = CreateRoot();
		try
		{
			var agentsPath = Path.Combine(root, "AGENTS.md");
			File.WriteAllText(agentsPath, "User guidance");
			var ownedPath = Path.Combine(root, ".agent", "modmon", "SKILL.md");
			Directory.CreateDirectory(Path.GetDirectoryName(ownedPath)!);
			File.WriteAllText(
				ownedPath,
				$"{AgentGuidanceTemplates.OwnershipMarker}\nOld");

			var result = WriteGuidance(root, "Demo");

			Assert.Contains(agentsPath, result.SkippedFiles);
			Assert.DoesNotContain(agentsPath, result.CreatedFiles);
			Assert.Contains("User guidance", File.ReadAllText(agentsPath));
			Assert.Contains("Demo", File.ReadAllText(ownedPath));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	[Fact]
	public void DryRunReportsGuidanceWithoutWritingFiles()
	{
		var root = CreateRoot();
		try
		{
			var tracker = new DryRunTracker(true);
			var writer = new AgentGuidanceWriter(
				new PhysicalFileSystemWriter());
			var result = writer.WriteAll(root, "Demo", tracker);

			Assert.Empty(result.CreatedFiles);
			Assert.Equal(4, tracker.GetOperations().Count);
			Assert.False(File.Exists(Path.Combine(root, "AGENTS.md")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	private static AgentGuidanceWriteResult WriteGuidance(
		string root,
		string projectName)
	{
		return new AgentGuidanceWriter(new PhysicalFileSystemWriter())
			.WriteAll(root, projectName, new NullDryRunTracker());
	}

	private static string CreateRoot()
	{
		var root = Path.Combine(
			Path.GetTempPath(),
			$"modmon-guidance-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		return root;
	}

	private static void DeleteRoot(string root)
	{
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}
}
