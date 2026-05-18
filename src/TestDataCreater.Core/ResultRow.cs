namespace TestDataCreater.Core;

public sealed class ResultRow
{
    public string Id { get; set; } = ResultColumn.CreateId();

    public Dictionary<string, CellValue> Cells { get; set; } = [];

    public void SetCell(string columnId, CellValue value)
    {
        Cells[columnId] = value;
    }

    public CellValue GetCell(string columnId, CellValueKind defaultKind = CellValueKind.String)
    {
        if (!Cells.TryGetValue(columnId, out CellValue? value))
        {
            value = new CellValue(defaultKind);
            Cells[columnId] = value;
        }

        return value;
    }
}
