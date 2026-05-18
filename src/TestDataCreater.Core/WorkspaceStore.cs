using System.Text.Json;

namespace TestDataCreater.Core;

public sealed class WorkspaceStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public WorkspaceDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return WorkspaceDocument.CreateDefault();
        }

        string json = File.ReadAllText(path);
        WorkspaceDocument? document = JsonSerializer.Deserialize<WorkspaceDocument>(json, Options);
        return document is null || document.ResultSets.Count == 0 ? WorkspaceDocument.CreateDefault() : document;
    }

    public void Save(string path, WorkspaceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(document, Options);
        File.WriteAllText(path, json);
    }
}
