using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModMon.Cli;

internal sealed class JsonOutputWriter : IOutputWriter
{
	private readonly List<string> _progressMessages = new();
	private object? _result;

	public void WriteSuccess(object data)
	{
		_result = data;
	}

	public void WriteError(string message, object? details = null)
	{
		_result = new
		{
			success = false,
			message,
			details
		};
	}

	public void WriteProgress(string message)
	{
		_progressMessages.Add(message);
	}

	public void Flush()
	{
		if (_result == null)
		{
			return;
		}

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			Converters = { new JsonStringEnumConverter() }
		};

		var json = JsonSerializer.Serialize(_result, options);
		Console.WriteLine(json);
	}
}
