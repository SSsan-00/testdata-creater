namespace TestDataCreater.Core;

public sealed class ResultColumn
{
    public string Id { get; set; } = CreateId();

    public string Name { get; set; } = "Column";

    public CellValueKind DefaultKind { get; set; } = CellValueKind.String;

    internal static string CreateId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
