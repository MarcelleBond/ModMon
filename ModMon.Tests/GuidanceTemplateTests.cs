using ModMon.Cli;

namespace ModMon.Tests;

public sealed class GuidanceTemplateTests
{
	private static readonly string[] TemplateFiles =
	{
		"AGENTS.md",
		"SKILL.md",
		"architecture-patterns.md",
		"quick-reference.md"
	};

	[Fact]
	public void EmbeddedTemplatesLoadAndSubstitutePlaceholders()
	{
		var outputs = new[]
		{
			AgentGuidanceTemplates.Agents("Demo"),
			AgentGuidanceTemplates.Skill("Demo"),
			AgentGuidanceTemplates.Architecture("Demo"),
			AgentGuidanceTemplates.QuickReference("Demo")
		};

		foreach (var output in outputs)
		{
			Assert.DoesNotContain("{{", output);
			Assert.Contains("Demo", output);
			Assert.Contains(
				AgentGuidanceTemplates.OwnershipMarker, output);
		}
	}

	[Fact]
	public void WindsurfSkillCopiesMatchCanonicalTemplates()
	{
		var repoRoot = FindRepoRoot();
		var pairs = new (string Canonical, string Windsurf)[]
		{
			("SKILL.md", "SKILL.md"),
			("architecture-patterns.md", "architecture-patterns.md"),
			("quick-reference.md", "quick-reference.md")
		};

		foreach (var (canonical, windsurf) in pairs)
		{
			var canonicalBody = Normalize(File.ReadAllText(
				Path.Combine(
					repoRoot, "templates", "agent-guidance", canonical)));
			var windsurfBody = Normalize(File.ReadAllText(
				Path.Combine(
					repoRoot, ".windsurf", "skills", "using-modmon",
					windsurf)));

			Assert.True(
				canonicalBody == windsurfBody,
				$"'{windsurf}' drifted from templates/agent-guidance/" +
				$"{canonical}. Sync the .windsurf copy.");
		}
	}

	private static string Normalize(string content)
	{
		var lines = content
			.Replace("\r\n", "\n")
			.Split('\n')
			.Where(line => !line.Contains(
				AgentGuidanceTemplates.OwnershipMarker))
			.ToList();
		var body = string.Join('\n', lines);
		return StripFrontmatter(body).Trim();
	}

	private static string StripFrontmatter(string body)
	{
		var trimmed = body.TrimStart('\n');
		if (!trimmed.StartsWith("---\n", StringComparison.Ordinal))
		{
			return trimmed;
		}

		var end = trimmed.IndexOf("\n---\n", 3, StringComparison.Ordinal);
		return end < 0 ? trimmed : trimmed[(end + 5)..];
	}

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(
			Path.Combine(dir.FullName, "modmon.csproj")))
		{
			dir = dir.Parent;
		}

		Assert.True(
			dir != null,
			"Could not locate repo root containing modmon.csproj");
		return dir!.FullName;
	}
}
