using System.Text.RegularExpressions;

namespace ModMon.Cli;

internal static class NameValidator
{
	private const string IdentifierPattern = "^[A-Za-z][A-Za-z0-9_]*$";

	public static bool IsValid(string name)
	{
		return !string.IsNullOrWhiteSpace(name)
			&& Regex.IsMatch(name, IdentifierPattern);
	}

	public static string Describe(string kind, string name)
	{
		return $"Invalid {kind} name '{name}'. " +
			"Use letters, digits and underscores; " +
			"the name must start with a letter.";
	}
}
