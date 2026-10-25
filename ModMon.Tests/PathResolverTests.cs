using ModMon.Cli;

namespace ModMon.Tests;

public sealed class PathResolverTests
{
	[Fact]
	public void ReturnsFullPathForRelativeInput()
	{
		var result = PathResolver.TryGetFullPath(".");

		Assert.NotNull(result);
		Assert.True(Path.IsPathRooted(result));
	}

	[Fact]
	public void ResolvesDotDotSegments()
	{
		var result = PathResolver.TryGetFullPath(
			Path.Combine(".", "sub", "..", "file.txt"));

		Assert.NotNull(result);
		Assert.DoesNotContain("..", result);
	}

	[Fact]
	public void ReturnsNullForInvalidInput()
	{
		var result = PathResolver.TryGetFullPath(string.Empty);

		Assert.Null(result);
	}
}
