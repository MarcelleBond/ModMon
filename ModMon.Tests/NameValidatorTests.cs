using ModMon.Cli;

namespace ModMon.Tests;

public sealed class NameValidatorTests
{
	[Theory]
	[InlineData("Demo")]
	[InlineData("MyProject")]
	[InlineData("A")]
	[InlineData("Project_2")]
	[InlineData("a9")]
	public void AcceptsValidIdentifiers(string name)
	{
		Assert.True(NameValidator.IsValid(name));
	}

	[Theory]
	[InlineData("My App")]
	[InlineData("My-App")]
	[InlineData("2Fast")]
	[InlineData("_lead")]
	[InlineData("App.Name")]
	[InlineData("App/Name")]
	[InlineData("")]
	[InlineData("   ")]
	public void RejectsInvalidIdentifiers(string name)
	{
		Assert.False(NameValidator.IsValid(name));
	}

	[Fact]
	public void DescribeMentionsTheKindAndName()
	{
		var message = NameValidator.Describe("project", "My App");

		Assert.Contains("project", message);
		Assert.Contains("My App", message);
	}
}
