using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class JsonFileSerializerTests : IDisposable
{
	private readonly string _rootPath;
	private readonly JsonFileSerializer _jsonFileSerializer = new();

	public JsonFileSerializerTests()
	{
		_rootPath = Path.Combine(Path.GetTempPath(), $"AgenticColorCreator.Tests.{Guid.NewGuid():N}");
		Directory.CreateDirectory(_rootPath);
	}

	[Fact]
	public void SerializeFile_ThenDeserializeFile_RoundTripsTreeViewEntries()
	{
		var path = Path.Combine(_rootPath, "TreeViewItems.json");
		var sourceEntries = new List<TreeViewSourceEntry>
		{
			new() { Value = "controls/inputs/textbox", Type = "control" },
			new() { Value = "palette/inputs/checkbox", Type = "palette" },
		};

		_jsonFileSerializer.SerializeFile(path, sourceEntries);
		var deserializedEntries = _jsonFileSerializer.DeserializeFile<List<TreeViewSourceEntry>>(path);

		Assert.Equal(2, deserializedEntries.Count);
		Assert.Equal("controls/inputs/textbox", deserializedEntries[0].Value);
		Assert.Equal("control", deserializedEntries[0].Type);
		Assert.Equal("palette/inputs/checkbox", deserializedEntries[1].Value);
		Assert.Equal("palette", deserializedEntries[1].Type);
	}

	public void Dispose()
	{
		if (Directory.Exists(_rootPath))
		{
			Directory.Delete(_rootPath, true);
		}
	}
}
