namespace TestDataCreater.Core;

public sealed class WorkspaceDocument
{
    public string? ActiveResultSetId { get; set; }

    public List<ResultSet> ResultSets { get; set; } = [];

    public static WorkspaceDocument CreateDefault()
    {
        ResultSet resultSet = new("Default");
        resultSet.AddColumn("COLUMN1");
        resultSet.AddRow();

        return new WorkspaceDocument
        {
            ActiveResultSetId = resultSet.Id,
            ResultSets = [resultSet]
        };
    }

    public void ResetToDefault()
    {
        WorkspaceDocument defaultDocument = CreateDefault();
        ActiveResultSetId = defaultDocument.ActiveResultSetId;
        ResultSets = defaultDocument.ResultSets;
    }
}
