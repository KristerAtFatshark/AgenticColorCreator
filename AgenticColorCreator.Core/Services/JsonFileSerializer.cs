using System;
using System.IO;
using Newtonsoft.Json;

namespace AgenticColorCreator.Core.Services;

public sealed class JsonFileSerializer
{
	private static readonly JsonSerializerSettings SerializerSettings = new()
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
	};

	public T DeserializeFile<T>(string path)
	{
		var json = File.ReadAllText(path);
		var value = JsonConvert.DeserializeObject<T>(json, SerializerSettings);

		if (value is null)
		{
			throw new InvalidOperationException($"Failed to deserialize JSON file '{path}'.");
		}

		return value;
	}

	public void SerializeFile<T>(string path, T value)
	{
		var directoryPath = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		var json = JsonConvert.SerializeObject(value, SerializerSettings);
		File.WriteAllText(path, json);
	}
}
